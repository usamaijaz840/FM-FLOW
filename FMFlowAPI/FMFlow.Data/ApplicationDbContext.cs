using System.Reflection;
using System.Text.Json;
using FMFlow.Entities;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
	public DbSet<BillingPlan> BillingPlans { get; set; }
	public DbSet<EmployeeUser> EmployeeUserDetail { get; set; }
	public DbSet<FileItem> FileItems { get; set; }
	public DbSet<FileItemToEstimate> FileToEstimates { get; set; }
	public DbSet<FlowUser> FMFlowUsers { get; set; }
	public DbSet<LeadSource> LeadSources { get; set; }
	public DbSet<Lead> Leads { get; set; }
	public DbSet<LeadNote> LeadNotes { get; set; }
	public DbSet<LeadTimeline> LeadTimelines { get; set; }
	public DbSet<Paint> Paints { get; set; }
	public DbSet<ProUserDetail> ProUserDetails { get; set; }
	public DbSet<ProUserToProZipcode> ProUserToProZipcodes { get; set; }
	public DbSet<ZipCode> ZipCodes { get; set; }
	public DbSet<State> States { get; set; }
	public DbSet<Sheen> Sheens { get; set; }
	public DbSet<FMTimeZone> FMTimeZones { get; set; }
	public DbSet<Project> Projects { get; set; }
	public DbSet<EstimateType> EstimateTypes { get; set; }
	public DbSet<ProjectNote> ProjectNotes { get; set; }
	public DbSet<RequestedEstimate> RequestedEstimates { get; set; }
	public DbSet<Estimate> Estimates { get; set; }
	public DbSet<ScheduledEstimate> ScheduledEstimates { get; set; }
	public DbSet<Address> Addresses { get; set; }
	public DbSet<Integration> Integrations { get; set; }
	public DbSet<ProWeekDayAvailability> ProWeekDayAvailabilities { get; set; }
	public DbSet<PaintSheen> PaintSheens { get; set; }
	public DbSet<PaintSheenPrice> PaintSheenPrices { get; set; }
	public DbSet<ProUserFile> ProUserFiles { get; set; }
	public DbSet<EstimateNote> EstimateNotes { get; set; }
	public DbSet<Transaction> Transactions { get; set; }
	public DbSet<Job> Jobs { get; set; }
	public DbSet<JobNote> JobNotes { get; set; }
	public DbSet<Color> Colors { get; set; }
	public DbSet<CustomerTempPro> CustomerTempPros { get; set; }
	public DbSet<Nonce> Nonces { get; set; }
	public DbSet<MxVaultedAccount> MxVaultedAccounts { get; set; }
	public DbSet<FCMRegistration> FCMRegistrations { get; set; }
	public DbSet<ChatBotSessionPro> ChatBotSessionPros { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.HasAnnotation("Relational:AutoMigrationsEnabled", false);
		modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

		modelBuilder.Entity<Estimate>()
			.Property(d => d.Attributes)
			.HasColumnType("jsonb");

		modelBuilder.Entity<FileItemToEstimate>()
			.HasKey(f => new { f.FileID, f.EstimateID });

		modelBuilder.Entity<LeadTimeline>()
			.Property(e => e.EventParameters)
			.HasConversion(
				v => v.RootElement.GetRawText(),
				v => JsonDocument.Parse(v, default));

		// Set bidirectional relationship:
		modelBuilder.Entity<Estimate>()
			.HasOne(e => e.Job)
			.WithOne(j => j.Estimate)
			.HasForeignKey<Job>(j => j.EstimateId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<Job>()
			.HasOne(j => j.Estimate)
			.WithOne(e => e.Job)
			.HasForeignKey<Estimate>(e => e.JobId)
			.IsRequired(false)
			.OnDelete(DeleteBehavior.Restrict);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder
			.Properties<Enum>()
			.HaveConversion<string>();
	}
}
