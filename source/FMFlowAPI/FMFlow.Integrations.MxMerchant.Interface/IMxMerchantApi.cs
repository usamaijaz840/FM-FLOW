using FMFlow.Integrations.MxMerchant.Interface.DTOs;
using Refit;

namespace FMFlow.Integrations.MxMerchant.Interface;

public interface IMxMerchantApi
{
	// Customer API Endpoints
	[Post("/checkout/v3/customer")]
	[Headers("Content-Type: application/json", "Accept: application/json", "User-Agent: FMFlow-API")]
	Task<CreateCustomerResponseDto> CreateCustomer(
		[Body] CreateCustomerRequestDto request,
		[Header("Authorization")] string authorization,
		[Query] bool echo = true,
		CancellationToken ct = default);

	[Post("/checkout/v3/customercardaccount")]
	[Headers("Content-Type: application/json", "Accept: application/json", "User-Agent: FMFlow-API")]
	Task<CreateVaultedCardResponseDto> CreateVaultedCard(
		[Body] CreateVaultedCardRequestDto request,
		[Header("Authorization")] string authorization,
		[Query] long id,
		[Query] bool echo = true,
		CancellationToken ct = default);

	[Get("/checkout/v3/customercardaccount")]
	[Headers("Accept: application/json", "User-Agent: FMFlow-API")]
	Task<GetVaultedCardsResponseDto> GetVaultedCards(
		[Header("Authorization")] string authorization,
		[Query] long id,
		CancellationToken ct = default);

	[Post("/checkout/v3/customerbankaccount")]
	[Headers("Content-Type: application/json", "Accept: application/json", "User-Agent: FMFlow-API")]
	Task<CreateVaultedBankAccountResponseDto> CreateVaultedBankAccount(
		[Body] CreateVaultedBankAccountRequestDto request,
		[Header("Authorization")] string authorization,
		[Query] long id,
		[Query] bool echo = true,
		CancellationToken ct = default);

	[Get("/checkout/v3/customerbankaccount")]
	[Headers("Accept: application/json", "User-Agent: FMFlow-API")]
	Task<GetVaultedBankAccountsResponseDto> GetVaultedBankAccounts(
		[Header("Authorization")] string authorization,
		[Query] long id,
		CancellationToken ct = default);

	// Contract API Endpoints
	[Post("/checkout/v3/contractsubscription")]
	[Headers("Content-Type: application/json", "Accept: application/json", "User-Agent: FMFlow-API")]
	Task<CreateContractSubscriptionResponseDto> CreateContractSubscription(
		[Body] CreateContractSubscriptionRequestDto request,
		[Header("Authorization")] string authorization,
		[Query] bool echo = false,
		CancellationToken ct = default);

	[Get("/checkout/v3/contract/{contractId}")]
	[Headers("Accept: application/json", "User-Agent: FMFlow-API")]
	Task<GetContractResponseDto> GetContract(
		[Header("Authorization")] string authorization,
		[AliasAs("contractId")] long contractId,
		CancellationToken ct = default);

	[Put("/checkout/v3/customercardaccount")]
	[Headers("Content-Type: application/json", "Accept: application/json", "User-Agent: FMFlow-API")]
	Task<HttpResponseMessage> UpdateVaultedCardAccount(
		[Body] UpdateVaultedCardAccountRequestDto request,
		[Header("Authorization")] string authorization,
		[Query] long id,
		[Query] long subId,
		CancellationToken ct = default);

	[Delete("/checkout/v3/contract")]
	[Headers("Accept: application/json", "User-Agent: FMFlow-API")]
	Task<HttpResponseMessage> CancelContract(
		[Header("Authorization")] string authorization,
		[Query] long id,
		CancellationToken ct = default);

	[Delete("/checkout/v3/customercardaccount")]
	[Headers("Accept: application/json", "User-Agent: FMFlow-API")]
	Task<HttpResponseMessage> DeleteVaultedCard(
		[Header("Authorization")] string authorization,
		[Query] long id,
		[Query] long subId,
		CancellationToken ct = default);

	// Invoices API Endpoints
	[Post("/checkout/v3/invoice")]
	[Headers("Content-Type: application/json", "Accept: application/json", "User-Agent: FMFlow-API")]
	Task<CreateInvoiceResponseDto> CreateInvoice(
		[Body] CreateInvoiceRequestDto request,
		[Header("Authorization")] string authorization,
		[Query] bool echo = true,
		CancellationToken ct = default);

	[Get("/checkout/v3/invoice")]
	[Headers("Accept: application/json", "User-Agent: FMFlow-API")]
	Task<GetInvoicesResponseDto> GetInvoices(
		[Header("Authorization")] string authorization,
		[Query] int? limit = null,
		[Query] int? offset = null,
		[Query] string? status = null,
		[Query] string? dateType = null,
		[Query] string? startDate = null,
		[Query] string? endDate = null,
		[Query] long? customerId = null,
		CancellationToken ct = default);

	[Get("/checkout/v3/invoice/{invoiceId}")]
	[Headers("Accept: application/json", "User-Agent: FMFlow-API")]
	Task<CreateInvoiceResponseDto> GetInvoice(
		[Header("Authorization")] string authorization,
		[AliasAs("invoiceId")] long invoiceId,
		CancellationToken ct = default);

	[Put("/checkout/v3/invoice/{invoiceId}")]
	[Headers("Content-Type: application/json", "Accept: application/json", "User-Agent: FMFlow-API")]
	Task<CreateInvoiceResponseDto> UpdateInvoice(
		[Body] UpdateInvoiceRequestDto request,
		[Header("Authorization")] string authorization,
		[AliasAs("invoiceId")] long invoiceId,
		[Query] bool echo = false,
		CancellationToken ct = default);

	[Post("/checkout/v3/invoice/{invoiceId}/receipt")]
	[Headers("Content-Type: application/json", "Accept: application/json", "User-Agent: FMFlow-API")]
	Task<SendInvoiceReceiptResponseDto> SendInvoiceReceipt(
		[Body] SendInvoiceReceiptRequestDto request,
		[Header("Authorization")] string authorization,
		[AliasAs("invoiceId")] long invoiceId,
		CancellationToken ct = default);

	[Get("/checkout/v3/invoice/{invoiceId}/receipt")]
	[Headers("Accept: application/json", "User-Agent: FMFlow-API")]
	Task<HttpResponseMessage> GetInvoiceReceipt(
		[Header("Authorization")] string authorization,
		[AliasAs("invoiceId")] long invoiceId,
		CancellationToken ct = default);

	[Post("/checkout/v3/invoicepayment")]
	[Headers("Content-Type: application/json", "Accept: application/json", "User-Agent: FMFlow-API")]
	Task<InvoicePaymentResponseDto> AddInvoicePayment(
		[Body] AddInvoicePaymentRequestDto request,
		[Header("Authorization")] string authorization,
		[Query] long id,
		[Query] bool echo = true,
		CancellationToken ct = default);

	[Get("/checkout/v3/invoicepayment/{invoiceId}")]
	[Headers("Accept: application/json", "User-Agent: FMFlow-API")]
	Task<GetInvoicePaymentsResponseDto> GetInvoicePayments(
		[Header("Authorization")] string authorization,
		[AliasAs("invoiceId")] long invoiceId,
		CancellationToken ct = default);
}



