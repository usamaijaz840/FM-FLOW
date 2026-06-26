namespace FMFlow.Email.Interface;

public record RenderedEmail(string Subject, string HtmlBody, string? TextBody);

public interface IEmailTemplateRenderer
{
    Task<RenderedEmail> RenderByTemplateIdAsync(string templateId, object model, CancellationToken ct);
    Task<RenderedEmail> RenderByKeyAsync(string templateKey, object model, CancellationToken ct);
}


