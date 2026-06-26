using FMFlow.FlowAPI.Interface;
using FMFlow.Integrations.Firebase.DTOs;

namespace FMFlow.Integrations.Firebase;

public interface IFCMService
{
	Task<Result> SaveDeviceRegistration(FCMRegistrationRequestDto request, CancellationToken ct);

	Task<Result> SendPushNotificationToUser(int userId, string title, string messageBody, PushNotificationPayload payload, CancellationToken ct);
}
