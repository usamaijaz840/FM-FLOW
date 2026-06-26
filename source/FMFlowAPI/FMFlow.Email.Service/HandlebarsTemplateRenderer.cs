using System.Text.Json;
using System.Collections.Concurrent;
using FMFlow.Email.Interface;
using HandlebarsDotNet;
using Microsoft.Extensions.Options;

namespace FMFlow.Email.Service;

public sealed class HandlebarsTemplateRenderer(IOptions<EmailSettings> emailSettingsOptions) : IEmailTemplateRenderer
{
    private readonly EmailSettings _settings = emailSettingsOptions.Value;
    private readonly string _root = ComputeRoot(emailSettingsOptions);
    private const string DefaultHtmlLayout = "_layout.hbs.html";
    private const string DefaultTextLayout = "_layout.hbs.txt";
    private const int WrapperWidth = 600;

    private readonly Dictionary<string, string> _preloadedSharedPartials = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _preloadedBrandPartials = new(StringComparer.OrdinalIgnoreCase);
    private sealed record FileCacheEntry(string Content, DateTime LastWriteUtc);
    private sealed record LayoutCacheEntry(HandlebarsTemplate<object, object> Compiled, DateTime LastWriteUtc);
    private readonly ConcurrentDictionary<string, FileCacheEntry> _fileCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, LayoutCacheEntry> _layoutCache = new(StringComparer.OrdinalIgnoreCase);

    private static string ComputeRoot(IOptions<EmailSettings> options)
    {
        var root = options.Value.TemplateRoot;
        if (Path.IsPathRooted(root)) return root;
        return Path.Combine(AppContext.BaseDirectory, root);
    }

    public async Task<RenderedEmail> RenderByTemplateIdAsync(string templateId, object model, CancellationToken ct)
    {
        var key = await ResolveKeyByTemplateIdAsync(templateId, ct)
            ?? throw new InvalidOperationException($"Template with id '{templateId}' not found under '{_settings.TemplateRoot}'.");
        return await RenderByKeyAsync(key, model, ct);
    }

    public async Task<RenderedEmail> RenderByKeyAsync(string templateKey, object model, CancellationToken ct)
    {
        var loaded = await LoadActiveTemplatesAsync(templateKey, ct);

        // Determine brand-specific layout if configured or inferred
        var layoutKey = await ResolveLayoutKeyAsync(templateKey, loaded.TemplateDir, loaded.VersionDir, ct);

        // Create a brand-scoped environment with shared + brand partials preloaded
        var hb = CreateBrandEnvironment(layoutKey);
        // Register template-level partials only into this request-scoped environment
        RegisterTemplatePartials(hb, loaded.TemplateDir);

        var subjectCompiled = hb.Compile(loaded.Subject ?? string.Empty);
        var htmlCompiled = hb.Compile(loaded.Html ?? string.Empty);
        var textCompiled = hb.Compile(loaded.Text ?? string.Empty);

        var subject = subjectCompiled(model) ?? string.Empty;
        var htmlBody = htmlCompiled(model) ?? string.Empty;
        var text = string.IsNullOrWhiteSpace(loaded.Text) ? null : textCompiled(model);
        var htmlLayoutFileName = string.IsNullOrWhiteSpace(layoutKey) ? DefaultHtmlLayout : $"_layout.{layoutKey}.hbs.html";
        var textLayoutFileName = string.IsNullOrWhiteSpace(layoutKey) ? DefaultTextLayout : $"_layout.{layoutKey}.hbs.txt";

        // Optional layout wrapping for HTML and Text
        htmlBody = ApplyLayoutIfPresent(hb, htmlBody, loaded.TemplateDir, htmlLayoutFileName: htmlLayoutFileName);
        if (!string.IsNullOrWhiteSpace(text))
        {
            text = ApplyLayoutIfPresent(hb, text!, loaded.TemplateDir, htmlLayoutFileName: textLayoutFileName);
        }

        // Ensure final HTML is a full document
        htmlBody = EnsureHtmlDocument(htmlBody);

        // Inline CSS for better client compatibility (HTML only)
        string inlinedHtml;
        try
        {
            var inlined = PreMailer.Net.PreMailer.MoveCssInline(htmlBody);
            inlinedHtml = inlined.Html ?? htmlBody;
        }
        catch
        {
            inlinedHtml = htmlBody;
        }

        return new RenderedEmail(subject.Trim(), inlinedHtml, text);
    }

    private async Task<string?> ResolveKeyByTemplateIdAsync(string templateId, CancellationToken ct)
    {
        foreach (var dir in EnumerateTemplateDirectories())
        {
            ct.ThrowIfCancellationRequested();
            var metaPath = Path.Combine(dir, "template.json");
            if (!File.Exists(metaPath)) continue;
            try
            {
                using var stream = File.OpenRead(metaPath);
                var meta = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: ct);
                var id = meta.GetProperty("sendgridTemplateId").GetString();
                if (string.Equals(id, templateId, StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFileName(dir);
                }
            }
            catch
            {
                // ignore incorrect JSON format
            }
        }
        return null;
    }

    // Local data record to carry loaded template assets
    private sealed record LoadedTemplates(string? Subject, string? Html, string? Text, string TemplateDir, string VersionDir);

    private async Task<LoadedTemplates> LoadActiveTemplatesAsync(string templateKey, CancellationToken ct)
    {
        var templateDir = ResolveTemplateDirByKey(templateKey);
        if (templateDir == null)
            throw new DirectoryNotFoundException($"Template directory not found: {templateKey}");
        
        // choose active version folder
        string? chosenVersionDir = null;
        foreach (var versionDir in Directory.EnumerateDirectories(templateDir))
        {
            ct.ThrowIfCancellationRequested();
            var versionMeta = Path.Combine(versionDir, "version.json");
            if (!File.Exists(versionMeta)) continue;
            try
            {
                using var stream = File.OpenRead(versionMeta);
                var json = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: ct);
                var active = json.TryGetProperty("active", out var activeEl) && (activeEl.GetBoolean());
                if (active)
                {
                    chosenVersionDir = versionDir;
                    break;
                }
            }
            catch
            {
                // ignore
            }
        }
        chosenVersionDir ??= Directory.EnumerateDirectories(templateDir).FirstOrDefault();
        if (chosenVersionDir == null)
            throw new InvalidOperationException($"No version directories found under {templateDir}");
        
        string? ReadOrNull(string file)
        {
            if (!File.Exists(file)) return null;
            return ReadCached(file);
        }

        var subjectTpl = ReadOrNull(Path.Combine(chosenVersionDir, "subject.hbs.txt"));
        var htmlTpl = ReadOrNull(Path.Combine(chosenVersionDir, "html.hbs.html"));
        var textTpl = ReadOrNull(Path.Combine(chosenVersionDir, "text.hbs.txt"));

        return new LoadedTemplates(subjectTpl, htmlTpl, textTpl, templateDir, chosenVersionDir);
    }

    private static string? ReadLayoutFromMeta(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return null;
        try
        {
            using var s = File.OpenRead(jsonPath);
            var meta = JsonSerializer.Deserialize<JsonElement>(s);
            if (meta.TryGetProperty("layout", out var l) && l.ValueKind == JsonValueKind.String)
            {
                var v = l.GetString();
                return string.IsNullOrWhiteSpace(v) ? null : v;
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private async Task<string?> ResolveLayoutKeyAsync(string templateKey, string templateDir, string versionDir, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var fromTemplate = ReadLayoutFromMeta(Path.Combine(templateDir, "template.json"));
        if (!string.IsNullOrWhiteSpace(fromTemplate)) return fromTemplate;
        var fromVersion = ReadLayoutFromMeta(Path.Combine(versionDir, "version.json"));
        if (!string.IsNullOrWhiteSpace(fromVersion)) return fromVersion;

        // Heuristic: fm-* templates use fmflow wrapper; otherwise referral
        if (templateKey.StartsWith("fm-", StringComparison.OrdinalIgnoreCase)) return "fmflow";
        return "referral";
    }

    private string GetRoot() => _root;

    private IEnumerable<string> EnumerateTemplateDirectories()
    {
        var root = GetRoot();
        // Support two layouts: templates directly under root, or under root/MyTemplates
        var direct = Directory.Exists(root) ? Directory.EnumerateDirectories(root) : Array.Empty<string>();
        var my = Path.Combine(root, "MyTemplates");
        var nested = Directory.Exists(my) ? Directory.EnumerateDirectories(my) : Array.Empty<string>();
        // Exclude _shared
        return direct.Concat(nested).Where(d => Path.GetFileName(d) != "_shared");
    }

    private string? ResolveTemplateDirByKey(string key)
    {
        foreach (var dir in EnumerateTemplateDirectories())
        {
            if (string.Equals(Path.GetFileName(dir), key, StringComparison.OrdinalIgnoreCase)) return dir;
        }
        return null;
    }

    private void RegisterBrandPartials(IHandlebars hb, string? layoutKey)
    {
        if (string.IsNullOrWhiteSpace(layoutKey)) return;
        var brand = layoutKey.ToLowerInvariant(); // expected: "fmflow" or "referral"
        // Map layout keys to folder names
        var folder = brand switch
        {
            "fmflow" => "fm",
            "referral" => "rs",
            _ => brand
        };
        // Use preloaded brand header/footer content; avoid per-call disk reads
        if (_preloadedBrandPartials.TryGetValue($"{folder}/header", out var headerContent))
        {
            hb.RegisterTemplate("header", headerContent);
        }
        if (_preloadedBrandPartials.TryGetValue($"{folder}/footer", out var footerContent))
        {
            hb.RegisterTemplate("footer", footerContent);
        }
    }

    private void RegisterSharedPartials(IHandlebars hb)
    {
        foreach (var kv in _preloadedSharedPartials)
        {
            hb.RegisterTemplate(kv.Key, kv.Value);
        }
    }

    private void RegisterTemplatePartials(IHandlebars hb, string templateDir)
    {
        var partialsDir = Path.Combine(templateDir, "partials");
        if (Directory.Exists(partialsDir))
        {
            RegisterPartialsFromDirectory(hb, partialsDir, baseNamePrefix: string.Empty);
        }
    }

    private void RegisterPartialsFromDirectory(IHandlebars hb, string dir, string baseNamePrefix)
    {
        foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
        {
            // Avoid auto-registering brand-specific header/footer here; they are mapped via RegisterBrandPartials
            var rel = Path.GetRelativePath(dir, file).Replace('\\', '/');
            if (rel.StartsWith("brands/", StringComparison.OrdinalIgnoreCase)) continue;
            if (!file.EndsWith(".hbs") && !file.EndsWith(".hbs.html") && !file.EndsWith(".hbs.txt")) continue;
            var name = Path.GetFileNameWithoutExtension(rel);
            // strip double extensions like .hbs.html → .html removed first, name becomes file without .html then .hbs removed by GetFileNameWithoutExtension
            if (name.EndsWith(".hbs", StringComparison.OrdinalIgnoreCase))
            {
                name = Path.GetFileNameWithoutExtension(name);
            }
            if (!string.IsNullOrEmpty(baseNamePrefix)) name = baseNamePrefix + name;
            var content = File.ReadAllText(file);
            hb.RegisterTemplate(name, content);
        }
    }

    private IHandlebars CreateBrandEnvironment(string? layoutKey)
    {
        var hb = Handlebars.Create();
        // Shared partials
        if (_preloadedSharedPartials.Count == 0) PreloadSharedPartials();
        if (_preloadedBrandPartials.Count == 0) PreloadBrandHeaderFooter();
        RegisterSharedPartials(hb);
        // Brand header/footer aliases
        RegisterBrandPartials(hb, layoutKey);
        return hb;
    }

    private void PreloadSharedPartials()
    {
        _preloadedSharedPartials.Clear();
        void LoadDir(string rootDir)
        {
            if (!Directory.Exists(rootDir)) return;
            foreach (var file in Directory.EnumerateFiles(rootDir, "*.*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(rootDir, file).Replace('\\', '/');
                if (rel.StartsWith("brands/", StringComparison.OrdinalIgnoreCase)) continue;
                if (!file.EndsWith(".hbs") && !file.EndsWith(".hbs.html") && !file.EndsWith(".hbs.txt")) continue;
                var name = Path.GetFileNameWithoutExtension(rel);
                if (name.EndsWith(".hbs", StringComparison.OrdinalIgnoreCase))
                {
                    name = Path.GetFileNameWithoutExtension(name);
                }
                var content = File.ReadAllText(file);
                // last-in-wins to allow sibling directories to override
                _preloadedSharedPartials[name] = content;
            }
        }
        var root = GetRoot();
        LoadDir(Path.Combine(root, "_shared", "partials"));
        var parent = Path.GetDirectoryName(root) ?? root;
        LoadDir(Path.Combine(parent, "_shared", "partials"));
    }

    private void PreloadBrandHeaderFooter()
    {
        _preloadedBrandPartials.Clear();
        void LoadBrand(string folder)
        {
            string? FindFile(string dir, string baseName)
            {
                var a = Directory.EnumerateFiles(dir, $"{baseName}.hbs.*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (!string.IsNullOrEmpty(a)) return a;
                return Directory.EnumerateFiles(dir, $"{baseName}.hbs", SearchOption.TopDirectoryOnly).FirstOrDefault();
            }
            var root = GetRoot();
            var candidates = new List<string>
            {
                Path.Combine(root, "_shared", "partials", "brands", folder)
            };
            var parent = Path.GetDirectoryName(root) ?? root;
            candidates.Add(Path.Combine(parent, "_shared", "partials", "brands", folder));
            foreach (var dir in candidates)
            {
                if (!Directory.Exists(dir)) continue;
                var hp = FindFile(dir, "header");
                var fp = FindFile(dir, "footer");
                if (!string.IsNullOrEmpty(hp)) _preloadedBrandPartials[$"{folder}/header"] = File.ReadAllText(hp);
                if (!string.IsNullOrEmpty(fp)) _preloadedBrandPartials[$"{folder}/footer"] = File.ReadAllText(fp);
                break; // first hit wins
            }
        }
        LoadBrand("fm");
        LoadBrand("rs");
    }

    private string ApplyLayoutIfPresent(IHandlebars hb, string body, string templateDir, string htmlLayoutFileName)
    {
        // Local layout has priority
        string? layoutPath = Path.Combine(templateDir, htmlLayoutFileName);
        if (!File.Exists(layoutPath))
        {
            var sharedLayout = Path.Combine(GetRoot(), "_shared", "layouts", htmlLayoutFileName);
            if (File.Exists(sharedLayout)) layoutPath = sharedLayout; else layoutPath = null;
        }
        // Legacy/sibling shared: Templates/_shared/layouts (when TemplateRoot is Templates/_Emails)
        if (layoutPath == null)
        {
            var root = GetRoot();
            var parent = Path.GetDirectoryName(root) ?? root;
            var siblingLayout = Path.Combine(parent, "_shared", "layouts", htmlLayoutFileName);
            if (File.Exists(siblingLayout)) layoutPath = siblingLayout;
        }
        if (layoutPath == null) { return body; }

        var compiled = GetOrCompileLayout(hb, layoutPath);
        // Provide the body's rendered HTML/text as a variable for the layout
        var modelWithBody = new { body };
        var rendered = compiled(modelWithBody);
        var wrapped = rendered?.ToString() ?? body;
        return wrapped;
    }

    private static string EnsureHtmlDocument(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "<!doctype html><html><head><meta charset=\"utf-8\"></head><body></body></html>";
        var lower = html.TrimStart().ToLowerInvariant();
        if (lower.StartsWith("<!doctype") || lower.StartsWith("<html")) return html;
        // Wrap inner rows/content in a minimal document and container
        return $"<!doctype html><html><head><meta charset=\"utf-8\"></head><body><center><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\"><tr><td align=\"center\"><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"{WrapperWidth}\">{html}</table></td></tr></table></center></body></html>";
    }

    private string ReadCached(string path)
    {
        var mtime = File.GetLastWriteTimeUtc(path);
        if (_fileCache.TryGetValue(path, out var entry) && entry.LastWriteUtc == mtime)
        {
            return entry.Content;
        }
        var content = File.ReadAllText(path);
        _fileCache[path] = new FileCacheEntry(content, mtime);
        return content;
    }

    private HandlebarsTemplate<object, object> GetOrCompileLayout(IHandlebars hb, string layoutPath)
    {
        var mtime = File.GetLastWriteTimeUtc(layoutPath);
        if (_layoutCache.TryGetValue(layoutPath, out var entry) && entry.LastWriteUtc == mtime)
        {
            return entry.Compiled;
        }
        var tpl = ReadCached(layoutPath);
        var compiled = hb.Compile(tpl);
        _layoutCache[layoutPath] = new LayoutCacheEntry(compiled, mtime);
        return compiled;
    }
}


