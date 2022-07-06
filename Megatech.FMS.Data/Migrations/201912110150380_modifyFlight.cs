namespace FMS.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class modifyFlight : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.Flights", name: "Aircraft_Id", newName: "AircraftId");
            RenameColumn(table: "dbo.Flights", name: "Airline_Id", newName: "AirlineId");
            RenameColumn(table: "dbo.Flights", name: "Arrival_Id", newName: "ArrivalId");
            RenameColumn(table: "dbo.Flights", name: "Departure_Id", newName: "DepartureId");
            RenameIndex(table: "dbo.Flights", name: "IX_Departure_Id", newName: "IX_DepartureId");
            RenameIndex(table: "dbo.Flights", name: "IX_Arrival_Id", newName: "IX_ArrivalId");
            RenameIndex(table: "dbo.Flights", name: "IX_Airline_Id", newName: "IX_AirlineId");
            RenameIndex(table: "dbo.Flights", name: "IX_Aircraft_Id", newName: "IX_AircraftId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.Flights", name: "IX_AircraftId", newName: "IX_Aircraft_Id");
            RenameIndex(table: "dbo.Flights", name: "IX_AirlineId", newName: "IX_Airline_Id");
            RenameIndex(table: "dbo.Flights", name: "IX_ArrivalId", newName: "IX_Arrival_Id");
            RenameIndex(table: "dbo.Flights", name: "IX_DepartureId", newName: "IX_Departure_Id");
            RenameColumn(table: "dbo.Flights", name: "DepartureId", newName: "Departure_Id");
            RenameColumn(table: "dbo.Flights", name: "ArrivalId", newName: "Arrival_Id");
            RenameColumn(table: "dbo.Flights", name: "AirlineId", newName: "Airline_Id");
            RenameColumn(table: "dbo.Flights", name: "AircraftId", newName: "Aircraft_Id");
        }
    }
}
