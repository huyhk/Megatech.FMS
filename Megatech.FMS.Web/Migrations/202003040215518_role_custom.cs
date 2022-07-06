namespace Megatech.FMS.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class role_custom : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetRoles", "DisplayName", c => c.Int());
            AddColumn("dbo.AspNetRoles", "Discriminator", c => c.String(nullable: false, maxLength: 128));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetRoles", "Discriminator");
            DropColumn("dbo.AspNetRoles", "DisplayName");
        }
    }
}
