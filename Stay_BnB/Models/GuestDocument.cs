using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Stay_BnB.Models.Enums;

namespace Stay_BnB.Models;

public class GuestDocument
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GuestCheckInId { get; set; }

    [Required]
    [StringLength(100)]
    public string DocumentType { get; set; } = "ID or Passport";

    [Required]
    [StringLength(200)]
    public string FileName { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(GuestCheckInId))]
    public virtual GuestCheckIn? GuestCheckIn { get; set; }
}
