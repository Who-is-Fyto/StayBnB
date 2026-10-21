using System.ComponentModel.DataAnnotations;
using Stay_BnB.Models;

namespace Stay_BnB.ViewModels.Admin;

public class ApplicationReviewViewModel
{
    public HostApplication Application { get; set; } = null!;
    public HostProperty Property { get; set; } = null!;
    public ApplicationUser Applicant { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Rejection feedback cannot exceed 500 characters")]
    [Display(Name = "Reason for Rejection")]
    public string? RejectionReason { get; set; }
}
