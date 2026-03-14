using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartTimeCVs.Web.Core.ViewModels
{
    public class UploadContractRequirementsViewModel
    {
        [Required]
        public int ContractId { get; set; }

        [Required(ErrorMessage = "Please upload the signed contract.")]
        [Display(Name = "Signed Contract")]
        public IFormFile SignedContract { get; set; } = null!;

        [Required(ErrorMessage = "Please upload your National ID.")]
        [Display(Name = "National ID")]
        public IFormFile NationalIdFile { get; set; } = null!;
    }
}
