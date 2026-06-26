using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Common;

public interface INonceService
{
	Task<Result<string>> GenerateAndSaveNonce(int entityId, NonceType type, CancellationToken ct);

	Task<Result> ValidateNonce(string nonce, CancellationToken ct);

	Task<Result<Nonce>> ValidateAndConsumeNonce(string nonce, CancellationToken ct);
}
