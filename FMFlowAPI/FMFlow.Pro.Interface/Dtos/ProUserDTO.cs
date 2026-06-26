namespace FMFlow.Pro.Interface.Dtos;

public record ProUserDto
{
	public int? UserID { get; set; }
	public string? FirstName { get; set; }
	public string? LastName { get; set; }
	public string? Email { get; set; }
	public string? Phone { get; set; }
	public string? BusinessName { get; set; }
	public string? BusinessAddress { get; set; }
	public string? City { get; set; }
	public string? State { get; set; }
	public string? ZipCode { get; set; }
	public string? TimeZone { get; set; }
	public string? Description { get; set; }
	public string? GoogleReview { get; set; }
	public string? YelpReview { get; set; }
	public string[]? SizeOfJob { get; set; }
	public string[]? Services { get; set; }
	public string? AddressOfStore { get; set; }
	public string? CityOfStore { get; set; }
	public string? StateOfStore { get; set; }
	public string? ZipCodeOfStore { get; set; }
	public string? BusinessType { get; set; }
	public string? TaxID { get; set; }
	public string? NumberOfEmployees { get; set; }
	public string? OnboardingFormStop { get; set; }
	public bool IsApproved { get; set; }
	public bool? RequestedReferrals { get; set; }
	public long MerchantID { get; set; }
	public DateTimeOffset? InsuranceExpDate { get; set; }
	public DateTimeOffset DateCreated { get; set; }
	public DateTimeOffset? DateUpdated { get; set; }
	public bool IsDeleted { get; set; }
	public DateTimeOffset? DateDeleted { get; set; }
	public string? ReCaptchaToken { get; set; }
}
