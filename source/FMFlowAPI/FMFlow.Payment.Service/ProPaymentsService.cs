using EFRepository;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Integrations.MxMerchant.Interface;
using FMFlow.Integrations.MxMerchant.Interface.DTOs;
using FMFlow.ProPayments.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Refit;

namespace FMFlow.ProPayments.Service;

public class ProPaymentsService(
		IRepository Repository,
		ILogger<ProPaymentsService> Logger,
		IConfiguration Configuration,
		IMxMerchantApi MxMerchantApi) : IPaymentService
{
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

	public async Task<ApiResponse> SavePaymentInfo(PaymentInfoModel info, string onboardingFormStop, CancellationToken ct)
	{
		try
		{
			var foundUser = await GetProUserByID(info.UserID, ct);

			if (foundUser == null)
			{
				return new ApiResponse()
				{
					IsSuccessful = false,
					Message = "User not found"
				};
			}

			long? mxCustomerId = foundUser.Billing?.MerchantID;
			if (mxCustomerId == null)
			{
				var mxCustomer = await CreateCustomer(foundUser, ct);
				mxCustomerId = mxCustomer.Id;
			}

			var vaultedCard = await CreateVaultedCard(info, foundUser, mxCustomerId ?? 0, ct);

			var billingPlan = await GetBillingPlan(info.BillingFrequency, ct);
			var contractSubscription = await CreateContractSubscription(mxCustomerId ?? 0, vaultedCard.Id, billingPlan, info.StartDate, ct);

			// Create or update the Billing record
			var billing = foundUser.Billing ?? new Billing();
			billing.MerchantID = mxCustomerId;
			billing.CardID = vaultedCard.Id;
			billing.VaultedCardToken = vaultedCard.Token;
			billing.ContractID = contractSubscription.Contract.Id;
			billing.StartDate = EnsureUtc(info.StartDate);
			billing.BillingPlanID = billingPlan.BillingPlanID;

			if (foundUser.Billing == null)
			{
				billing.DateCreated = DateTime.UtcNow;
			}

			foundUser.Billing = billing;

			foundUser.OnboardingFormStop = onboardingFormStop;
			Repository.AddOrUpdate(foundUser);
			await Repository.SaveAsync(ct);

			return new ApiResponse()
			{
				IsSuccessful = true,
				Message = "Payment information saved successfully"
			};
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error saving payment information");
			throw;
		}
	}

	/// <summary>
	/// Ensures that a DateTime value is in UTC format.
	/// </summary>
	/// <param name="dateTime">The date time to convert</param>
	/// <returns>A UTC DateTime</returns>
	private static DateTime EnsureUtc(DateTime dateTime)
	{
		if (dateTime.Kind == DateTimeKind.Utc)
		{
			return dateTime;
		}

		// If unspecified or local, convert to UTC
		return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
	}

	public async Task<CreateCustomerResponseDto> CreateCustomer(ProUserDetail user, CancellationToken ct)
	{
		try
		{
			var customerRequest = new CreateCustomerRequestDto
			{
				MerchantId = GetMerchantId(Configuration),
				FirstName = user.FlowUser.FirstName,
				LastName = user.FlowUser.LastName
			};

			var response = await MxMerchantApi.CreateCustomer(
				customerRequest,
				"Basic " + GetMxCredentials(Configuration),
				echo: true,
				ct: ct);

			return response;
		}
		catch (ApiException ex)
		{
			Logger.LogError(ex,
				"API error creating customer. Status: {StatusCode}, Method: {Method}, URI: {Uri}, Headers: {@Headers}",
				ex.StatusCode,
				ex.RequestMessage?.Method,
				ex.RequestMessage?.RequestUri,
				ex.RequestMessage?.Headers);
			throw;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Unexpected error while creating customer");
			throw;
		}
	}

	public async Task<CreateVaultedCardResponseDto> CreateVaultedCard(PaymentInfoModel info, ProUserDetail user, long customerId, CancellationToken ct)
	{
		try
		{
			// Create vaulted card request
			var dateParts = info.ExpirationDate.Split('/');
			var request = new CreateVaultedCardRequestDto
			{
				Number = info.CardNumber,
				ExpiryMonth = dateParts[0],
				ExpiryYear = dateParts[1],
				Cvv = info.CVV,
			};

			if (!info.IsShippingSameAsBilling)
			{
				request.AvsStreet = info.BillingAddress;
				request.AvsZip = info.BillingZipCode;
			}
			else
			{
				request.AvsStreet = user.BusinessAddress?.Line1;
				request.AvsZip = user.BusinessAddress?.ZipCode;
			}

			var response = await MxMerchantApi.CreateVaultedCard(
				request,
				"Basic " + GetMxCredentials(Configuration),
				id: customerId,
				echo: true);

			return response;
		}
		catch (ApiException ex)
		{
			Logger.LogError(ex,
				"API error creating vaulted card. Status: {StatusCode}, Method: {Method}, URI: {Uri}, Headers: {@Headers}",
				ex.StatusCode,
				ex.RequestMessage?.Method,
				ex.RequestMessage?.RequestUri,
				ex.RequestMessage?.Headers);
			throw;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Unexpected error while creating vaulted card");
			throw;
		}
	}

	public async Task<VaultedCard?> GetVaultedCard(long customerId, long cardId, CancellationToken ct)
	{
		try
		{
			var response = await MxMerchantApi.GetVaultedCards(
				"Basic " + GetMxCredentials(Configuration),
				id: customerId,
				ct: ct);

			var card = response.Records?.FirstOrDefault(c => c.Id == cardId);
			return card;
		}
		catch (ApiException ex)
		{
			Logger.LogError(ex,
				"API error retrieving vaulted cards. Status: {StatusCode}, Method: {Method}, URI: {Uri}, Headers: {@Headers}",
				ex.StatusCode,
				ex.RequestMessage?.Method,
				ex.RequestMessage?.RequestUri,
				ex.RequestMessage?.Headers);
			throw;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Unexpected error while retrieving vaulted card. CustomerId: {CustomerId}, CardId: {CardId}", customerId, cardId);
			throw;
		}
	}

	public async Task<CreateContractSubscriptionResponseDto> CreateContractSubscription(
		long customerId,
		long cardAccountId,
		BillingPlan billingPlan,
		DateTime startDate,
		CancellationToken ct)
	{
		try
		{
			// Ensure startDate is in UTC before formatting
			startDate = EnsureUtc(startDate);

			var request = new CreateContractSubscriptionRequestDto
			{
				Contract = new Contract
				{
					Prorated = false,
					Purchases = new List<Purchase>
					{
						new Purchase
						{
							ProductName = billingPlan.Description,
							Price = billingPlan.Amount,
							Quantity = 1
						}
					},
					MerchantId = GetMerchantId(Configuration),
					Frequency = 1,
					Interval = billingPlan.BillingFrequency.ToString(),
					DayNumber = 1,
					TotalAmount = billingPlan.Amount,
					SubTotalAmount = billingPlan.Amount.ToString(),
					DiscountAmount = 0,
					TaxAmount = 0,
					Quantity = "1"
				},
				Subscription = new Subscription
				{
					InvoiceMethod = "Autopay",
					AllowPartialPayment = true,
					EftAgreementRequested = true,
					CustomerId = customerId,
					CardAccountId = cardAccountId,
					Status = "Active",
					StartDate = startDate.ToString("yyyy-MM-dd")
				}
			};

			var response = await MxMerchantApi.CreateContractSubscription(
				request,
				"Basic " + GetMxCredentials(Configuration),
				echo: false);

			return response;
		}
		catch (ApiException ex)
		{
			Logger.LogError(ex,
				"API error creating subscription. Status: {StatusCode}, Method: {Method}, URI: {Uri}, Headers: {@Headers}",
				ex.StatusCode,
				ex.RequestMessage?.Method,
				ex.RequestMessage?.RequestUri,
				ex.RequestMessage?.Headers);
			throw;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Unexpected error while creating subscription");
			throw;
		}
	}

	public async Task<BillingPlan> GetBillingPlan(BillingFrequency billingFrequency, CancellationToken ct)
	{
		try
		{
			// Get all billing plans and filter in memory to avoid Entity Framework translation issues
			var allBillingPlans = Repository.Query<BillingPlan>().AsNoTracking().ToList();
			var billingPlan = allBillingPlans.FirstOrDefault(bp => bp.BillingFrequency == billingFrequency && bp.IsActive);

			if (billingPlan == null)
			{
				throw new InvalidOperationException($"No active billing plan found with frequency: {billingFrequency}");
			}

			return billingPlan;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error retrieving billing plan with frequency {BillingFrequency}", billingFrequency);
			throw;
		}
	}

	public async Task<PaymentInfoModel> GetPaymentInfoByProUserID(int proUserID, CancellationToken ct)
	{
		try
		{
			ProUserDetail? foundUser = await GetProUserByID(proUserID, ct);

			if (foundUser == null || foundUser.Billing == null || foundUser.Billing.MerchantID == null || foundUser.Billing.CardID == null)
			{
				return null;
			}

			var vaultedCard = await GetVaultedCard(foundUser.Billing.MerchantID.Value, foundUser.Billing.CardID.Value, ct);

			if (vaultedCard == null)
			{
				Logger.LogWarning("Vaulted card not found for customer {CustomerId}, card {CardId}", foundUser.Billing.MerchantID, foundUser.Billing.CardID);
				return null;
			}

			var billingPlan = foundUser.Billing.BillingPlan ?? Repository.Query<BillingPlan>()
				.Where(bp => bp.IsActive)
				.AsNoTracking()
				.FirstOrDefault();

			// Ensure the StartDate is valid and in UTC format
			var startDate = foundUser.Billing.StartDate;
			if (startDate == default || startDate.Year < 2000)
			{
				// If StartDate is the default value or invalid, use the current date
				startDate = EnsureUtc(DateTime.Now);
				Logger.LogWarning("Invalid StartDate found for ProUser ID: {ProUserID}, using current date instead", proUserID);
			}
			else
			{
				startDate = EnsureUtc(startDate);
			}

			var paymentInfo = new PaymentInfoModel
			{
				UserID = proUserID,
				NameOnCard = $"{foundUser.FlowUser.GetFullName()}",
				CardNumber = $"************{vaultedCard.Last4}", // Mask all but last 4
				ExpirationDate = $"{vaultedCard.ExpiryMonth}/{vaultedCard.ExpiryYear}",
				CVV = "***", // CVV is never returned from payment processor
				IsShippingSameAsBilling = true, // Default assumption
				BillingAddress = foundUser.BusinessAddress?.Line1 ?? string.Empty,
				BillingCity = foundUser.BusinessAddress?.City ?? string.Empty,
				BillingState = foundUser.BusinessAddress?.State?.StateName ?? string.Empty,
				BillingZipCode = foundUser.BusinessAddress?.ZipCode ?? string.Empty,
				ProductsTotal = billingPlan?.Amount ?? 99M,
				ShippingTotal = 0,
				TotalAmount = billingPlan?.Amount ?? 99M,
				BillingFrequency = billingPlan?.BillingFrequency ?? BillingFrequency.Monthly,
				StartDate = startDate
			};

			return paymentInfo;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error getting payment information for ProUser ID: {ProUserID}", proUserID);
			throw;
		}
	}

	public async Task<ProUserDetail?> GetProUserByID(int proUserID, CancellationToken ct)
	{
		return await Repository.Query<ProUserDetail>()
					.Where(x => x.UserID == proUserID)
					.Include(x => x.FlowUser)
					.Include(u => u.Billing)
					.ThenInclude(b => b.BillingPlan)
					.Include(x => x.BusinessAddress)
					.ThenInclude(a => a.State)
					.FirstOrDefaultAsync(ct);
	}

	public async Task<ApiResponse> UpdatePaymentInfo(PaymentInfoModel info, CancellationToken ct)
	{
		try
		{
			var foundUser = await GetProUserByID(info.UserID, ct);

			if (foundUser == null)
			{
				return new ApiResponse()
				{
					IsSuccessful = false,
					Message = "User not found"
				};
			}

			if (foundUser.Billing == null)
			{
				return new ApiResponse()
				{
					IsSuccessful = false,
					Message = "Payment information not found. Please add payment information first."
				};
			}

			if (foundUser.Billing.ContractID != null)
			{
				// Cancel existing contract
				await MxMerchantApi.CancelContract(
					"Basic " + GetMxCredentials(Configuration),
					foundUser.Billing.ContractID.Value);
			}

			// Delete existing vaulted card
			await MxMerchantApi.DeleteVaultedCard(
				"Basic " + GetMxCredentials(Configuration),
				foundUser.Billing.MerchantID.Value,
				foundUser.Billing.CardID.Value);

			// Save new payment information
			string onboardingFormStop = foundUser.OnboardingFormStop;
			return await SavePaymentInfo(info, onboardingFormStop, ct);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error updating payment information");
			return new ApiResponse()
			{
				IsSuccessful = false,
				Message = $"Error updating payment information: {ex.Message}"
			};
		}
	}
}
