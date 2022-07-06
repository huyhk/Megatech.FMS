using System;

namespace Megatech.FMS.WebAPI.Models
{

    public class BaseViewModel
    {
        public int Id { get; set; }
        public bool IsDeleted { get; set; }

        public DateTime? DateUpdated { get; set; }

        public string UniqueId { get; set; }
    }
}