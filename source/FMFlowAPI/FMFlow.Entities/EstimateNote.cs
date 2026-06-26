using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;

namespace FMFlow.Entities;

public class EstimateNote : IHasDateChangeTracking, IHasDeleted
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EstimateNoteId { get; set; }

    [ForeignKey(nameof(Estimate))]
    public int EstimateId { get; set; }
    
    public virtual Estimate Estimate { get; set; } = null!;

    [Required]
    public string Note { get; set; } = null!;

    [Required]
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DateUpdated { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DateDeleted { get; set; }
} 