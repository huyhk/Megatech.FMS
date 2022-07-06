namespace FMS.Data
{
    /// <summary>
    /// LCIQ device
    /// </summary>
    public class Device : BaseEntity
    {
        public string SerialNumber { get; set; }

        public int Status { get; set; }
    }

    public enum DeviceStatus
    {
        ACTIVE,
        INACTIVE,
        ERROR
    }
}
