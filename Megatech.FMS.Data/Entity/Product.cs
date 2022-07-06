using System.ComponentModel.DataAnnotations;

namespace FMS.Data
{
    public class Product : BaseEntity
    {
        [Required]
        public string Code { get; set; }
        [Required]
        public string Name { get; set; }
    }
}
