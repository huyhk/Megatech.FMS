using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMS.Data
{
    public class Review : BaseEntity
    {
        public Review()
        {
            
        }
        public int FlightId { get; set; }

        public Flight Flight { get; set; }

        public REVIEW_RATE Rate { get; set; }

        public BAD_REVIEW_REASON BadReason { get; set; }

        public string OtherReason { get; set; }
        public DateTime ReviewDate { get; set; }
        public Guid UniqueId { get; set; } = Guid.NewGuid();

        public string ImagePath { get; set; }
    }

    public enum REVIEW_RATE
    {
        WORST = 1,
        BAD = 2,
        NEUTRAL = 3,
        GOOD = 4, 
        BEST = 5
    }
    [Flags]
    public enum BAD_REVIEW_REASON
    {
        UNSAFE_REFUELING = 1,
        INACCURATELY_PERFORM = 2,
        NOT_ON_TIME = 4, 
        INAPPROPRIATE_ATTITUDE = 8,
        INACCURATE_AMOUNT = 16,
        OTHER = 32,
    }
    public struct BAD_REVIEW_REASON_TEXT
    {
        public const string UNSAFE_REFUELING = "Tra nạp thiếu an toàn";

        public const string INACCURATELY_PERFORM = "Nhân viên thao tác sai";
        public const string NOT_ON_TIME = "TRa nạp chậm muộn";
        public const string INAPPROPRIATE_ATTITUDE = "Thái độ nhân viên không tốt";
        public const string INACCURATE_AMOUNT = "Khối lượng nhiên liệu không chuẩn xác";
        public const string OTHER = "Lý do khác";
    }
}
