namespace FMFlow.ProUser;

// FUTURE: Not currently being used, but would be best practice to use instead of ProUserDto in UpdatePro method to limit what can be updated.
public record UpdateProUser(
	int UserID,
	string FirstName,
	string LastName,
	string Email,
	string PhoneNumber,
	string BusinessType,
	string TaxID,
	string NumberOfEmployees,
	string?[]? SizeOfJob,
	string?[]? Services,
	string? AddressOfStore,
	string? ZipCodeOfStore,
	string? BusinessName,
	string? BusinessAddress,
	string City,
	string State,
	string ZipCode,
	bool IsApproved,
	bool? RequestedReferrals,
	string? Description,
	string? GoogleReview,
	string? YelpReview,
	DateTimeOffset? InsuranceExpDate,
	long? MerchantID,
	bool IsAccountManager);
