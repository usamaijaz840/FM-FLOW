using FMFlow.FlowAPI.Interface;
using FMFlow.Referral.Interface.Dtos;

namespace FMFlow.Referral.Interface;

public interface IReferralsService
{
	Task<Result> Save(ReferralDto referral, CancellationToken ct);
}
