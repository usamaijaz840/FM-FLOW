using System.Net;
using FMFlow.Email.Interface;
using FMFlow.FlowAPI.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FMFlow.Email.Service
{
	public class SendGridEmailService : IEmailService
	{
		private readonly EmailSettings emailSettings;
		private readonly ILogger<SendGridEmailService> logger;
    private readonly SendGridClient sendGridClient;
    private readonly IEmailTemplateRenderer templateRenderer;

        public SendGridEmailService(
            IOptions<EmailSettings> emailSettingsOptions,
            ILogger<SendGridEmailService> logger,
            IEmailTemplateRenderer templateRenderer)
		{
			emailSettings = emailSettingsOptions.Value;
			this.logger = logger;
            this.templateRenderer = templateRenderer;
			sendGridClient = new SendGridClient(emailSettings.ApiKey);
		}

		public async Task<Result> SendEmail(
			string recipient,
			string subject,
			string htmlContent,
			CancellationToken ct,
			string? plainTextContent = null)
		{
			try
			{
				if (!emailSettings.EnableEmailService)
				{
					logger.LogInformation("Email service is disabled. Email to {Recipient} with subject {Subject} not sent.", recipient, subject);
					return Result.Success();
				}

				var from = new EmailAddress(emailSettings.FromEmail, emailSettings.FromName);
				var toAddress = new EmailAddress(recipient);

				var message = MailHelper.CreateSingleEmail(
					from,
					toAddress,
					subject,
					plainTextContent ?? string.Empty,
					htmlContent);

				var response = await sendGridClient.SendEmailAsync(message, ct);

				if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
				{
					logger.LogInformation("Email sent successfully to {Recipient} with subject {Subject}", recipient, subject);
					return Result.Success();
				}

				var responseBody = await response.Body.ReadAsStringAsync(ct);

				logger.LogError("Failed to send email. Status code: {StatusCode}, Response: {Response}",
					response.StatusCode, responseBody);

				return Result.Failure($"Failed to send email. Status code: {response.StatusCode}");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred while sending email to {Recipient} with subject {Subject}", recipient, subject);
				return Result.Failure($"An error occurred while sending email: {ex.Message}");
			}
		}

        public async Task<Result> SendTemplateEmail(
			string recipient,
			string templateId,
			object dynamicTemplateData,
			CancellationToken ct)
		{
			try
			{
				if (!emailSettings.EnableEmailService)
				{
					logger.LogInformation("Email service is disabled. Template email to {Recipient} with template {TemplateId} not sent.", recipient, templateId);
					return Result.Success();
				}

                var from = new EmailAddress(emailSettings.FromEmail, emailSettings.FromName);
                var toAddress = new EmailAddress(recipient);

                Response response;
                if (emailSettings.UseCodeTemplates)
                {
                    // Render from in-repo templates
                    var rendered = await templateRenderer.RenderByTemplateIdAsync(templateId, dynamicTemplateData, ct);
                    var message = MailHelper.CreateSingleEmail(
                        from,
                        toAddress,
                        rendered.Subject,
                        rendered.TextBody ?? string.Empty,
                        rendered.HtmlBody);
                    response = await sendGridClient.SendEmailAsync(message, ct);
                }
                else
                {
                    var message = MailHelper.CreateSingleTemplateEmail(
                        from,
                        toAddress,
                        templateId,
                        dynamicTemplateData);
                    response = await sendGridClient.SendEmailAsync(message, ct);
                }

				if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
				{
					logger.LogInformation("Template email sent successfully to {Recipient} with template {TemplateId}", recipient, templateId);
					return Result.Success();
				}

				var responseBody = await response.Body.ReadAsStringAsync(ct);

				logger.LogError("Failed to send template email. Status code: {StatusCode}, Response: {Response}",
					response.StatusCode, responseBody);

				return Result.Failure($"Failed to send template email. Status code: {response.StatusCode}");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred while sending template email to {Recipient} with template {TemplateId}", recipient, templateId);
				return Result.Failure($"An error occurred while sending template email: {ex.Message}");
			}
		}

		public async Task<Result> SendTemplateEmailToMultipleRecipients(
			List<string> recipients,
			string templateId,
			object dynamicTemplateData,
			CancellationToken ct)
		{
			try
			{
				if (!emailSettings.EnableEmailService)
				{
					logger.LogInformation("Email service is disabled. Template email to multiple recipients with template {TemplateId} not sent.", templateId);
					return Result.Success();
				}

				var from = new EmailAddress(emailSettings.FromEmail, emailSettings.FromName);

				Response response;
				if (emailSettings.UseCodeTemplates)
				{
					// Render once (shared data) and send individually to avoid hosted templates
					var rendered = await templateRenderer.RenderByTemplateIdAsync(templateId, dynamicTemplateData, ct);

					foreach (var recipient in recipients)
					{
						ct.ThrowIfCancellationRequested();
						var to = new EmailAddress(recipient);
						var msg = MailHelper.CreateSingleEmail(
							from,
							to,
							rendered.Subject,
							rendered.TextBody ?? string.Empty,
							rendered.HtmlBody);

						response = await sendGridClient.SendEmailAsync(msg, ct);
						if (response.StatusCode != HttpStatusCode.Accepted && response.StatusCode != HttpStatusCode.OK)
						{
							var body = await response.Body.ReadAsStringAsync(ct);
							logger.LogError("Failed to send rendered code-template email to {Recipient}. Status: {Status}, Response: {Response}",
								recipient, response.StatusCode, body);
							return Result.Failure($"Failed to send to {recipient}. Status code: {response.StatusCode}");
						}
					}

					logger.LogInformation("Template email (code-rendered) sent successfully to {Count} recipients with template {TemplateId}", recipients.Count, templateId);
					return Result.Success();
				}
				else
				{
					// Legacy hosted SendGrid dynamic template to multiple recipients
					var recipientsAddresses = recipients.Select(email => new EmailAddress(email)).ToList();
					var message = MailHelper.CreateSingleEmailToMultipleRecipients(
						from,
						recipientsAddresses,
						subject: null, // subject is ignored for templates
						plainTextContent: null,
						htmlContent: null
					);

					message.SetTemplateId(templateId);
					message.SetTemplateData(dynamicTemplateData);

					response = await sendGridClient.SendEmailAsync(message, ct);
				}

				if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
				{
					logger.LogInformation("Template email sent successfully to multiple recipients with template {TemplateId}", templateId);
					return Result.Success();
				}

				var responseBody = await response.Body.ReadAsStringAsync(ct);

				logger.LogError("Failed to send template email to multiple recipients. Status code: {StatusCode}, Response: {Response}",
					response.StatusCode, responseBody);

				return Result.Failure($"Failed to send template email to multiple recipients. Status code: {response.StatusCode}");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred while sending template email to multiple recipients with template {TemplateId}", templateId);
				return Result.Failure($"An error occurred while sending template email to multiple recipients: {ex.Message}");
			}
		}
	}
}
