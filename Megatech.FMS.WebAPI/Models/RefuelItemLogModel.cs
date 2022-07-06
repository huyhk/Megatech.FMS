using System;

namespace Megatech.FMS.WebAPI.Models
{
    public class RefuelItemLogModel
    {
        public int Id { get; set; }

        public DateTime DateUpdated { get; set; }

        public string Name { get; set; }

        public string OldValue { get; set; }

        public string NewValue { get; set; }

        public int UserId { get; set; }
    }
}