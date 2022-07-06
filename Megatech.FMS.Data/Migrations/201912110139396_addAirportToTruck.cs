namespace FMS.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addAirportToTruck : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Trucks", "Airport", c => c.Int());
            AddColumn("dbo.Trucks", "CurrentAirport_Id", c => c.Int());
            CreateIndex("dbo.Trucks", "CurrentAirport_Id");
            AddForeignKey("dbo.Trucks", "CurrentAirport_Id", "dbo.Airports", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Trucks", "CurrentAirport_Id", "dbo.Airports");
            DropIndex("dbo.Trucks", new[] { "CurrentAirport_Id" });
            DropColumn("dbo.Trucks", "CurrentAirport_Id");
            DropColumn("dbo.Trucks", "Airport");
        }
    }
}
