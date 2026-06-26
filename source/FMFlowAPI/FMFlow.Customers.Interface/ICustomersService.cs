using FMFlow.Customers.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Customers.Interface;

public interface ICustomersService
{
	Task<Result> FinalizeCustomerActivation(CustomerRequestDto customerRequest, CancellationToken ct);
	Task<Result<NonceResponseDto>> VerifyNonce(NonceRequestDto request, CancellationToken ct);
	Task<Result<CustomerLeadResponseDto>> CreateCustomerLead(CustomerLeadRequestDto request, CancellationToken ct);
}
