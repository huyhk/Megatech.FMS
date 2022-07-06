namespace FMS.Data
{
    public class Route : BaseEntity
    {
        public int DepartureId { get; set; }
        public Airport Departure { get; set; }
        public int ArrivalId { get; set; }
        public Airport Arrival { get; set; }

    }
}
