using FMFlow.Common;
using FMFlow.Common.Extensions;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.FlowAPI.Interface;
using Microsoft.Extensions.Configuration;

namespace FMFlow.Estimates.Service;

public class MagicLinkService : IMagicLinkService
{
	private readonly INonceService _nonceService;
	private readonly string _webAppBaseUrl;

	public MagicLinkService(INonceService nonceService, IConfiguration config)
	{
		_nonceService = nonceService;

		if (string.IsNullOrWhiteSpace(config["WebAppBaseUrl"]))
		{
			throw new ArgumentException("WebAppBaseUrl is not configured.", nameof(config));
		}

		_webAppBaseUrl = config["WebAppBaseUrl"]!;
	}

	public async Task<Result<string>> GenerateCustomerEstimateMagicLink(int estimateId, int customerId, CancellationToken ct)
	{
		return await GenerateViewEstimateLink(estimateId, customerId, NonceType.CustomerMagicLink, ct);
	}

	public async Task<Result<string>> GenerateEstimateRecipientMagicLink(int estimateId, int recipientId, CancellationToken ct)
	{
		return await GenerateViewEstimateLink(estimateId, recipientId, NonceType.EstimateRecipientMagicLink, ct);
	}

	private async Task<Result<string>> GenerateViewEstimateLink(int estimateId, int entityId, NonceType nonceType, CancellationToken ct)
	{
		string redirectUrl = $"{_webAppBaseUrl}/app/estimate/{estimateId}/summary";

		Result<string> result = await _nonceService.GenerateAndSaveNonce(entityId, nonceType, ct)
		  .MapResult((string nonce) =>
		  {
			  string viewEstimateLink = $"{_webAppBaseUrl}/login/magic-link/{nonce.ToUrlEncoded()}?redirectUrl={redirectUrl}";
			  return Result<string>.Success(viewEstimateLink);
		  }, ct);

		return result;
	}
}
