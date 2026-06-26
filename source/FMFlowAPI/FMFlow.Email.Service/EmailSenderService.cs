using EFRepository;
using FMFlow.Common;
using FMFlow.Email.Interface;
using FMFlow.Email.Interface.Templates;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace FMFlow.Email.Service;

public class EmailSenderService(
	IEmailService emailService,
	IOptions<EmailStaticDataSettings> staticData,
	IOptions<EmailTemplateIdsSettings> templates,
	IRepository repository,
	IConfiguration configuration) : IEmailSenderService
{
	private readonly EmailStaticDataSettings staticDataSettings = staticData.Value;
	private readonly EmailTemplateIdsSettings templateIdsSettings = templates.Value;
	private readonly string apiBaseUrl = configuration["ApiBaseUrl"] ?? string.Empty;
	private readonly string webAppBaseUrl = configuration["WebAppBaseUrl"] ?? string.Empty;

	private readonly string currentYear = DateTime.UtcNow.Year.ToString();
	private const string templateIdError = "Template Id not found";

	public async Task<Result> SendEmailProAdvancedPayment(Estimate estimate, decimal amount, CancellationToken ct)
	{
		var templateId = templateIdsSettings.ProAdvancedPayment;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		string customerFullName = CustomerInfoHelper.GetCustomerFullName(estimate.RequestedEstimate.Project.Lead);

		var templateData = new ProAdvancedPayment(
			estimate.ProUser.GetFullName(),
			estimate.RequestedEstimate.Name,
			customerFullName,
			customerFullName, // how is client_name different than customer_name?
			amount.ToString("F2"),
			estimate.EstimateID.ToString(),
			estimate.Total!.Value.ToString("F2"),
			staticDataSettings.LoginUrl,
			staticDataSettings.LogoUrl,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl,
			currentYear);

		return await emailService.SendTemplateEmail(estimate.ProUser.Email, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailProCustomerApproved(Estimate estimate, CancellationToken ct)
	{
		var templateId = templateIdsSettings.ProCustomerApproved;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		var templateData = new ProCustomerApproved(
			estimate.ProUser.GetFullName(),
			estimate.RequestedEstimate.Name,
			CustomerInfoHelper.GetCustomerFullName(estimate.RequestedEstimate.Project.Lead),
			estimate.EstimateID.ToString(),
			estimate.Total!.Value.ToString("F2"),
			staticDataSettings.LoginUrl,
			staticDataSettings.LogoUrl,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl,
			currentYear);

		return await emailService.SendTemplateEmail(estimate.ProUser.Email, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailProEstimateFullyPaid(Estimate estimate, CancellationToken ct)
	{
		var templateId = templateIdsSettings.ProEstimateFullyPaid;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		var templateData = new ProEstimateFullyPaid(
			estimate.ProUser.GetFullName(),
			estimate.RequestedEstimate.Name,
			CustomerInfoHelper.GetCustomerFullName(estimate.RequestedEstimate.Project.Lead),
			estimate.EstimateID.ToString(),
			estimate.Total!.Value.ToString("F2"),
			staticDataSettings.LoginUrl,
			staticDataSettings.LogoUrl,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl,
			currentYear);

		return await emailService.SendTemplateEmail(estimate.ProUser.Email, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailProOnboardingComplete(ProUserDetail pro, CancellationToken ct)
	{
		var templateId = templateIdsSettings.ProOnboardingComplete;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		var templateData = new ProOnboardingComplete(
			pro.FlowUser!.GetFullName(),
			staticDataSettings.LoginUrl,
			staticDataSettings.LogoUrl,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl,
			currentYear);

		return await emailService.SendTemplateEmail(pro.FlowUser.Email, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailCustomerResidentialEstimateScheduled(List<int> scheduledEstimatesIds, CancellationToken ct)
	{
		if (scheduledEstimatesIds.Count == 0) return Result.Success();

		var templateId = templateIdsSettings.CustomerResidentialEstimateScheduled;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		var scheduledEstimates = await repository.Query<ScheduledEstimate>()
			.Where(se => scheduledEstimatesIds.Contains(se.ScheduledEstimateID))
			.Include(se => se.ProUser)
				.ThenInclude(p => p.ProUser)
			.Include(se => se.Project)
				.ThenInclude(p => p.Lead)
					.ThenInclude(l => l.Customer)
			.ToListAsync(ct);

		Lead lead = scheduledEstimates.First().Project.Lead;
		string customerFullName = CustomerInfoHelper.GetCustomerFullName(lead);
		string customerEmail = CustomerInfoHelper.GetCustomerEmail(lead);

		var estimatesData = new List<EstimateTemplate>();

		foreach (var scheduledEstimate in scheduledEstimates)
		{
			var avatarUrl = await GetProAvatarUrl(scheduledEstimate.ProUserID, ct);
			var pro = scheduledEstimate.ProUser!;
			var scheduledDateTimeFormatted = await FormatDateTime(scheduledEstimate.ScheduledDateTime, pro.ProUser?.FMTimeZoneID, ct);

			estimatesData.Add(new EstimateTemplate(
				avatarUrl,
				pro.GetFullName() ?? string.Empty,
				pro.ProUser?.BusinessName ?? string.Empty,
				scheduledDateTimeFormatted,
				pro.PhoneNumber ?? string.Empty,
				pro.Email ?? string.Empty));
		}

		var templateData = new CustomerResidentialEstimateScheduled(
			customerFullName,
			staticDataSettings.LoginUrl,
			staticDataSettings.CreateAccountUrl,
			staticDataSettings.LogoUrl,
			staticDataSettings.ReferralLogoUrl,
			currentYear,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl,
			staticDataSettings.ReferralSource,
			staticDataSettings.ChipsImageUrl,
			staticDataSettings.CollectionsImageUrl,
			[.. estimatesData]);

		return await emailService.SendTemplateEmail(customerEmail, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailCustomerEstimatorArrived(Estimate estimate, CancellationToken ct)
	{
		var templateId = templateIdsSettings.CustomerEstimatorArrived;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		Lead lead = estimate.RequestedEstimate.Project.Lead;
		string customerFullName = CustomerInfoHelper.GetCustomerFullName(lead);
		string customerEmail = CustomerInfoHelper.GetCustomerEmail(lead);

		var templateData = new CustomerEstimatorArrived(
			customerFullName,
			estimate.RequestedEstimate.Name,
			estimate.ProUser.GetFullName(),
			staticDataSettings.HeroImageUrl,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.LogoUrl,
			currentYear,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl);

		return await emailService.SendTemplateEmail(customerEmail, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailCustomerJobScheduled(Job job, CancellationToken ct)
	{
		var templateId = templateIdsSettings.CustomerJobScheduled;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		Lead lead = job.Estimate.RequestedEstimate.Project.Lead;
		string customerFullName = CustomerInfoHelper.GetCustomerFullName(lead);
		string customerEmail = CustomerInfoHelper.GetCustomerEmail(lead);

		var pro = job.Estimate.ProUser;
		var avatarUrl = await GetProAvatarUrl(pro.UserID!.Value, ct);
		var prepBlocks = GetPrepBlocks("EmailStaticData:PrepBlockCommercial");
		var scheduledDateWorkStartedFormatted = await FormatDateTime(job.ScheduledDateWorkStarted, pro.ProUser.FMTimeZoneID, ct);

		var templateData = new CustomerJobScheduled(
			customerFullName,
			pro.ProUser!.BusinessName ?? string.Empty,
			job.Estimate.RequestedEstimate.Name,
			pro.ProUser!.BusinessName ?? string.Empty, // how's company_name different than pro_business_name?
			pro.GetFullName(),
			scheduledDateWorkStartedFormatted,
			pro.PhoneNumber,
			pro.Email,
			avatarUrl,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.FooterLogoUrl,
			currentYear,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl,
			staticDataSettings.CollectionsImageUrl,
			staticDataSettings.ReferralSource,
			prepBlocks);

		return await emailService.SendTemplateEmail(customerEmail, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailCustomerResidentialJobScheduled(Job job, CancellationToken ct)
	{
		var templateId = templateIdsSettings.CustomerResidentialJobScheduled;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		Lead lead = job.Estimate.RequestedEstimate.Project.Lead;
		string customerFullName = CustomerInfoHelper.GetCustomerFullName(lead);
		string customerEmail = CustomerInfoHelper.GetCustomerEmail(lead);

		var pro = job.Estimate.ProUser;
		var avatarUrl = await GetProAvatarUrl(pro.UserID!.Value, ct);
		var prepBlocks = GetPrepBlocks("EmailStaticData:PrepBlockResidential");
		var scheduledDateWorkStartedFormatted = await FormatDateTime(job.ScheduledDateWorkStarted, pro.ProUser?.FMTimeZoneID, ct);

		var templateData = new CustomerResidentialJobScheduled(
			customerFullName,
			pro.ProUser!.BusinessName ?? string.Empty,
			pro.GetFullName(),
			pro.PhoneNumber,
			pro.Email,
			scheduledDateWorkStartedFormatted,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.FooterLogoUrl,
			avatarUrl,
			staticDataSettings.CollectionsImageUrl,
			prepBlocks,
			staticDataSettings.ReferralSource,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl,
			currentYear,
			staticDataSettings.Unsubscribe);

		return await emailService.SendTemplateEmail(customerEmail, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailCustomerSignOffSuccessful(Estimate estimate, CancellationToken ct)
	{
		var templateId = templateIdsSettings.CustomerSignOffSuccessful;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		Lead lead = estimate.RequestedEstimate.Project.Lead;
		string customerFullName = CustomerInfoHelper.GetCustomerFullName(lead);
		string customerEmail = CustomerInfoHelper.GetCustomerEmail(lead);

		var amountDue = estimate.Total - estimate.PaidAmount;

		var templateData = new CustomerSignOffSuccessful(
			customerFullName,
			estimate.RequestedEstimate.Project.Title,
			customerFullName,
			estimate.ProUser.GetFullName(),
			estimate.Total!.Value.ToString("F2"),
			estimate.PaidAmount.ToString("F2"),
			amountDue!.Value.ToString("F2"),
			$"{webAppBaseUrl}/app/estimate/{estimate.EstimateID}/summary",
			staticDataSettings.LogoUrl,
			staticDataSettings.ReferralLogoUrl,
			currentYear,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl,
			staticDataSettings.ReferralSource,
			staticDataSettings.Unsubscribe);

		return await emailService.SendTemplateEmail(customerEmail, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailCustomerJobCancelled(Project project, CancellationToken ct)
	{
		var templateId = templateIdsSettings.CustomerJobCancelled;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		string customerFullName = CustomerInfoHelper.GetCustomerFullName(project.Lead);
		string customerEmail = CustomerInfoHelper.GetCustomerEmail(project.Lead);

		var pros = new List<CustomerJobCancelledPro>();

		foreach (var requestedEstimate in project.RequestedEstimates)
		{
			var pro = requestedEstimate.ProUser;

			if (pro == null || pros.Any(x => x.email == pro.Email)) continue;

			var avatarUrl = await GetProAvatarUrl(pro.UserID!.Value, ct);

			pros.Add(new CustomerJobCancelledPro(
				pro.ProUser?.BusinessName ?? string.Empty,
				pro.GetFullName(),
				pro.PhoneNumber ?? string.Empty,
				pro.Email,
				avatarUrl));
		}

		var templateData = new CustomerJobCancelled(
			customerFullName,
			project.Title,
			currentYear,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.FooterLogoUrl,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl,
			staticDataSettings.Unsubscribe, // example value set on config, what's the real value? this is used in other templates as well
			staticDataSettings.UnsubscribePreferences, // example value set on config, what's the real value? this is used in other templates as well
			[.. pros]);

		return await emailService.SendTemplateEmail(customerEmail, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailCustomerPaymentSuccessful(Estimate estimate, decimal paymentAmount, CancellationToken ct)
	{
		var templateId = templateIdsSettings.CustomerPaymentSuccessful;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		Lead lead = estimate.RequestedEstimate.Project.Lead;
		string customerFullName = CustomerInfoHelper.GetCustomerFullName(lead);
		string customerEmail = CustomerInfoHelper.GetCustomerEmail(lead);

		var templateData = new CustomerPaymentSuccessful(
			customerFullName,
			customerFullName, // how is recipient_name differant than customer_name?
			paymentAmount.ToString("F2"),
			estimate.RequestedEstimate.Project.Title,
			estimate.Total!.Value.ToString("F2"),
			estimate.Total!.Value.ToString("F2"),
			estimate.ProUser.ProUser?.BusinessName ?? string.Empty,
			staticDataSettings.HeroImageUrl,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.FooterLogoUrl,
			staticDataSettings.ReviewUrl, // example value set on config, where this should really come from?
			currentYear,
			staticDataSettings.Unsubscribe,
			staticDataSettings.UnsubscribePreferences,
			staticDataSettings.GoogleReviewUrl, // example value set on config, where this should really come from?
			staticDataSettings.YelpReviewUrl, // example value set on config, where this should really come from?
			staticDataSettings.CreateAccountUrl,
			staticDataSettings.LoginUrl);

		return await emailService.SendTemplateEmail(customerEmail, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailCustomerRequestedEstimate(Lead lead, CancellationToken ct)
	{
		var templateId = templateIdsSettings.CustomerRequestedEstimate;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		string customerFullName = CustomerInfoHelper.GetCustomerFullName(lead);
		string customerEmail = CustomerInfoHelper.GetCustomerEmail(lead);

		var templateData = new CustomerRequestedEstimate(
			customerFullName,
			staticDataSettings.SupportPhone,
			staticDataSettings.ReferralSource,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.FooterLogoUrl,
			staticDataSettings.HeroImageUrl,
			currentYear,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl,
			staticDataSettings.Unsubscribe,
			staticDataSettings.UnsubscribePreferences);

		Result sendTemplateEmailResult = await emailService.SendTemplateEmail(customerEmail, templateId, templateData, ct);

		return sendTemplateEmailResult;
	}

	public async Task<Result> SendEmailCustomerReviewEstimate(
		string customerFullName,
		string viewEstimateLink,
		string recipientEmail,
		string? businessName,
		CancellationToken ct
	)
	{
		string templateId = templateIdsSettings.CustomerReviewEstimate;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		var templateData = new CustomerReviewEstimate(
			customerFullName,
			businessName ?? string.Empty,
			viewEstimateLink,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.FooterLogoUrl,
			currentYear,
			staticDataSettings.FacebookUrl,
			staticDataSettings.InstagramUrl,
			staticDataSettings.LinkedinUrl,
			staticDataSettings.Unsubscribe,
			staticDataSettings.UnsubscribePreferences);

			return await emailService.SendTemplateEmail(recipientEmail, templateId, templateData, ct);
	}

	public async Task<Result> SendEmailPasswordResetLink(FlowUser user, string resetLink, CancellationToken ct)
	{
		var templateId = templateIdsSettings.ResetPasswordEmail;
		if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure(templateIdError);

		// Read the password reset expiration from configuration and convert to minutes
		var raw = configuration["NonceSettings:NonceConfigurations:PasswordReset:Expiration"];
		string expirationMinutes = "30";

		if (TimeSpan.TryParse(raw, out var ts))
		{
			expirationMinutes = ((int)ts.TotalMinutes).ToString();
		}

		var templateData = new PasswordResetEmail(
			user.GetFullName(),
			"FM Flow",
			"FM Flow",
			currentYear,
			staticDataSettings.ReferralLogoUrl,
			staticDataSettings.FooterLogoUrl,
			resetLink,
			expirationMinutes,
			staticDataSettings.SupportUrl,
			staticDataSettings.SupportEmail,
			staticDataSettings.SupportPhone);

		return await emailService.SendTemplateEmail(user.Email, templateId, templateData, ct);
	}

	private Prep_Blocks[] GetPrepBlocks(string sectionName)
	{
		var section = configuration.GetSection(sectionName);
		var list = new List<Prep_Blocks>();

		foreach (var child in section.GetChildren())
		{
			var text = child["Text"] ?? string.Empty;
			var imageUrl = child["ImageUrl"] ?? string.Empty;
			list.Add(new Prep_Blocks(text, imageUrl));
		}

		return [.. list];
	}

	private async Task<string> GetProAvatarUrl(int proId, CancellationToken ct)
	{
		var avatarFileId = await repository.Query<ProUserFile>()
				.ByUserID(proId)
				.Where(f => f.ProFileType == ProUserFileType.ProfilePicture)
				.Select(x => x.ProUserFileID)
				.FirstOrDefaultAsync(ct);

		return avatarFileId != 0
			? $"{apiBaseUrl}/api/ProUsers/{proId}/Files/{avatarFileId}"
			: string.Empty;
	}

	private async Task<string> FormatDateTime(DateTimeOffset dateTimeOffset, int? timeZoneId, CancellationToken ct)
	{
		if (!timeZoneId.HasValue)
		{
			return dateTimeOffset.ToString("ddd, MM/dd/yyyy h:mm tt") + " - UTC";
		}

		var timeZone = await repository
			.Query<FMTimeZone>()
			.ByTimeZoneId(timeZoneId)
			.FirstAsync(ct);

		TimeZoneInfo timeZoneInfo;

		try
		{
			timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone.SystemTimeZoneId);
		}
		catch (TimeZoneNotFoundException)
		{
			timeZoneInfo = TimeZoneInfo.Utc;
		}
		catch (InvalidTimeZoneException)
		{
			timeZoneInfo = TimeZoneInfo.Utc;
		}

		var converted = TimeZoneInfo.ConvertTime(dateTimeOffset, timeZoneInfo);
		var formatted = converted.ToString("ddd, MM/dd/yyyy h:mm tt");
		return $"{formatted} - {timeZone.Name}";
	}
}
