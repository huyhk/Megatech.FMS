using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace FMS.Data
{
    public partial class PropertyFlightValue
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Flight_Id { get; set; }
        public int Property_Id { get; set; }
        public string Note { get; set; }
        [ForeignKey("Flight_Id")]
        public Flight Flight { get; set; }
    }
}