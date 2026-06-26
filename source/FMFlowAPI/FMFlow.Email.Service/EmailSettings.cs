namespace FMFlow.Email.Service;

    public class EmailSettings
    {
        public const string SectionName = "EmailSettings";

        /// <summary>
        /// SendGrid API Key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// From email address
        /// </summary>
        public string FromEmail { get; set; } = string.Empty;

        /// <summary>
        /// From name (display name)
        /// </summary>
        public string FromName { get; set; } = string.Empty;

        /// <summary>
        /// Whether to enable email sending
        /// </summary>
        public bool EnableEmailService { get; set; } = false;

        /// <summary>
        /// When true, render templates from the local codebase instead of SendGrid dynamic templates
        /// </summary>
        public bool UseCodeTemplates { get; set; } = false;

        /// <summary>
        /// Root directory (relative to application) containing exported templates
        /// Defaults to Templates/_Emails
        /// </summary>
        public string TemplateRoot { get; set; } = "Templates/_Emails";
    }