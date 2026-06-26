using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FMFlow.Data;
using FMFlow.Entities;
using EFRepository;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Common;

public class NonceService : INonceService
{
	private readonly IRepository _repository;
	private readonly NonceSettings _nonceSettings;

	private static readonly RandomNumberGenerator random = RandomNumberGenerator.Create();

	public NonceService(IRepository repository, IOptions<NonceSettings> nonceSettings)
	{
		_repository = repository;
		_nonceSettings = nonceSettings.Value;
	}

	public async Task<Result<string>> GenerateAndSaveNonce(int entityId, NonceType type, CancellationToken ct)
	{
		if (_nonceSettings == null || _nonceSettings.NonceConfigurations == null)
			return Result<string>.Failure("Nonce settings are not configured properly.", ResultErrorType.BadRequest);

		if (!_nonceSettings.NonceConfigurations.TryGetValue(type, out NonceConfiguration? config))
			return Result<string>.Failure($"No configuration found for nonce type {type}", ResultErrorType.BadRequest);

		int nonceLengthInBytes = config.NonceLengthInBytes;

		byte[] data = new byte[nonceLengthInBytes];
		random.GetNonZeroBytes(data);

		string nonce = Convert.ToBase64String(data);

		var nonceObject = new Nonce
		{
			Value = nonce,
			EntityId = entityId,
			CreatedAt = DateTime.UtcNow,
			Consumed = false,
			Type = type
		};

		nonceObject.SetExpiration(config.Expiration);

		_repository.AddNew(nonceObject);
		await _repository.SaveAsync(ct);

		return Result<string>.Success(nonce);
	}

	public async Task<Result> ValidateNonce(string nonce, CancellationToken ct)
	{
		Result result = await ValidateNonceInternal(nonce, ct).ToResult(ct);
		return result;
	}

	private async Task<Result<Nonce?>> ValidateNonceInternal(string nonce, CancellationToken ct)
	{
		// First check if nonce exists at all (regardless of consumed status)
		Nonce? nonceObject = await _repository.Query<Nonce>()
			.Where(n => n.Value == nonce)
			.FirstOrDefaultAsync(ct);

		if (nonceObject == null)
			return Result<Nonce?>.Failure("Nonce not found.", ResultErrorType.NotFound);

		if (nonceObject.Consumed)
			return Result<Nonce?>.Failure("Nonce has already been used.", ResultErrorType.BadRequest);

		if (nonceObject.IsExpired)
		{
			// Don't delete magic link nonces to allow for resend functionality at any time
			// Only delete other nonce types (e.g., password reset) for security
			//
			// FUTURE: Consider implementing a background cleanup job to delete magic link nonces older than 90 days or so
			bool isMagicLinkNonce = nonceObject.Type == NonceType.CustomerMagicLink || 
			                        nonceObject.Type == NonceType.EstimateRecipientMagicLink;
			
			if (!isMagicLinkNonce)
			{
				_repository.Delete(nonceObject);
				await _repository.SaveAsync(ct);
			}
			
			return Result<Nonce?>.Failure("Nonce has expired.", ResultErrorType.BadRequest);
		}

		return Result<Nonce?>.Success(nonceObject);
	}

	public async Task<Result<Nonce>> ValidateAndConsumeNonce(string nonce, CancellationToken ct)
	{
		Result<Nonce> result = await ValidateNonceInternal(nonce, ct)
			.MapResult(ConsumeNonce, ct);

		return result;
	}

	private async Task<Result<Nonce>> ConsumeNonce(Nonce? nonce, CancellationToken ct)
	{
		if (nonce == null)
			return Result<Nonce>.Failure("Nonce is null.", ResultErrorType.BadRequest);

		nonce.Consumed = true;
		nonce.ConsumedAt = DateTime.UtcNow;

		await _repository.SaveAsync(ct);

		return Result<Nonce>.Success(nonce);
	}
}
