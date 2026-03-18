using System.ComponentModel.DataAnnotations;
using SmartTimeCVs.Web.Core.Models.Base;

namespace SmartTimeCVs.Web.Core.Models
{
    public class ContractType : BaseModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string NameEn { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string NameNative { get; set; } = null!;

        public string? DescriptionEn { get; set; }

        public string? DescriptionNative { get; set; }

        [MaxLength(200)]
        public string? ContractFor { get; set; }

        public string? ClausesEn { get; set; }

        public string? ClausesNative { get; set; }

        [MaxLength(200)]
        public string? FirstPartyName { get; set; }

        [MaxLength(500)]
        public string? FirstPartyAddress { get; set; }

        [MaxLength(200)]
        public string? AuthorizedSignatory { get; set; }

        [MaxLength(100)]
        public string? CommercialNumber { get; set; }

        public ICollection<DocumentRequirementLookup> DocumentRequirements { get; set; } = new List<DocumentRequirementLookup>();
    }
}
