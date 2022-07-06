namespace FMS.Data.Migrations
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Infrastructure.Annotations;
    using System.Data.Entity.Migrations;
    
    public partial class FHSImport : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Receipts",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Number = c.String(),
                        Date = c.DateTime(nullable: false),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(nullable: false),
                        Gallon = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Volume = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Weight = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CustomerId = c.Int(nullable: false),
                        CustomerCode = c.String(),
                        CustomerName = c.String(),
                        CustomerAddress = c.String(),
                        TaxCode = c.String(),
                        FlightId = c.Int(nullable: false),
                        RouteName = c.String(),
                        AircraftCode = c.String(),
                        ReturnAmount = c.Decimal(precision: 18, scale: 2),
                        DefuelingNo = c.String(),
                        InvoiceSplit = c.Boolean(nullable: false),
                        SplitAmount = c.Decimal(precision: 18, scale: 2),
                        Image = c.Binary(storeType: "image"),
                        Signature = c.Binary(storeType: "image"),
                        SellerImage = c.Binary(storeType: "image"),
                        IsReturn = c.Boolean(),
                        RefuelCompany = c.Int(),
                        FlightType = c.Int(),
                        FlightCode = c.String(),
                        AircraftType = c.String(),
                        Manual = c.Boolean(),
                        CustomerType = c.Int(),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Receipt_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airlines", t => t.CustomerId, cascadeDelete: true)
                .ForeignKey("dbo.Flights", t => t.FlightId, cascadeDelete: true)
                .Index(t => t.CustomerId)
                .Index(t => t.FlightId);
            
            CreateTable(
                "dbo.Invoices",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        InvoiceNumber = c.String(),
                        InvoiceFormId = c.Int(),
                        FormNo = c.String(),
                        SignNo = c.String(),
                        Date = c.DateTime(),
                        Gallon = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Volume = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Weight = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TaxRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SaleAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Temperature = c.Decimal(precision: 18, scale: 2),
                        Density = c.Decimal(precision: 18, scale: 2),
                        InvoiceType = c.Int(nullable: false),
                        Exported = c.Boolean(),
                        Currency = c.Int(nullable: false),
                        Unit = c.Int(nullable: false),
                        Price = c.Decimal(precision: 18, scale: 2),
                        FlightId = c.Int(nullable: false),
                        TotalAmount = c.Decimal(precision: 18, scale: 2),
                        CustomerId = c.Int(nullable: false),
                        CustomerCode = c.String(),
                        CustomerName = c.String(),
                        CustomerAddress = c.String(),
                        CustomerEmail = c.String(),
                        TaxCode = c.String(),
                        Image = c.Binary(storeType: "image"),
                        Signature = c.Binary(storeType: "image"),
                        CustomerType = c.Int(nullable: false),
                        FlightType = c.Int(nullable: false),
                        BillNo = c.String(),
                        BillDate = c.DateTime(nullable: false),
                        Exported_AITS = c.Boolean(),
                        Exported_AITS_Date = c.DateTime(),
                        Exported_OMEGA = c.Boolean(),
                        Exported_OMEGA_Date = c.DateTime(),
                        LoginTaxCode = c.String(),
                        DefuelingNo = c.String(),
                        ReceiptId = c.Int(),
                        UniqueId = c.Guid(),
                        ExchangeRate = c.Decimal(precision: 18, scale: 2),
                        Cancelled = c.Boolean(),
                        CancelledTime = c.DateTime(),
                        CancelReason = c.String(),
                        RequestCancel = c.Boolean(),
                        RefuelCompany = c.Int(),
                        RouteName = c.String(),
                        AircraftType = c.String(),
                        AircraftCode = c.String(),
                        FlightCode = c.String(),
                        Manual = c.Boolean(),
                        IsElectronic = c.Boolean(),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Invoice_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airlines", t => t.CustomerId, cascadeDelete: true)
                .ForeignKey("dbo.Flights", t => t.FlightId, cascadeDelete: true)
                .ForeignKey("dbo.Receipts", t => t.ReceiptId)
                .Index(t => t.FlightId)
                .Index(t => t.CustomerId)
                .Index(t => t.ReceiptId);
            
            CreateTable(
                "dbo.InvoiceItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        InvoiceId = c.Int(nullable: false),
                        RefuelItemId = c.Int(),
                        TruckId = c.Int(),
                        TruckNo = c.String(),
                        QCNo = c.String(),
                        Gallon = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Volume = c.Decimal(precision: 18, scale: 2),
                        Weight = c.Decimal(precision: 18, scale: 2),
                        StartNumber = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EndNumber = c.Decimal(nullable: false, precision: 18, scale: 2),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(nullable: false),
                        Temperature = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Density = c.Decimal(nullable: false, precision: 18, scale: 4),
                        WeightNote = c.String(),
                        DriverId = c.Int(),
                        OperatorId = c.Int(),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SaleAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_InvoiceItem_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.DriverId)
                .ForeignKey("dbo.Invoices", t => t.InvoiceId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.OperatorId)
                .Index(t => t.InvoiceId)
                .Index(t => t.DriverId)
                .Index(t => t.OperatorId);
            
            CreateTable(
                "dbo.ReceiptItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TruckId = c.Int(),
                        RefuelId = c.Int(),
                        ReceiptId = c.Int(nullable: false),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(nullable: false),
                        StartNumber = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EndNumber = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Gallon = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Volume = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Weight = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Temperature = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Density = c.Decimal(nullable: false, precision: 18, scale: 4),
                        QualityNo = c.String(),
                        DriverId = c.Int(),
                        OperatorId = c.Int(),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_ReceiptItem_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Receipts", t => t.ReceiptId, cascadeDelete: true)
                .ForeignKey("dbo.Trucks", t => t.TruckId)
                .Index(t => t.TruckId)
                .Index(t => t.ReceiptId);
            
            CreateTable(
                "dbo.BM2505",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TruckId = c.Int(nullable: false),
                        Time = c.DateTime(nullable: false),
                        FlightId = c.Int(),
                        TankNo = c.String(),
                        RTCNo = c.String(),
                        Temperature = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Density = c.Decimal(nullable: false, precision: 18, scale: 4),
                        Density15 = c.Decimal(nullable: false, precision: 18, scale: 4),
                        DensityCheck = c.Boolean(nullable: false),
                        AppearanceCheck = c.String(),
                        WaterCheck = c.Boolean(nullable: false),
                        PressureDiff = c.String(),
                        HosePressure = c.String(),
                        OperatorId = c.Int(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_BM2505_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Flights", t => t.FlightId)
                .ForeignKey("dbo.Users", t => t.OperatorId, cascadeDelete: true)
                .ForeignKey("dbo.Trucks", t => t.TruckId, cascadeDelete: true)
                .Index(t => t.TruckId)
                .Index(t => t.FlightId)
                .Index(t => t.OperatorId);
            
            CreateTable(
                "dbo.FHSImports",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AirlineCode = c.String(),
                        AirlineName = c.String(),
                        Address = c.String(),
                        TaxCode = c.String(),
                        Email = c.String(),
                        FlightCode = c.String(),
                        ArrivalTime = c.DateTime(nullable: false),
                        DepartureTime = c.DateTime(nullable: false),
                        RouteName = c.String(),
                        AircraftCode = c.String(),
                        AircraftType = c.String(),
                        IsInternational = c.Boolean(nullable: false),
                        ReceiptNumber = c.String(),
                        UserImportedId = c.Int(nullable: false),
                        DateImported = c.DateTime(nullable: false),
                        ResultCode = c.Int(nullable: false),
                        Result = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_FHSImport_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.FHSImportItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TruckCode = c.String(),
                        Gallon = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Litter = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Kg = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Temperature = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Density = c.Decimal(nullable: false, precision: 18, scale: 4),
                        QualityNo = c.String(),
                        Techlog = c.Decimal(nullable: false, precision: 18, scale: 2),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(nullable: false),
                        StartNumber = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EndNumber = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                        FHSImport_Id = c.Int(),
                    },
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_FHSImportItem_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.FHSImports", t => t.FHSImport_Id)
                .Index(t => t.FHSImport_Id);
            
            AlterTableAnnotations(
                "dbo.Actions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ActionName = c.String(),
                        DisplayName = c.String(),
                        ControllerId = c.Int(nullable: false),
                    },
                annotations: new Dictionary<string, AnnotationValues>
                {
                    { 
                        "DynamicFilter_ActionInfo_IsNotDeleted",
                        new AnnotationValues(oldValue: "EntityFramework.DynamicFilters.DynamicFilterDefinition", newValue: null)
                    },
                });
            
            AlterTableAnnotations(
                "dbo.Controllers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ControllerName = c.String(),
                        DisplayName = c.String(),
                    },
                annotations: new Dictionary<string, AnnotationValues>
                {
                    { 
                        "DynamicFilter_ControllerInfo_IsNotDeleted",
                        new AnnotationValues(oldValue: "EntityFramework.DynamicFilters.DynamicFilterDefinition", newValue: null)
                    },
                });
            
            AlterTableAnnotations(
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
                        AirlineId = c.Int(),
                        AircraftId = c.Int(),
                        AircraftCode = c.String(),
                        AircraftType = c.String(),
                        EstimateAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        RefuelScheduledTime = c.DateTime(),
                        RefuelScheduledHours = c.DateTime(),
                        RefuelTime = c.DateTime(),
                        ParkingLotId = c.Int(),
                        Parking = c.String(),
                        DriverName = c.String(),
                        TechnicalerName = c.String(),
                        Shift = c.String(),
                        ShiftStartTime = c.DateTime(),
                        ShiftEndTime = c.DateTime(),
                        AirportName = c.String(),
                        TruckName = c.String(),
                        Status = c.Int(nullable: false),
                        SortOrder = c.Int(),
                        AirportId = c.Int(),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(nullable: false),
                        Note = c.String(),
                        Follow = c.Int(),
                        InvoiceNameCharter = c.String(),
                        CreatedLocation = c.Int(nullable: false),
                        FlightType = c.Int(nullable: false),
                        FlightCarry = c.Int(nullable: false),
                        CreateType = c.Int(),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, AnnotationValues>
                {
                    { 
                        "DynamicFilter_Flight_UserAirport",
                        new AnnotationValues(oldValue: null, newValue: "EntityFramework.DynamicFilters.DynamicFilterDefinition")
                    },
                });
            
            AlterTableAnnotations(
                "dbo.InvoiceForms",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AirportId = c.Int(nullable: false),
                        InvoiceType = c.Int(nullable: false),
                        FormNo = c.String(),
                        Sign = c.String(),
                        StartDate = c.DateTime(),
                        IsDefault = c.Boolean(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, AnnotationValues>
                {
                    { 
                        "DynamicFilter_InvoiceForm_UserAirport",
                        new AnnotationValues(oldValue: null, newValue: "EntityFramework.DynamicFilters.DynamicFilterDefinition")
                    },
                });
            
            AddColumn("dbo.Users", "Branch", c => c.Int(nullable: false));
            AddColumn("dbo.Users", "Group", c => c.String());
            AddColumn("dbo.Users", "ImportName", c => c.String());
            AddColumn("dbo.Airports", "TaxCode", c => c.String());
            AddColumn("dbo.Airports", "InvoiceName", c => c.String());
            AddColumn("dbo.Airlines", "InvoiceCode", c => c.String());
            AddColumn("dbo.Airlines", "Email", c => c.String());
            AddColumn("dbo.Flights", "SortOrder", c => c.Int());
            AddColumn("dbo.Flights", "Follow", c => c.Int());
            AddColumn("dbo.Flights", "FlightCarry", c => c.Int(nullable: false));
            AddColumn("dbo.Flights", "CreateType", c => c.Int());
            AddColumn("dbo.RefuelItems", "OriginalEndMeter", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.RefuelItems", "CreateType", c => c.Int());
            AddColumn("dbo.RefuelItems", "TechLog", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.RefuelItems", "ReturnInvoiceNumber", c => c.String());
            AddColumn("dbo.RefuelItems", "Volume", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.RefuelItems", "Weight", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.RefuelItems", "OriginalTemperature", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.RefuelItems", "OriginalDensity", c => c.Decimal(precision: 18, scale: 4));
            AddColumn("dbo.RefuelItems", "OriginalGallon", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.RefuelItems", "BM2508Result", c => c.Int());
            AddColumn("dbo.RefuelItems", "UniqueId", c => c.Guid());
            AddColumn("dbo.RefuelItems", "ReceiptId", c => c.Int());
            AddColumn("dbo.RefuelItems", "ReceiptCount", c => c.Int());
            AddColumn("dbo.RefuelItems", "ReturnUnit", c => c.Int());
            AddColumn("dbo.Trucks", "RefuelCompany", c => c.Int());
            AddColumn("dbo.ProductPrices", "ExchangeRate", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.TruckFuels", "AccumulateRefuelAmount", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.TruckFuels", "Time", c => c.DateTime(nullable: false));
            AddColumn("dbo.TruckFuels", "TankNo", c => c.String());
            AddColumn("dbo.TruckFuels", "TicketNo", c => c.String());
            AddColumn("dbo.TruckFuels", "OperatorId", c => c.Int());
            AddColumn("dbo.TruckFuels", "MaintenanceStaff", c => c.String());
            AlterColumn("dbo.Flights", "Code", c => c.String());
            AlterColumn("dbo.RefuelItems", "Exported", c => c.Boolean());
            CreateIndex("dbo.RefuelItems", "ReceiptId");
            CreateIndex("dbo.TruckFuels", "OperatorId");
            AddForeignKey("dbo.RefuelItems", "ReceiptId", "dbo.Receipts", "Id");
            AddForeignKey("dbo.TruckFuels", "OperatorId", "dbo.Users", "Id");
            DropColumn("dbo.Actions", "DateCreated");
            DropColumn("dbo.Actions", "DateUpdated");
            DropColumn("dbo.Actions", "UserCreatedId");
            DropColumn("dbo.Actions", "UserUpdatedId");
            DropColumn("dbo.Actions", "IsDeleted");
            DropColumn("dbo.Actions", "DateDeleted");
            DropColumn("dbo.Actions", "UserDeletedId");
            DropColumn("dbo.Controllers", "DateCreated");
            DropColumn("dbo.Controllers", "DateUpdated");
            DropColumn("dbo.Controllers", "UserCreatedId");
            DropColumn("dbo.Controllers", "UserUpdatedId");
            DropColumn("dbo.Controllers", "IsDeleted");
            DropColumn("dbo.Controllers", "DateDeleted");
            DropColumn("dbo.Controllers", "UserDeletedId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Controllers", "UserDeletedId", c => c.Int());
            AddColumn("dbo.Controllers", "DateDeleted", c => c.DateTime());
            AddColumn("dbo.Controllers", "IsDeleted", c => c.Boolean(nullable: false));
            AddColumn("dbo.Controllers", "UserUpdatedId", c => c.Int());
            AddColumn("dbo.Controllers", "UserCreatedId", c => c.Int());
            AddColumn("dbo.Controllers", "DateUpdated", c => c.DateTime(nullable: false));
            AddColumn("dbo.Controllers", "DateCreated", c => c.DateTime(nullable: false));
            AddColumn("dbo.Actions", "UserDeletedId", c => c.Int());
            AddColumn("dbo.Actions", "DateDeleted", c => c.DateTime());
            AddColumn("dbo.Actions", "IsDeleted", c => c.Boolean(nullable: false));
            AddColumn("dbo.Actions", "UserUpdatedId", c => c.Int());
            AddColumn("dbo.Actions", "UserCreatedId", c => c.Int());
            AddColumn("dbo.Actions", "DateUpdated", c => c.DateTime(nullable: false));
            AddColumn("dbo.Actions", "DateCreated", c => c.DateTime(nullable: false));
            DropForeignKey("dbo.TruckFuels", "OperatorId", "dbo.Users");
            DropForeignKey("dbo.FHSImportItems", "FHSImport_Id", "dbo.FHSImports");
            DropForeignKey("dbo.BM2505", "TruckId", "dbo.Trucks");
            DropForeignKey("dbo.BM2505", "OperatorId", "dbo.Users");
            DropForeignKey("dbo.BM2505", "FlightId", "dbo.Flights");
            DropForeignKey("dbo.RefuelItems", "ReceiptId", "dbo.Receipts");
            DropForeignKey("dbo.ReceiptItems", "TruckId", "dbo.Trucks");
            DropForeignKey("dbo.ReceiptItems", "ReceiptId", "dbo.Receipts");
            DropForeignKey("dbo.Invoices", "ReceiptId", "dbo.Receipts");
            DropForeignKey("dbo.InvoiceItems", "OperatorId", "dbo.Users");
            DropForeignKey("dbo.InvoiceItems", "InvoiceId", "dbo.Invoices");
            DropForeignKey("dbo.InvoiceItems", "DriverId", "dbo.Users");
            DropForeignKey("dbo.Invoices", "FlightId", "dbo.Flights");
            DropForeignKey("dbo.Invoices", "CustomerId", "dbo.Airlines");
            DropForeignKey("dbo.Receipts", "FlightId", "dbo.Flights");
            DropForeignKey("dbo.Receipts", "CustomerId", "dbo.Airlines");
            DropIndex("dbo.TruckFuels", new[] { "OperatorId" });
            DropIndex("dbo.FHSImportItems", new[] { "FHSImport_Id" });
            DropIndex("dbo.BM2505", new[] { "OperatorId" });
            DropIndex("dbo.BM2505", new[] { "FlightId" });
            DropIndex("dbo.BM2505", new[] { "TruckId" });
            DropIndex("dbo.ReceiptItems", new[] { "ReceiptId" });
            DropIndex("dbo.ReceiptItems", new[] { "TruckId" });
            DropIndex("dbo.InvoiceItems", new[] { "OperatorId" });
            DropIndex("dbo.InvoiceItems", new[] { "DriverId" });
            DropIndex("dbo.InvoiceItems", new[] { "InvoiceId" });
            DropIndex("dbo.Invoices", new[] { "ReceiptId" });
            DropIndex("dbo.Invoices", new[] { "CustomerId" });
            DropIndex("dbo.Invoices", new[] { "FlightId" });
            DropIndex("dbo.Receipts", new[] { "FlightId" });
            DropIndex("dbo.Receipts", new[] { "CustomerId" });
            DropIndex("dbo.RefuelItems", new[] { "ReceiptId" });
            AlterColumn("dbo.RefuelItems", "Exported", c => c.Boolean(nullable: false));
            AlterColumn("dbo.Flights", "Code", c => c.String(nullable: false));
            DropColumn("dbo.TruckFuels", "MaintenanceStaff");
            DropColumn("dbo.TruckFuels", "OperatorId");
            DropColumn("dbo.TruckFuels", "TicketNo");
            DropColumn("dbo.TruckFuels", "TankNo");
            DropColumn("dbo.TruckFuels", "Time");
            DropColumn("dbo.TruckFuels", "AccumulateRefuelAmount");
            DropColumn("dbo.ProductPrices", "ExchangeRate");
            DropColumn("dbo.Trucks", "RefuelCompany");
            DropColumn("dbo.RefuelItems", "ReturnUnit");
            DropColumn("dbo.RefuelItems", "ReceiptCount");
            DropColumn("dbo.RefuelItems", "ReceiptId");
            DropColumn("dbo.RefuelItems", "UniqueId");
            DropColumn("dbo.RefuelItems", "BM2508Result");
            DropColumn("dbo.RefuelItems", "OriginalGallon");
            DropColumn("dbo.RefuelItems", "OriginalDensity");
            DropColumn("dbo.RefuelItems", "OriginalTemperature");
            DropColumn("dbo.RefuelItems", "Weight");
            DropColumn("dbo.RefuelItems", "Volume");
            DropColumn("dbo.RefuelItems", "ReturnInvoiceNumber");
            DropColumn("dbo.RefuelItems", "TechLog");
            DropColumn("dbo.RefuelItems", "CreateType");
            DropColumn("dbo.RefuelItems", "OriginalEndMeter");
            DropColumn("dbo.Flights", "CreateType");
            DropColumn("dbo.Flights", "FlightCarry");
            DropColumn("dbo.Flights", "Follow");
            DropColumn("dbo.Flights", "SortOrder");
            DropColumn("dbo.Airlines", "Email");
            DropColumn("dbo.Airlines", "InvoiceCode");
            DropColumn("dbo.Airports", "InvoiceName");
            DropColumn("dbo.Airports", "TaxCode");
            DropColumn("dbo.Users", "ImportName");
            DropColumn("dbo.Users", "Group");
            DropColumn("dbo.Users", "Branch");
            AlterTableAnnotations(
                "dbo.InvoiceForms",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AirportId = c.Int(nullable: false),
                        InvoiceType = c.Int(nullable: false),
                        FormNo = c.String(),
                        Sign = c.String(),
                        StartDate = c.DateTime(),
                        IsDefault = c.Boolean(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, AnnotationValues>
                {
                    { 
                        "DynamicFilter_InvoiceForm_UserAirport",
                        new AnnotationValues(oldValue: "EntityFramework.DynamicFilters.DynamicFilterDefinition", newValue: null)
                    },
                });
            
            AlterTableAnnotations(
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
                        AirlineId = c.Int(),
                        AircraftId = c.Int(),
                        AircraftCode = c.String(),
                        AircraftType = c.String(),
                        EstimateAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        RefuelScheduledTime = c.DateTime(),
                        RefuelScheduledHours = c.DateTime(),
                        RefuelTime = c.DateTime(),
                        ParkingLotId = c.Int(),
                        Parking = c.String(),
                        DriverName = c.String(),
                        TechnicalerName = c.String(),
                        Shift = c.String(),
                        ShiftStartTime = c.DateTime(),
                        ShiftEndTime = c.DateTime(),
                        AirportName = c.String(),
                        TruckName = c.String(),
                        Status = c.Int(nullable: false),
                        SortOrder = c.Int(),
                        AirportId = c.Int(),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(nullable: false),
                        Note = c.String(),
                        Follow = c.Int(),
                        InvoiceNameCharter = c.String(),
                        CreatedLocation = c.Int(nullable: false),
                        FlightType = c.Int(nullable: false),
                        FlightCarry = c.Int(nullable: false),
                        CreateType = c.Int(),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, AnnotationValues>
                {
                    { 
                        "DynamicFilter_Flight_UserAirport",
                        new AnnotationValues(oldValue: "EntityFramework.DynamicFilters.DynamicFilterDefinition", newValue: null)
                    },
                });
            
            AlterTableAnnotations(
                "dbo.Controllers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ControllerName = c.String(),
                        DisplayName = c.String(),
                    },
                annotations: new Dictionary<string, AnnotationValues>
                {
                    { 
                        "DynamicFilter_ControllerInfo_IsNotDeleted",
                        new AnnotationValues(oldValue: null, newValue: "EntityFramework.DynamicFilters.DynamicFilterDefinition")
                    },
                });
            
            AlterTableAnnotations(
                "dbo.Actions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ActionName = c.String(),
                        DisplayName = c.String(),
                        ControllerId = c.Int(nullable: false),
                    },
                annotations: new Dictionary<string, AnnotationValues>
                {
                    { 
                        "DynamicFilter_ActionInfo_IsNotDeleted",
                        new AnnotationValues(oldValue: null, newValue: "EntityFramework.DynamicFilters.DynamicFilterDefinition")
                    },
                });
            
            DropTable("dbo.FHSImportItems",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_FHSImportItem_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.FHSImports",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_FHSImport_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.BM2505",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_BM2505_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.ReceiptItems",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_ReceiptItem_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.InvoiceItems",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_InvoiceItem_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Invoices",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Invoice_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Receipts",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Receipt_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
        }
    }
}
