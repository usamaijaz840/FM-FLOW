using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;

namespace FMFlow.Entities;

public class LeadNote : IHasDateChangeTracking, IHasDeleted
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LeadNoteId { get; set; }

    [ForeignKey(nameof(Lead))]
    public int LeadId { get; set; }
    
    public virtual Lead Lead { get; set; } = null!;

    [ForeignKey(nameof(Pro))]
    public int? ProId { get; set; }
    
    public virtual ProUserDetail? Pro { get; set; }

    [Required]
    public string Note { get; set; } = null!;

    [Required]
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DateUpdated { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DateDeleted { get; set; }
} 