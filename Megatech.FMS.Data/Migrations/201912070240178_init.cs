namespace FMS.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Aircraft",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Code = c.String(),
                        AircraftType = c.String(),
                        CustomerId = c.Int(),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airlines", t => t.CustomerId)
                .Index(t => t.CustomerId);
            
            CreateTable(
                "dbo.Airlines",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(),
                        Name = c.String(),
                        TaxCode = c.String(),
                        Address = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Flights",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(),
                        DepartureScheduledTime = c.DateTime(),
                        DepartuteTime = c.DateTime(),
                        ArrivalScheduledTime = c.DateTime(),
                        ArrivalTime = c.DateTime(),
                        FlightTime = c.DateTime(),
                        RouteName = c.String(),
                        AircraftCode = c.String(),
                        AircraftType = c.String(),
                        EstimateAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        RefuelTime = c.DateTime(),
                        Parking = c.String(),
                        RefuelId = c.Int(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                        Aircraft_Id = c.Int(),
                        Airline_Id = c.Int(),
                        Arrival_Id = c.Int(nullable: false),
                        Departure_Id = c.Int(nullable: false),
                        ParkingLot_Id = c.Int(),
                        Route_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Aircraft", t => t.Aircraft_Id)
                .ForeignKey("dbo.Airlines", t => t.Airline_Id)
                .ForeignKey("dbo.Airports", t => t.Arrival_Id)
                .ForeignKey("dbo.Airports", t => t.Departure_Id)
                .ForeignKey("dbo.ParkingLots", t => t.ParkingLot_Id)
                .ForeignKey("dbo.Routes", t => t.Route_Id)
                .Index(t => t.Aircraft_Id)
                .Index(t => t.Airline_Id)
                .Index(t => t.Arrival_Id)
                .Index(t => t.Departure_Id)
                .Index(t => t.ParkingLot_Id)
                .Index(t => t.Route_Id);
            
            CreateTable(
                "dbo.Airports",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(),
                        Name = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ParkingLots",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AirportId = c.Int(nullable: false),
                        Code = c.String(maxLength: 20),
                        Name = c.String(maxLength: 100),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                        Location_Id = c.Int(),
                        Flight_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airports", t => t.AirportId, cascadeDelete: true)
                .ForeignKey("dbo.GeoLocations", t => t.Location_Id)
                .ForeignKey("dbo.Flights", t => t.Flight_Id)
                .Index(t => t.AirportId)
                .Index(t => t.Location_Id)
                .Index(t => t.Flight_Id);
            
            CreateTable(
                "dbo.GeoLocations",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Longitude = c.Single(nullable: false),
                        Latitude = c.Single(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Refuels",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FlightId = c.Int(nullable: false),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(nullable: false),
                        Status = c.Int(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                        Flight_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Flights", t => t.FlightId, cascadeDelete: true)
                .ForeignKey("dbo.Flights", t => t.Flight_Id)
                .Index(t => t.FlightId)
                .Index(t => t.Flight_Id);
            
            CreateTable(
                "dbo.RefuelItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TruckId = c.Int(nullable: false),
                        StartTime = c.DateTime(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Completed = c.Boolean(nullable: false),
                        Printed = c.Boolean(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                        Refuel_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Trucks", t => t.TruckId, cascadeDelete: true)
                .ForeignKey("dbo.Refuels", t => t.Refuel_Id)
                .Index(t => t.TruckId)
                .Index(t => t.Refuel_Id);
            
            CreateTable(
                "dbo.Trucks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DeviceId = c.Int(nullable: false),
                        TabletId = c.String(),
                        Code = c.String(),
                        MaxAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CurrentAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Devices", t => t.DeviceId, cascadeDelete: true)
                .Index(t => t.DeviceId);
            
            CreateTable(
                "dbo.Devices",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SerialNumber = c.String(),
                        Status = c.Int(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Routes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DepartureId = c.Int(nullable: false),
                        ArrivalId = c.Int(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airports", t => t.ArrivalId)
                .ForeignKey("dbo.Airports", t => t.DepartureId)
                .Index(t => t.DepartureId)
                .Index(t => t.ArrivalId);
            
            CreateTable(
                "dbo.ChangeLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EntityName = c.String(),
                        PropertyName = c.String(),
                        KeyValues = c.String(),
                        OldValues = c.String(),
                        NewValues = c.String(),
                        DateChanged = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Tablets",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SerialNumber = c.String(),
                        CurrentUserId = c.Int(nullable: false),
                        TruckId = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserName = c.String(),
                        FullName = c.String(),
                        Email = c.String(),
                        IsEnabled = c.Boolean(nullable: false),
                        LastLogin = c.DateTime(),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Flights", "Route_Id", "dbo.Routes");
            DropForeignKey("dbo.Routes", "DepartureId", "dbo.Airports");
            DropForeignKey("dbo.Routes", "ArrivalId", "dbo.Airports");
            DropForeignKey("dbo.Refuels", "Flight_Id", "dbo.Flights");
            DropForeignKey("dbo.RefuelItems", "Refuel_Id", "dbo.Refuels");
            DropForeignKey("dbo.RefuelItems", "TruckId", "dbo.Trucks");
            DropForeignKey("dbo.Trucks", "DeviceId", "dbo.Devices");
            DropForeignKey("dbo.Refuels", "FlightId", "dbo.Flights");
            DropForeignKey("dbo.ParkingLots", "Flight_Id", "dbo.Flights");
            DropForeignKey("dbo.ParkingLots", "Location_Id", "dbo.GeoLocations");
            DropForeignKey("dbo.Flights", "ParkingLot_Id", "dbo.ParkingLots");
            DropForeignKey("dbo.ParkingLots", "AirportId", "dbo.Airports");
            DropForeignKey("dbo.Flights", "Departure_Id", "dbo.Airports");
            DropForeignKey("dbo.Flights", "Arrival_Id", "dbo.Airports");
            DropForeignKey("dbo.Flights", "Airline_Id", "dbo.Airlines");
            DropForeignKey("dbo.Flights", "Aircraft_Id", "dbo.Aircraft");
            DropForeignKey("dbo.Aircraft", "CustomerId", "dbo.Airlines");
            DropIndex("dbo.Routes", new[] { "ArrivalId" });
            DropIndex("dbo.Routes", new[] { "DepartureId" });
            DropIndex("dbo.Trucks", new[] { "DeviceId" });
            DropIndex("dbo.RefuelItems", new[] { "Refuel_Id" });
            DropIndex("dbo.RefuelItems", new[] { "TruckId" });
            DropIndex("dbo.Refuels", new[] { "Flight_Id" });
            DropIndex("dbo.Refuels", new[] { "FlightId" });
            DropIndex("dbo.ParkingLots", new[] { "Flight_Id" });
            DropIndex("dbo.ParkingLots", new[] { "Location_Id" });
            DropIndex("dbo.ParkingLots", new[] { "AirportId" });
            DropIndex("dbo.Flights", new[] { "Route_Id" });
            DropIndex("dbo.Flights", new[] { "ParkingLot_Id" });
            DropIndex("dbo.Flights", new[] { "Departure_Id" });
            DropIndex("dbo.Flights", new[] { "Arrival_Id" });
            DropIndex("dbo.Flights", new[] { "Airline_Id" });
            DropIndex("dbo.Flights", new[] { "Aircraft_Id" });
            DropIndex("dbo.Aircraft", new[] { "CustomerId" });
            DropTable("dbo.Users");
            DropTable("dbo.Tablets");
            DropTable("dbo.ChangeLogs");
            DropTable("dbo.Routes");
            DropTable("dbo.Devices");
            DropTable("dbo.Trucks");
            DropTable("dbo.RefuelItems");
            DropTable("dbo.Refuels");
            DropTable("dbo.GeoLocations");
            DropTable("dbo.ParkingLots");
            DropTable("dbo.Airports");
            DropTable("dbo.Flights");
            DropTable("dbo.Airlines");
            DropTable("dbo.Aircraft");
        }
    }
}
