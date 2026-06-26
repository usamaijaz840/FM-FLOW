using EFRepository;
using FluentValidation;
using FMFlow.AccessValidation;
using FMFlow.Common;
using FMFlow.Email.Interface;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.Integrations.MxMerchant.Interface.DTOs;
using FMFlow.Transactions.Interface;
using FMFlow.Transactions.Interface.DTOs;
using FMFlow.Transactions.Interface.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FMFlow.Transactions.Service;

public class TransactionsService(
	IRepository repository,
	IMxService mxService,
	ICurrentUserService currentUserService,
	IAccessValidator accessValidator,
	IValidator<PaymentRequestDto> paymentRequestValidator,
	IValidator<DiscountRequestDto> discountRequestValidator,
	IJobCompletionService jobCompletionService,
	IEmailSenderService emailSenderService,
	ILogger<TransactionsService> logger) : ITransactionsService
{
	public async Task<Result<PaymentResponseDto>> CreateEstimatePayment(int estimateId, PaymentRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, paymentRequestValidator, ct);

		if (!requestValidation.IsSuccess)
			return Result<PaymentResponseDto>.Failure(requestValidation.Error!);

		if (request.PaymentMethod == PaymentMethod.Manual &&
			!currentUserService.IsSuperAdmin() &&
			!currentUserService.IsAccountManager() &&
			!currentUserService.IsPro())
			return Result<PaymentResponseDto>.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);

		var estimate = await repository.Query<Estimate>()
			.ByEstimateID(estimateId)
			.Include(e => e.Job)
			.Include(e => e.ProUser)
			.Include(e => e.RequestedEstimate)
				.ThenInclude(re => re.Project)
					.ThenInclude(p => p.Lead)
						.ThenInclude(l => l.Customer)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<PaymentResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		if (estimate.Status != EstimateStatus.Approved)
			return Result<PaymentResponseDto>.Failure("Estimate must be approved for creating a payment.",
				ResultErrorType.BadRequest);


		// Ensure Total is set (default to 0 if not calculated)
		if (!estimate.Total.HasValue)
			estimate.Total = 0;

		decimal remainingAmount = estimate.Total!.Value - estimate.PaidAmount;

		if (request.Amount > remainingAmount)
			return Result<PaymentResponseDto>.Failure(
				$"Amount paid in this transaction exceeds the remaining amount ({remainingAmount}) for this estimate.",
				ResultErrorType.BadRequest);

		var transaction = PaymentMapper.MapRequestToTransaction(request, estimateId);

		ct.ThrowIfCancellationRequested();

		if (request.PaymentMethod == PaymentMethod.ACH ||
			request.PaymentMethod == PaymentMethod.CC)
		{
			var mxCustomerId = await GetOrCreateMxCustomerId(estimate, ct);

			var invoiceRequest = new CreateInvoiceRequestDto
			{
				Customer = new Customer { Id = mxCustomerId },
				Status = "Unpaid",
				Purchases =
				[
					new InvoicePurchase
					{
						ProductName = estimate.RequestedEstimate.Name,
						Quantity = 1,
						Price = request.Amount
					}
				]
			};

			var invoicePaymentRequest = await BuildInvoicePaymentRequestDto(request, mxCustomerId, ct);

			if (!invoicePaymentRequest.IsSuccess || invoicePaymentRequest.Value == null)
				return Result<PaymentResponseDto>.Failure(invoicePaymentRequest.Error!);

			var invoiceResponse = await mxService.CreateInvoice(invoiceRequest, ct);

			var invoicePaymentResponse = await mxService.AddInvoicePayment(invoiceResponse.Id, invoicePaymentRequest.Value, ct);

			if (request.PaymentMethod == PaymentMethod.ACH && request.SavePaymentMethod)
			{
				var vaultBankAccountRequest = new CreateVaultedBankAccountRequestDto
				{
					AccountNumber = request.BankAccountNumber,
					RoutingNumber = request.BankAccountRoutingNumber,
					Name = request.BankAccountName,
					Type = request.BankAccountType.ToString()
				};

				await mxService.CreateVaultedBankAccount(vaultBankAccountRequest, mxCustomerId, ct);
			}

			// Capture response details from payment processor
			transaction.ResponseMessage = invoicePaymentResponse.ResponseMessage;
			transaction.AuthMessage = invoicePaymentResponse.AuthMessage;

			if (Enum.TryParse(invoicePaymentResponse.Status, true, out TxStatus statusEnum))
			{
				transaction.Status = statusEnum;
			}
			else
			{
				transaction.Status = TxStatus.Unknown;
			}

			// Log payment failures with detailed error information
			if (transaction.Status == TxStatus.Declined || transaction.Status == TxStatus.Unknown)
			{
				logger.LogWarning(
					"Payment declined or failed for Estimate {EstimateId}. " +
					"Status: {Status}, ResponseMessage: {ResponseMessage}, AuthMessage: {AuthMessage}, " +
					"ReferenceNumber: {ReferenceNumber}, AuthorizationCode: {AuthorizationCode}",
					estimateId,
					transaction.Status,
					transaction.ResponseMessage ?? "N/A",
					transaction.AuthMessage ?? "N/A",
					invoicePaymentResponse.ReferenceNumber ?? "N/A",
					invoicePaymentResponse.AuthorizationCode ?? "N/A"
				);
			}
		}
		else
		{
			transaction.Status = TxStatus.Settled;
		}

		repository.AddNew(transaction);

		await repository.SaveAsync(ct);

		if (estimate.Job != null)
			await jobCompletionService.CloseJobIfEligible(estimate.Job.JobId, ct);

		await emailSenderService.SendEmailProAdvancedPayment(estimate, request.Amount, ct);

		var estimateHasBeenPaid = await repository.Query<Estimate>()
			.ByEstimateID(estimateId)
			.Select(e => e.HasBeenPaid)
			.FirstAsync(ct);

		if (estimateHasBeenPaid)
		{
			await emailSenderService.SendEmailCustomerPaymentSuccessful(estimate, request.Amount, ct);
			await emailSenderService.SendEmailProEstimateFullyPaid(estimate, ct);
		}

		var response = PaymentMapper.MapTransactionToPaymentResponse(transaction);

		return Result<PaymentResponseDto>.Success(response);
	}

	private async Task<long> GetOrCreateMxCustomerId(Estimate estimate, CancellationToken ct)
	{
		var customer = await repository.Query<Estimate>()
			.Where(e => e.EstimateID == estimate.EstimateID)
			.Select(e => e.RequestedEstimate.Project.Lead.Customer)
			.FirstAsync(ct);

		if (customer.MxCustomerId.HasValue)
			return customer.MxCustomerId.Value;

		var response = await mxService.CreateMxCustomerId(new CreateCustomerRequestDto
		{
			FirstName = customer.FirstName,
			LastName = customer.LastName
		}, ct);

		customer.MxCustomerId = response.Id;

		await repository.SaveAsync(ct);

		return response.Id;
	}

	private async Task<Result<AddInvoicePaymentRequestDto>> BuildInvoicePaymentRequestDto(PaymentRequestDto request, long mxCustomerId, CancellationToken ct)
	{
		AddInvoicePaymentRequestDto? addInvoicePaymentRequestDto = null;

		MxVaultedAccount? vaultedAccount = null;

		if (request.VaultedAccountId != null)
		{
			vaultedAccount = await repository.Query<MxVaultedAccount>()
				.ByAccountId(request.VaultedAccountId.Value)
				.ByMxCustomerId(mxCustomerId)
				.ByExpirationTimeIsAfter(DateTime.UtcNow)
				.FirstOrDefaultAsync(ct);

			if (vaultedAccount == null)
			{
				return Result<AddInvoicePaymentRequestDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);
			}
		}

		if (request.PaymentMethod == PaymentMethod.CC)
		{
			addInvoicePaymentRequestDto = new AddInvoicePaymentRequestDto
			{
				TenderType = "Card",
				Amount = request.Amount,
				ShouldVaultCard = request.SavePaymentMethod,
			};

			if (vaultedAccount != null)
			{
				addInvoicePaymentRequestDto.CardAccount = new InvoicePaymentCardAccount
				{
					Token = vaultedAccount.Token
				};
			}
			else
			{
				addInvoicePaymentRequestDto.CardAccount = new InvoicePaymentCardAccount
				{
					Number = request.CardNumber!,
					ExpiryMonth = request.CardExpiryMonth!,
					ExpiryYear = request.CardExpiryYear!,
					Cvv = request.CardCvv!,
					AvsStreet = request.CardAddressStreet!,
					AvsZip = request.CardAddressZipCode!
				};
			}
		}

		if (request.PaymentMethod == PaymentMethod.ACH)
		{
			addInvoicePaymentRequestDto = new AddInvoicePaymentRequestDto
			{
				TenderType = "ACH",
				Amount = request.Amount,
				EntryClass = "WEB"
			};

			if (vaultedAccount != null)
			{
				addInvoicePaymentRequestDto.BankAccount = new InvoicePaymentBankAccount
				{
					Token = vaultedAccount.Token
				};
			}
			else
			{
				addInvoicePaymentRequestDto.BankAccount = new InvoicePaymentBankAccount
				{
					Name = request.BankAccountName!,
					AccountNumber = request.BankAccountNumber!,
					RoutingNumber = request.BankAccountRoutingNumber!,
					Type = request.BankAccountType.ToString()!
				};
			}
		}

		if (addInvoicePaymentRequestDto == null)
			return Result<AddInvoicePaymentRequestDto>.Failure("Unable to build payment request.", ResultErrorType.BadRequest);

		return Result<AddInvoicePaymentRequestDto>.Success(addInvoicePaymentRequestDto);
	}

	public async Task<Result<SearchResult<PaymentResponseDto>>> SearchEstimatePayments(
		int estimateId,
		TxStatus? status,
		int pageIndex,
		int pageSize,
		CancellationToken ct)
	{
		if (pageIndex < 0 || pageSize < 1)
			return Result<SearchResult<PaymentResponseDto>>.Failure("Invalid pagination parameters.");

		pageSize = Math.Min(pageSize, 50);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimateId, ct);

		if (!accessResult.IsSuccess)
			return Result<SearchResult<PaymentResponseDto>>.Failure(accessResult.Error!, accessResult.ErrorType);

		var query = repository.Query<Transaction>()
			.ByEstimateId(estimateId);

		if (status != null)
			query = query.Where(t => t.Status == status);

		var totalResults = await query.CountAsync(ct);

		var pagedTransactions = await query
			.OrderByDescending(t => t.PaymentDate)
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.ToListAsync(ct);

		var response = pagedTransactions
			.Select(PaymentMapper.MapTransactionToPaymentResponse)
			.ToList();

		var searchResult = new SearchResult<PaymentResponseDto>(response, totalResults);

		return Result<SearchResult<PaymentResponseDto>>.Success(searchResult);
	}

	public async Task<Result<PaymentResponseDto>> ApplyDiscount(int estimateId, DiscountRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, discountRequestValidator, ct);

		if (!requestValidation.IsSuccess)
			return Result<PaymentResponseDto>.Failure(requestValidation.Error!);

		var estimate = await repository.Query<Estimate>()
			.ByEstimateID(estimateId)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<PaymentResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimateId, ct);

		if (!accessResult.IsSuccess)
			return Result<PaymentResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		if (estimate.Status != EstimateStatus.Approved)
			return Result<PaymentResponseDto>.Failure("Transactional discounts can only be applied to approved estimates.",
				ResultErrorType.BadRequest);

		// Check if estimate is already fully paid
		if (estimate.HasBeenPaid)
			return Result<PaymentResponseDto>.Failure("Cannot apply discount to a fully paid estimate.",
				ResultErrorType.BadRequest);

		// Calculate the remaining amount that can be discounted
		var remainingAmount = estimate.Total!.Value - estimate.PaidAmount;

		if (remainingAmount <= 0)
			return Result<PaymentResponseDto>.Failure("Cannot apply discount as the estimate is already fully paid.",
				ResultErrorType.BadRequest);

		decimal discountAmount = request.DiscountType switch
		{
			DiscountType.Percentage => Math.Round(remainingAmount * request.Discount / 100, 2, MidpointRounding.AwayFromZero),
			DiscountType.FlatAmount => Math.Round(request.Discount, 2, MidpointRounding.AwayFromZero),
			_ => 0m
		};

		// Cap discount so total credits (paid amount + discount) never exceed estimate total
		// This prevents negative outstanding balances
		if (discountAmount > remainingAmount)
			discountAmount = remainingAmount;

		var discountTransaction = new Transaction
		{
			Credit = discountAmount,
			EstimateId = estimateId,
			Description = "Discount Applied",
			PaymentDate = DateTime.UtcNow,
			PaymentMethod = PaymentMethod.Discount,
			Status = TxStatus.Settled
		};

		repository.AddNew(discountTransaction);

		await repository.SaveAsync(ct);

		var response = PaymentMapper.MapTransactionToPaymentResponse(discountTransaction);

		return Result<PaymentResponseDto>.Success(response);
	}

	public async Task<Result<List<PaymentMethodResponseDto>>> GetPaymentMethods(int customerId, CancellationToken ct)
	{
		var customer = await repository.Query<FlowUser>()
			.ByUserID(customerId)
			.FirstOrDefaultAsync(ct);

		if (customer == null || !customer.MxCustomerId.HasValue)
		{
			return Result<List<PaymentMethodResponseDto>>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);
		}

		if (currentUserService.IsCustomer())
		{
			if (customer.UserID != currentUserService.GetUserID())
			{
				return Result<List<PaymentMethodResponseDto>>.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
			}
		}

		if (currentUserService.IsPro())
		{
			var proEstimateForCustomerExists = await repository.Query<Estimate>()
				.ByProUserID(currentUserService.GetUserID())
				.Where(e => e.Status == EstimateStatus.Approved ||
							e.Status == EstimateStatus.InProgress ||
							e.Status == EstimateStatus.Completed)
				.Where(e => e.RequestedEstimate.Project.Lead.Customer.UserID == customerId)
				.AnyAsync(ct);

			if (!proEstimateForCustomerExists)
			{
				return Result<List<PaymentMethodResponseDto>>.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
			}
		}

		var mxVaultedCards = await mxService.GetVaultedCards(customer.MxCustomerId.Value, ct);
		var mxVaultedBankAccounts = await mxService.GetVaultedBankAccounts(customer.MxCustomerId.Value, ct);

		var savedAccounts = await RefreshMxVaultedAccounts(mxVaultedCards, mxVaultedBankAccounts, customer.MxCustomerId.Value, ct);

		var paymentMethods = MapMxResponse(mxVaultedCards, mxVaultedBankAccounts, savedAccounts);

		return Result<List<PaymentMethodResponseDto>>.Success(paymentMethods);
	}

	private async Task<List<MxVaultedAccount>> RefreshMxVaultedAccounts(
		GetVaultedCardsResponseDto mxVaultedCards,
		GetVaultedBankAccountsResponseDto mxVaultedBankAccounts,
		long mxCustomerId,
		CancellationToken ct)
	{
		await repository.Query<MxVaultedAccount>()
			.Where(a => a.MxCustomerId == mxCustomerId)
			.ExecuteDeleteAsync(ct);

		var newAccounts = new List<MxVaultedAccount>();

		if (mxVaultedCards.Records is { Length: > 0 })
		{
			foreach (var cardAccount in mxVaultedCards.Records)
			{
				newAccounts.Add(new MxVaultedAccount
				{
					MxInternalAccountId = cardAccount.Id,
					MxCustomerId = mxCustomerId,
					Token = cardAccount.Token
				});
			}
		}

		if (mxVaultedBankAccounts.Records is { Length: > 0 })
		{
			foreach (var bankAccount in mxVaultedBankAccounts.Records)
			{
				newAccounts.Add(new MxVaultedAccount
				{
					MxInternalAccountId = bankAccount.Id,
					MxCustomerId = mxCustomerId,
					Token = bankAccount.Token
				});
			}
		}

		foreach (var account in newAccounts)
		{
			repository.AddNew(account);
		}

		await repository.SaveAsync(ct);

		return newAccounts;
	}

	private static List<PaymentMethodResponseDto> MapMxResponse(
		GetVaultedCardsResponseDto mxVaultedCards,
		GetVaultedBankAccountsResponseDto mxVaultedBankAccounts,
		List<MxVaultedAccount> savedAccounts)
	{
		var paymentMethods = new List<PaymentMethodResponseDto>();

		if (mxVaultedCards.Records != null)
		{
			paymentMethods.AddRange(
				mxVaultedCards.Records.Select(card => new PaymentMethodResponseDto(
					Id: savedAccounts.First(sa => sa.MxInternalAccountId == card.Id).AccountId,
					PaymentMethodType: PaymentMethod.CC,
					Last4Digits: card.Last4,
					BankAccountName: null,
					CardType: card.CardType
				))
			);
		}

		if (mxVaultedBankAccounts.Records != null)
		{
			paymentMethods.AddRange(
				mxVaultedBankAccounts.Records.Select(bankAccount => new PaymentMethodResponseDto(
					Id: savedAccounts.First(sa => sa.MxInternalAccountId == bankAccount.Id).AccountId,
					PaymentMethodType: PaymentMethod.ACH,
					Last4Digits: bankAccount.Last4,
					BankAccountName: bankAccount.Name,
					CardType: null
				))
			);
		}

		return paymentMethods;
	}
}
