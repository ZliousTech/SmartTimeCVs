using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace SmartTimeCVs.Web.Core.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string CompanyId { get; set; } = null!;
        
        [Required]
        [Remote(action: "VerifyUserName", controller: "NewCompanySetup")]
        [Display(Name = "User Name")]
        public string UserName { get; set; } = null!;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
        
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
        
        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
