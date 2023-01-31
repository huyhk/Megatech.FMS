using EntityFramework.DynamicFilters;
using Megatech.FMS.Data.Permissions;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace FMS.Data
{
    public class DataContext : DbContext
    {
        private static DataContext _db;
        public DataContext() : this("FMSConnection")
        { }
        public DataContext(string cnName) : base(cnName)
        {
            //Database.SetInitializer(new CreateDatabaseIfNotExists<DataContext>());
            Database.SetInitializer<DataContext>(null);
        }
        public static DataContext GetInstance()
        {
            if (_db == null)
                _db = new DataContext();
            return _db;
        }
        public DbSet<ChangeLog> ChangeLogs { get; set; }
        //public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<Airport> Airports { get; set; }
        public DbSet<Aircraft> Aircrafts { get; set; }

        public DbSet<Airline> Airlines { get; set; }
        public DbSet<InvoiceForm> InvoiceForms { get; set; }
        // public DbSet<Agency> Agencies { get; set; }
        public DbSet<Refuel> Refuels { get; set; }
        //public DbSet<AirportType> AirportTypes { get; set; }
        public DbSet<RefuelItem> RefuelItems { get; set; }

        public DbSet<Truck> Trucks { get; set; }


        //public DbSet<TruckAssign> TruckAssigns { get; set; }

        public DbSet<Device> Devices { get; set; }

        public DbSet<Tablet> Tablets { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        public DbSet<ControllerInfo> Controllers { get; set; }

        public DbSet<ActionInfo> Actions { get; set; }

        public DbSet<Invoice> Invoices { get; set; }

        public DbSet<TruckFuel> TruckFuels { get; set; }

        public DbSet<BM2505> BM2505s { get; set; }

        public DbSet<GreenTax> GreenTaxes { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

            //modelBuilder.Entity<BM2505>().Property(o => o.Density).HasPrecision(18, 4);

            //modelBuilder.Entity<BM2505>().Property(o => o.Density15).HasPrecision(18, 4);


            modelBuilder.Filter("IsNotDeleted", (BaseEntity entity) => entity.IsDeleted, false);
            modelBuilder.Filter("UserAirport", (IAirportBase entity, ICollection<int> airportList) => airportList.Contains(entity.Airport.Id), new List<int>());
            modelBuilder.DisableFilterGlobally("UserAirport");

            modelBuilder.Entity<Route>()
          .HasRequired<Airport>(s => s.Departure)
          .WithMany()
          .WillCascadeOnDelete(false);

            modelBuilder.Entity<Route>()
         .HasRequired<Airport>(s => s.Arrival)
         .WithMany()
         .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<Shift>()
            //    .HasRequired<Airport>(s => s.Airport)
            //    .WithMany()
            //    .WillCascadeOnDelete(false);

            //modelBuilder.Entity<Truck>()
            //   .HasRequired<Airport>(s => s.AirportObject)
            //   .WithMany()
            //   .WillCascadeOnDelete(false);

            //modelBuilder.Entity<TruckAssign>()
            //    .HasRequired<Shift>(s => s.Shift)
            //    .WithOptional()
            //    .WillCascadeOnDelete(false);

            modelBuilder.Entity<Truck>()
                 .HasOptional<Airport>(s => s.CurrentAirport)
                 .WithMany()
                 .WillCascadeOnDelete(false);

            modelBuilder.Entity<RefuelItem>().Property(o => o.Density).HasPrecision(18, 4);
            modelBuilder.Entity<FHSImportItem>().Property(o => o.Density).HasPrecision(18, 4);

            modelBuilder.Entity<InvoiceItem>().Property(o => o.Density).HasPrecision(18, 4);

            modelBuilder.Entity<ReceiptItem>().Property(o => o.Density).HasPrecision(18, 4);

            modelBuilder.Entity<RefuelItem>().Property(o => o.OriginalDensity).HasPrecision(18, 4);

            modelBuilder.Entity<BM2505>().Property(o => o.Density).HasPrecision(18, 4);
            modelBuilder.Entity<BM2505>().Property(o => o.Density15).HasPrecision(18, 4);
            modelBuilder.Entity<BM2505>().Property(o => o.DensityDiff).HasPrecision(18, 4);

            modelBuilder.Entity<User>()
               .HasMany<Airport>(s => s.Airports)
               .WithMany(c => c.Users)
               .Map(cs =>
               {
                   cs.MapLeftKey("UserId");
                   cs.MapRightKey("AirportId");
                   cs.ToTable("UserAirport");
               });

        }

        public System.Data.Entity.DbSet<FMS.Data.Flight> Flights { get; set; }

        public System.Data.Entity.DbSet<FMS.Data.ParkingLot> ParkingLots { get; set; }

        public System.Data.Entity.DbSet<FMS.Data.Product> Products { get; set; }

        public System.Data.Entity.DbSet<FMS.Data.Shift> Shifts { get; set; }
        public System.Data.Entity.DbSet<FMS.Data.ProductPrice> ProductPrices { get; set; }
        public System.Data.Entity.DbSet<FMS.Data.TruckAssign> TruckAssigns { get; set; }
        public System.Data.Entity.DbSet<FMS.Data.CheckTruck> CheckTrucks { get; set; }

        public System.Data.Entity.DbSet<FMS.Data.Receipt> Receipts { get; set; }

        public DbSet<FHSImport> FHSImports { get; set; }

        public DbSet<TruckLog> TruckLog { get; set; }


        public DbSet<Review> Reviews { get; set; }

        public void ClearChanges()
        {
            var changedEntriesCopy = this.ChangeTracker.Entries()
                                .Where(e => e.State == EntityState.Added ||
                                            e.State == EntityState.Modified ||
                                            e.State == EntityState.Deleted)
                                .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;

        }
        //public System.Data.Entity.DbSet<FMS.Data.ParkingReport> ParkingReports { get; set; }
    }
}
