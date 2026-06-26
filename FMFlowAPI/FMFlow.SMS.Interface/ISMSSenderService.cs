namespace FMFlow.SMS.Interface;

public interface ISMSSenderService
{
	void SendSmsCustomerProCreatedEstimate(int estimateId, string proBusinessName, string phoneNumber);

	void SendSmsCustomerProCreatedScheduledEstimate(int projectId, string phoneNumber);
}
