using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace FMFlow.SMS.Service;

public class TwilioSMSService
{
	private static bool _smsEnabled = false;

	public static void InitializeTwilioClient(string accountSid, string authToken, bool isSmsEnabled)
	{
		_smsEnabled = isSmsEnabled;
		TwilioClient.Init(accountSid, authToken);
	}

	public static void SendSMS(string text, string from, string to)
	{
		if (!_smsEnabled)
		{
			// SMS is disabled, skip sending
			return;
		}

		MessageResource.Create(
			body: text,
			from: new Twilio.Types.PhoneNumber(from),
			to: new Twilio.Types.PhoneNumber(to)
		);
	}
}
