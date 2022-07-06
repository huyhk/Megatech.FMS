namespace FMS.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class truck : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Trucks", "DeviceId", "dbo.Devices");
            DropIndex("dbo.Trucks", new[] { "DeviceId" });
            AlterColumn("dbo.Trucks", "DeviceId", c => c.Int());
            CreateIndex("dbo.Trucks", "DeviceId");
            AddForeignKey("dbo.Trucks", "DeviceId", "dbo.Devices", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Trucks", "DeviceId", "dbo.Devices");
            DropIndex("dbo.Trucks", new[] { "DeviceId" });
            AlterColumn("dbo.Trucks", "DeviceId", c => c.Int(nullable: false));
            CreateIndex("dbo.Trucks", "DeviceId");
            AddForeignKey("dbo.Trucks", "DeviceId", "dbo.Devices", "Id", cascadeDelete: true);
        }
    }
}
