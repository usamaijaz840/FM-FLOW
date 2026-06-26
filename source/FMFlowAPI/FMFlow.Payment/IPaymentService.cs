using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Integrations.MxMerchant.Interface.DTOs;

namespace FMFlow.ProPayments.Interface;

public interface IPaymentService
{
	Task<ApiResponse> SavePaymentInfo(PaymentInfoModel info, string onboardingFormStop, CancellationToken ct);

	Task<VaultedCard?> GetVaultedCard(long customerId, long cardId, CancellationToken ct);

	Task<PaymentInfoModel> GetPaymentInfoByProUserID(int proUserID, CancellationToken ct);

	Task<ApiResponse> UpdatePaymentInfo(PaymentInfoModel info, CancellationToken ct);
}
