namespace FMFlow.Entities.ModelInterfaces;

public interface IHasDateChangeTracking
{
	DateTimeOffset DateCreated { get; set; }
	DateTimeOffset? DateUpdated { get; set; }
}
