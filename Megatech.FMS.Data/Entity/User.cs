using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMS.Data
{
    public class User : BaseEntity
    {
        public User()
        {
            Permission = USER_PERMISSION.NONE;
        }

        public string UserName { get; set; }
        [Required]
        //[StringLength(20, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 5)]
        public string FullName { get; set; }

        //[Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        public bool IsEnabled { get; set; }

        public DateTime? LastLogin { get; set; }

        public int? AirportId { get; set; }
        public Branch Branch { get; set; }
        public Airport Airport { get; set; }
        public string DisplayName { get; set; }
        public string Group { get; set; }
        public string ImportName { get; set; }


        public ICollection<Airport> Airports { get; set; }


        public ICollection<Role> Roles { get; set; }

        public USER_PERMISSION Permission { get; set; }
        [NotMapped]
        public string RoleName { get; set; }
        [NotMapped]
        public bool IsCreateRefuel
        {
            get { return Permission.HasFlag(USER_PERMISSION.CREATE_REFUEL); }
            set { Permission = value ? Permission | USER_PERMISSION.CREATE_REFUEL : Permission & ~USER_PERMISSION.CREATE_REFUEL; }
        }

        [NotMapped]
        public bool IsCreateExtract
        {
            get { return Permission.HasFlag(USER_PERMISSION.CREATE_EXTRACT); }
            set { Permission = value ? Permission | USER_PERMISSION.CREATE_EXTRACT : Permission & ~USER_PERMISSION.CREATE_EXTRACT; }
        }

        [NotMapped]
        public bool IsCreateCustomer
        {
            get { return Permission.HasFlag(USER_PERMISSION.CREATE_CUSTOMER); }
            set { Permission = value ? Permission | USER_PERMISSION.CREATE_CUSTOMER : Permission & ~USER_PERMISSION.CREATE_CUSTOMER; }
        }
    }

    [Flags]
    public enum USER_PERMISSION
    {
        NONE,
        CREATE_REFUEL = 1,
        CREATE_EXTRACT = 2,
        CREATE_CUSTOMER = 4
    }
}
