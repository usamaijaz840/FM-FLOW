using EFRepository;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.Integrations.Firebase.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Integrations.Firebase;

public class FCMService(IRepository repository, ICurrentUserService currentUserService) : IFCMService
{
	public async Task<Result> SaveDeviceRegistration(FCMRegistrationRequestDto request, CancellationToken ct)
	{
		var currentUserId = currentUserService.GetUserID();

		FCMRegistration? fcmRegistration;

		fcmRegistration = await repository.Query<FCMRegistration>()
			.ByUserId(currentUserId)
			.ByToken(request.Token)
			.FirstOrDefaultAsync(ct);

		if (fcmRegistration != null)
		{
			fcmRegistration.CheckInDateTime = DateTime.UtcNow;
		}
		else
		{
			fcmRegistration = new FCMRegistration
			{
				UserId = currentUserId,
				Token = request.Token,
			};
		}

		repository.AddOrUpdate(fcmRegistration);

		await repository.SaveAsync(ct);

		await SendPushNotificationToUser(
			currentUserId,
			"Test",
			"This is a test notification",
			new PushNotificationPayload(
				PushNotificationType.General,
				new Dictionary<string, string> {}
			),
			ct
		);

		return Result.Success();
	}

	public async Task<Result> SendPushNotificationToUser(
		int userId,
		string title,
		string messageBody,
		PushNotificationPayload payload,
		CancellationToken ct)
	{
		var registrationTokens = await repository.Query<FCMRegistration>()
			.ByUserId(userId)
			.Select(r => r.Token)
			.Distinct()
			.ToListAsync(ct);

		if (registrationTokens.Count == 0)
			return Result.Success();

		var data = new Dictionary<string, string>(payload.Attributes)
		{
			["type"] = payload.Type.ToString()
		};

		var message = new MulticastMessage
		{
			Notification = new Notification
			{
				Title = title,
				Body = messageBody
			},
			Data = data,
			Tokens = registrationTokens,
			Android = new AndroidConfig
			{
				Priority = Priority.High,
				Notification = new AndroidNotification
				{
					Sound = "default",
					ChannelId = "default_notification_channel",
				}
			},
			Apns = new ApnsConfig
			{
				Headers = new Dictionary<string, string>
				{
					["apns-priority"] = "10"
				},
				Aps = new Aps
				{
					Sound = "default",
				}
			},
			Webpush = new WebpushConfig
			{
				Headers = new Dictionary<string, string>
				{
					["Urgency"] = "high"
				}
			}
		};

		try
		{
			var batch = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, ct);

			var tokensToRemove = new List<string>();

			for (int i = 0; i < batch.Responses.Count; i++)
			{
				var sendResponse = batch.Responses[i];
				// order is preserved, so index i corresponds to registrationTokens[i]
				// ( https://firebase.google.com/docs/cloud-messaging/send/admin-sdk )

				if (!sendResponse.IsSuccess && sendResponse.Exception is FirebaseMessagingException fcmEx)
				{
					var isUnregisteredOrExpired =
						fcmEx.MessagingErrorCode == MessagingErrorCode.Unregistered ||
						fcmEx.MessagingErrorCode == MessagingErrorCode.SenderIdMismatch ||
						fcmEx.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
						fcmEx.ErrorCode == ErrorCode.NotFound;

					if (isUnregisteredOrExpired)
						tokensToRemove.Add(registrationTokens[i]);
				}
			}

			if (tokensToRemove.Count > 0)
			{
				await repository.Query<FCMRegistration>()
					.Where(reg => tokensToRemove.Contains(reg.Token))
					.ExecuteDeleteAsync(ct);
			}
		}
		catch (Exception)
		{
			return Result.Failure("Error when calling Firebase");
		}

		return Result.Success();
	}
}
