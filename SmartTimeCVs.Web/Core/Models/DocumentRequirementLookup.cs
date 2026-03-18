using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartTimeCVs.Web.Core.Models.Base;

namespace SmartTimeCVs.Web.Core.Models
{
    public class DocumentRequirementLookup : BaseModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string NameEn { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string NameNative { get; set; } = null!;

        public bool IsRequired { get; set; } = true;

        public ICollection<ContractType> ContractTypes { get; set; } = new List<ContractType>();
    }
}
