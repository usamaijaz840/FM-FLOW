using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Transactions.Interface.DTOs;

namespace FMFlow.Transactions.Interface;

public interface ITransactionsService
{
	Task<Result<PaymentResponseDto>> CreateEstimatePayment(int estimateId, PaymentRequestDto request, CancellationToken ct);

	Task<Result<SearchResult<PaymentResponseDto>>> SearchEstimatePayments(int estimateId, TxStatus? status, int pageIndex, int pageSize, CancellationToken ct);

	Task<Result<PaymentResponseDto>> ApplyDiscount(int estimateId, DiscountRequestDto request, CancellationToken ct);

	Task<Result<List<PaymentMethodResponseDto>>> GetPaymentMethods(int customerId, CancellationToken ct);
}
