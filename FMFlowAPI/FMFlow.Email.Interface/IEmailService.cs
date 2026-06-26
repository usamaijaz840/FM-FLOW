using FMFlow.FlowAPI.Interface;

namespace FMFlow.Email.Interface
{
	public interface IEmailService
	{
		/// <summary>
		/// Sends an email with the specified parameters
		/// </summary>
		/// <param name="recipient">Recipient email address</param>
		/// <param name="subject">Email subject</param>
		/// <param name="htmlContent">HTML content of the email</param>
		/// <param name="plainTextContent">Plain text content of the email</param>
		/// <param name="ct">Cancellation token</param>
		/// <returns>Result indicating success or failure with error details</returns>
		Task<Result> SendEmail(string recipient, string subject, string htmlContent, CancellationToken ct, string? plainTextContent = null);

		/// <summary>
		/// Sends an email with template
		/// </summary>
		/// <param name="recipient">Recipient email address</param>
		/// <param name="templateId">SendGrid template ID</param>
		/// <param name="dynamicTemplateData">Dynamic template data</param>
		/// <param name="ct">Cancellation token</param>
		/// <returns>Result indicating success or failure with error details</returns>
		Task<Result> SendTemplateEmail(string recipient, string templateId, object dynamicTemplateData, CancellationToken ct);

		/// <summary>
		/// Sends an email with template to multiple recipients
		/// </summary>
		/// <param name="recipients">Recipients email addresses</param>
		/// <param name="templateId">SendGrid template ID</param>
		/// <param name="dynamicTemplateData">Dynamic template data</param>
		/// <param name="ct">Cancellation token</param>
		/// <returns>Result indicating success or failure with error details</returns>
		Task<Result> SendTemplateEmailToMultipleRecipients(List<string> recipients, string templateId, object dynamicTemplateData, CancellationToken ct);
	}
}
