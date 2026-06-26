namespace FMFlow.Entities.ModelInterfaces;

public interface IHasDeleted
{
	DateTimeOffset? DateDeleted { get; set; }
	bool IsDeleted { get; set; }
}
