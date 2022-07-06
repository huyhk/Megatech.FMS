namespace FMS.Data
{
    public class Tablet : BaseEntity
    {
        public string SerialNumber { get; set; }

        public int CurrentUserId { get; set; }

        public int TruckId { get; set; }

        public TabletStatus Status { get; set; }
    }

    public enum TabletStatus
    {
        ACTIVE,
        INACTIVE,
        ERROR
    }
}
