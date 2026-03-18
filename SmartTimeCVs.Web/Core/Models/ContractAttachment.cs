using System.ComponentModel.DataAnnotations;
using SmartTimeCVs.Web.Core.Models.Base;

namespace SmartTimeCVs.Web.Core.Models
{
    public class ContractAttachment : BaseModel
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }
        public Contract? Contract { get; set; }

        public int DocumentRequirementLookupId { get; set; }
        public DocumentRequirementLookup? DocumentRequirementLookup { get; set; }

        public string? FileUrl { get; set; }
    }
}
