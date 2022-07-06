using System;

namespace FMS.Data
{
    public class ChangeLog
    {
        public int Id { get; set; }
        public string EntityName { get; set; }
        public string EntityDisplay { get; set; }
        public string PropertyName { get; set; }
        public string KeyValues { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public DateTime DateChanged { get; set; }
        public int? UserUpdatedId { get; set; }
        public string UserUpdatedName { get; set; }
    }
}
