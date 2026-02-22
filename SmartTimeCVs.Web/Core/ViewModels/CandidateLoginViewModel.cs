using System.ComponentModel.DataAnnotations;

namespace SmartTimeCVs.Web.Core.ViewModels
{
    public class CandidateLoginViewModel
    {
        [Required]
        public int AppId { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [Display(Name = "Mobile Number")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string MobileNumber { get; set; } = string.Empty;
    }
}
