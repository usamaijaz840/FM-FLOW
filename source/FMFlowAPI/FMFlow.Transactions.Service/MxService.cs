using FMFlow.Integrations.MxMerchant.Interface;
using FMFlow.Integrations.MxMerchant.Interface.DTOs;
using FMFlow.Transactions.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Refit;

namespace FMFlow.Transactions.Service;

public class MxService(
	ILogger<MxService> logger,
	IConfiguration configuration,
	IMxMerchantApi mxMerchantApi) : IMxService
{
	private readonly string _authHeader = "Basic " + GetMxCredentials(configuration);
	private readonly long _merchantId = GetMerchantId(configuration);

	private static string GetMxCredentials(IConfiguration configuration)
	{
		var apiKey = configuration["MXConnect:ApiKey"]
			?? throw new InvalidOperationException("MX Connect API Key not configured");

		var apiSecret = configuration["MXConnect:ApiSecret"]
			?? throw new InvalidOperationException("MX Connect API Secret not configured");

		return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));
	}

	private static long GetMerchantId(IConfiguration configuration)
	{
		var merchantIdString = configuration["MXConnect:MerchantId"]
			?? throw new InvalidOperationException("MX Connect Merchant ID not configured");

		if (!long.TryParse(merchantIdString, out var merchantId))
		{
			throw new InvalidOperationException($"Invalid MerchantId format: {merchantIdString}");
		}

		return merchantId;
	}

	private async Task<T> ExecuteWithLoggingAsync<T>(
		Func<Task<T>> action,
		string operation)
	{
		try
		{
			var result = await action();
			logger.LogInformation("Operation {Operation} succeeded.", operation);
			return result;
		}
		catch (ApiException ex)
		{
			logger.LogError(ex,
				"API error during {Operation}. Status: {StatusCode}, Method: {Method}, URI: {Uri}, Headers: {@Headers}",
				operation,
				ex.StatusCode,
				ex.RequestMessage?.Method,
				ex.RequestMessage?.RequestUri,
				ex.RequestMessage?.Headers);

			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unexpected error during {Operation}", operation);
			throw;
		}
	}

	public async Task<CreateCustomerResponseDto> CreateMxCustomerId(CreateCustomerRequestDto request, CancellationToken ct)
	{
		request.MerchantId = _merchantId;
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.CreateCustomer(request, _authHeader, true, ct),
			"CreateCustomer"
		);
	}

	public async Task<CreateVaultedCardResponseDto> CreateVaultedCard(CreateVaultedCardRequestDto request, long customerId, CancellationToken ct)
	{
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.CreateVaultedCard(request, _authHeader, customerId, true, ct),
			"CreateVaultedCard"
		);
	}

	public async Task<GetVaultedCardsResponseDto> GetVaultedCards(long customerId, CancellationToken ct)
	{
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.GetVaultedCards(_authHeader, customerId, ct),
			"GetVaultedCards"
		);
	}

	public async Task<CreateVaultedBankAccountResponseDto> CreateVaultedBankAccount(CreateVaultedBankAccountRequestDto request, long customerId, CancellationToken ct)
	{
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.CreateVaultedBankAccount(request, _authHeader, customerId, true, ct),
			"CreateVaultedCard"
		);
	}

	public async Task<GetVaultedBankAccountsResponseDto> GetVaultedBankAccounts(long customerId, CancellationToken ct)
	{
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.GetVaultedBankAccounts(_authHeader, customerId, ct),
			"GetVaultedBankAccounts"
		);
	}

	public async Task<CreateInvoiceResponseDto> CreateInvoice(CreateInvoiceRequestDto request, CancellationToken ct)
	{
		request.MerchantId = _merchantId;
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.CreateInvoice(request, _authHeader, true, ct),
			"CreateInvoice"
		);
	}

	public async Task<GetInvoicesResponseDto> GetInvoices(int? limit = null, int? offset = null, string? status = null,
		string? dateType = null, string? startDate = null, string? endDate = null, long? customerId = null,
		CancellationToken ct = default)
	{
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.GetInvoices(_authHeader, limit, offset, status, dateType, startDate, endDate, customerId, ct),
			"GetInvoices"
		);
	}

	public async Task<CreateInvoiceResponseDto> GetInvoice(long invoiceId, CancellationToken ct)
	{
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.GetInvoice(_authHeader, invoiceId, ct),
			"GetInvoice"
		);
	}

	public async Task<CreateInvoiceResponseDto> UpdateInvoice(long invoiceId, UpdateInvoiceRequestDto request, CancellationToken ct)
	{
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.UpdateInvoice(request, _authHeader, invoiceId, false, ct),
			"UpdateInvoice"
		);
	}

	public async Task<SendInvoiceReceiptResponseDto> SendInvoiceReceipt(long invoiceId, SendInvoiceReceiptRequestDto request, CancellationToken ct)
	{
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.SendInvoiceReceipt(request, _authHeader, invoiceId, ct),
			"SendInvoiceReceipt"
		);
	}

	public async Task<byte[]> GetInvoiceReceipt(long invoiceId, CancellationToken ct)
	{
		return await ExecuteWithLoggingAsync(
			async () =>
			{
				var response = await mxMerchantApi.GetInvoiceReceipt(_authHeader, invoiceId, ct);
				return await response.Content.ReadAsByteArrayAsync(ct);
			},
			"GetInvoiceReceipt"
		);
	}

	public async Task<InvoicePaymentResponseDto> AddInvoicePayment(long invoiceId, AddInvoicePaymentRequestDto request, CancellationToken ct)
	{
		request.MerchantId = _merchantId;
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.AddInvoicePayment(request, _authHeader, invoiceId, true, ct),
			"AddInvoicePayment"
		);
	}

	public async Task<GetInvoicePaymentsResponseDto> GetInvoicePayments(long invoiceId, CancellationToken ct)
	{
		return await ExecuteWithLoggingAsync(
			() => mxMerchantApi.GetInvoicePayments(_authHeader, invoiceId, ct),
			"GetInvoicePayments"
		);
	}
}
