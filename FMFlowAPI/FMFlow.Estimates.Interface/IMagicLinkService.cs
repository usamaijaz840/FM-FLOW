using FMFlow.FlowAPI.Interface;

namespace FMFlow.Estimates.Interface;

public interface IMagicLinkService
{
  Task<Result<string>> GenerateCustomerEstimateMagicLink(int estimateId, int customerId, CancellationToken ct);

  Task<Result<string>> GenerateEstimateRecipientMagicLink(int estimateId, int recipientId, CancellationToken ct);
}