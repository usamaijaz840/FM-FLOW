using FMFlow.SMS.Interface;
using Microsoft.Extensions.Configuration;

namespace FMFlow.SMS.Service;

public class SMSSenderService(IConfiguration config) : ISMSSenderService
{
	readonly string _apiBaseUrl = config["ApiBaseUrl"] ?? string.Empty;
	readonly string _fromPhoneNumber = config["Twilio:FromPhoneNumber"] ?? string.Empty;

	public void SendSmsCustomerProCreatedScheduledEstimate(int projectId, string toPhoneNumber)
	{
		var text = $"Congrats, you’ve been scheduled for a new Referral Source estimate! You can log in to your FM Flow account to view the details now:\n\n{_apiBaseUrl}/app/projects/{projectId}";

		TwilioSMSService.SendSMS(text, _fromPhoneNumber, toPhoneNumber);
	}

	public void SendSmsCustomerProCreatedEstimate(int estimateId, string proBusinessName, string toPhoneNumber)
	{
		var text = $"Here’s an estimate from {proBusinessName} for your upcoming painting project. You can click the link below to review, and please reach out to us if you have any questions.\n\n{_apiBaseUrl}/app/estimate/{estimateId}/summary";

		TwilioSMSService.SendSMS(text, _fromPhoneNumber, toPhoneNumber);
	}
}
