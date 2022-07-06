using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMS.Data
{
    public class BaseEntity
    {
        protected DataContext _dbContext = null;
        public BaseEntity()
        {
            this.DateCreated = this.DateUpdated = DateTime.Now;
        }

        public BaseEntity(DataContext context) : this()
        {
            _dbContext = context;
        }
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public DateTime DateUpdated { get; set; } = DateTime.Now;

        public int? UserCreatedId { get; set; }

        public int? UserUpdatedId { get; set; }


        public bool IsDeleted { get; set; }

        public DateTime? DateDeleted { get; set; }

        public int? UserDeletedId { get; set; }


    }
}
