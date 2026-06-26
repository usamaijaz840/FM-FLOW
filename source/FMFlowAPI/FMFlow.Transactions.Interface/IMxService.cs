using FMFlow.Integrations.MxMerchant.Interface.DTOs;

namespace FMFlow.Transactions.Interface;

public interface IMxService
{
	Task<CreateCustomerResponseDto> CreateMxCustomerId(CreateCustomerRequestDto request, CancellationToken ct);

	Task<CreateVaultedCardResponseDto> CreateVaultedCard(CreateVaultedCardRequestDto request, long customerId, CancellationToken ct);

	Task<GetVaultedCardsResponseDto> GetVaultedCards(long customerId, CancellationToken ct);

	Task<CreateVaultedBankAccountResponseDto> CreateVaultedBankAccount(CreateVaultedBankAccountRequestDto request, long customerId, CancellationToken ct);

	Task<GetVaultedBankAccountsResponseDto> GetVaultedBankAccounts(long customerId, CancellationToken ct);

	Task<CreateInvoiceResponseDto> CreateInvoice(CreateInvoiceRequestDto request, CancellationToken ct);

	Task<GetInvoicesResponseDto> GetInvoices(int? limit = null, int? offset = null, string? status = null,
		string? dateType = null, string? startDate = null, string? endDate = null, long? customerId = null,
		CancellationToken ct = default);

	Task<CreateInvoiceResponseDto> GetInvoice(long invoiceId, CancellationToken ct);

	Task<CreateInvoiceResponseDto> UpdateInvoice(long invoiceId, UpdateInvoiceRequestDto request, CancellationToken ct);

	Task<SendInvoiceReceiptResponseDto> SendInvoiceReceipt(long invoiceId, SendInvoiceReceiptRequestDto request, CancellationToken ct);

	Task<byte[]> GetInvoiceReceipt(long invoiceId, CancellationToken ct);

	Task<InvoicePaymentResponseDto> AddInvoicePayment(long invoiceId, AddInvoicePaymentRequestDto request, CancellationToken ct);

	Task<GetInvoicePaymentsResponseDto> GetInvoicePayments(long invoiceId, CancellationToken ct);
}
