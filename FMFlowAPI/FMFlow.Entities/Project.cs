using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;

namespace FMFlow.Entities;

public class Project : IHasDateChangeTracking, IHasDeleted
{
	[Key]
	public int ProjectID { get; set; }

	[Required]
	[ForeignKey(nameof(Lead))]
	public int LeadID { get; set; }

	public virtual Lead Lead { get; set; } = null!;

	[ForeignKey(nameof(FlowUser))]
	public int? ProId { get; set; } // For projects, this means the pro user CREATED the project

	public virtual FlowUser? Pro { get; set; }

	public List<RequestedEstimate> RequestedEstimates { get; set; } = [];

	public List<ScheduledEstimate> ScheduledEstimates { get; set; } = [];

	[Required]
	[ForeignKey(nameof(Address))]
	public int AddressID { get; set; }

	public virtual Address Address { get; set; } = null!;

	[Required]
	public string Title { get; set; } = null!;

	public bool IsOpen { get; set; } = true;

	public string? Summary { get; set; }

	[Required]
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public bool IsDeleted { get; set; }

	public bool IsDeleteable() => 
		RequestedEstimates.All(re => re.IsDeleted) && ScheduledEstimates.All(se => se.IsDeleted);

	public DateTimeOffset? DateDeleted { get; set; }

	public string? SelectedPaintColors { get; set; }

	public int? ApproxSquareFootage { get; set; }

	public string GetShortName() // Used for project tab on lead screen
	{
		return Title;
	}

	public string GetLongName() // Used for projects search or project details
	{
		return $"{Lead?.GetFullName()} - {Title}";
	}

	/// <summary>
	/// Generate names for the project
	/// </summary>
	/// <param name="project">The supplied project should have Lead included in order to properly use this function.</param>
	/// <returns></returns>
	public ProjectNames GetProjectNames()
	{
		string leadFullName = Lead != null ? Lead.GetFullName() : string.Empty;

		return new ProjectNames(
			leadFullName,
			GetShortName(),
			GetLongName());
	}
}

public record ProjectNames(
	string? LeadFullName,
	string ShortName,
	string LongName);
