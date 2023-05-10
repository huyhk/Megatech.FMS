using FMS.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Megatech.FMS.WebAPI.Models
{
    public class ReviewModel: BaseViewModel
    {
        public Guid FlightUniqueId { get; set; }

        public int FlightId { get; set; }

        public Flight Flight { get; set; }

        public REVIEW_RATE Rate { get; set; }

        public BAD_REVIEW_REASON BadReviewReason { get; set; }

        public string OtherReason { get; set; }

        public DateTime ReviewDate { get; set; }


        public string ImageString { get; set; }

        public string ImagePath { get; set; }

    }
}