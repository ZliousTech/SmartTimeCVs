using System.ComponentModel.DataAnnotations;
using SmartTimeCVs.Web.Core.Models.Base;

namespace SmartTimeCVs.Web.Core.Models
{
    public class ContractCategory : BaseModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string NameEn { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string NameNative { get; set; } = null!;
    }
}
