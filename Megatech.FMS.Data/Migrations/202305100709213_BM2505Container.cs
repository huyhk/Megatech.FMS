namespace FMS.Data.Migrations
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Infrastructure.Annotations;
    using System.Data.Entity.Migrations;
    
    public partial class BM2505Container : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Actions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ActionName = c.String(),
                        DisplayName = c.String(),
                        ControllerId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Controllers", t => t.ControllerId, cascadeDelete: true)
                .Index(t => t.ControllerId);
            
            CreateTable(
                "dbo.Controllers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ControllerName = c.String(),
                        DisplayName = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Roles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserName = c.String(),
                        FullName = c.String(nullable: false),
                        Email = c.String(),
                        IsEnabled = c.Boolean(nullable: false),
                        LastLogin = c.DateTime(),
                        AirportId = c.Int(),
                        Branch = c.Int(nullable: false),
                        DisplayName = c.String(),
                        Group = c.String(),
                        ImportName = c.String(),
                        Permission = c.Int(nullable: false),
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
                    { "DynamicFilter_User_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airports", t => t.AirportId)
                .Index(t => t.AirportId);
            
            CreateTable(
                "dbo.Airports",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false),
                        Name = c.String(nullable: false),
                        Address = c.String(),
                        SetTime = c.Int(),
                        Branch = c.Int(nullable: false),
                        DepotType = c.Int(),
                        TaxCode = c.String(),
                        InvoiceName = c.String(),
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
                    { "DynamicFilter_Airport_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Aircraft",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Code = c.String(nullable: false),
                        AircraftType = c.String(nullable: false),
                        CustomerId = c.Int(),
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
                    { "DynamicFilter_Aircraft_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airlines", t => t.CustomerId)
                .Index(t => t.CustomerId);
            
            CreateTable(
                "dbo.Airlines",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Pattern = c.String(),
                        IsCharter = c.Boolean(),
                        AirlineType = c.Int(),
                        Unit = c.Int(),
                        InvoiceCode = c.String(),
                        DomesticInvoice = c.Boolean(),
                        Branch = c.Int(),
                        PaymentMethod = c.Int(),
                        Code = c.String(),
                        Name = c.String(),
                        TaxCode = c.String(),
                        Address = c.String(),
                        Email = c.String(),
                        InvoiceName = c.String(),
                        InvoiceTaxCode = c.String(),
                        InvoiceAddress = c.String(),
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
                    { "DynamicFilter_Airline_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
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
                        UniqueId = c.Guid(),
                        LastUpdateDevice = c.String(),
                        LegNo = c.String(),
                        LegUpdateNo = c.String(),
                        IsOutRefuel = c.Boolean(),
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
                    { "DynamicFilter_Flight_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                    { "DynamicFilter_Flight_UserAirport", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Aircraft", t => t.AircraftId)
                .ForeignKey("dbo.Airlines", t => t.AirlineId)
                .ForeignKey("dbo.Airports", t => t.AirportId)
                .ForeignKey("dbo.ParkingLots", t => t.ParkingLotId)
                .Index(t => t.AirlineId)
                .Index(t => t.AircraftId)
                .Index(t => t.ParkingLotId)
                .Index(t => t.AirportId);
            
            CreateTable(
                "dbo.ParkingLots",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AirportId = c.Int(nullable: false),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(maxLength: 100),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                        Location_Id = c.Int(),
                    },
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_ParkingLot_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airports", t => t.AirportId, cascadeDelete: true)
                .ForeignKey("dbo.GeoLocations", t => t.Location_Id)
                .Index(t => t.AirportId)
                .Index(t => t.Location_Id);
            
            CreateTable(
                "dbo.GeoLocations",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Longitude = c.Single(nullable: false),
                        Latitude = c.Single(nullable: false),
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
                    { "DynamicFilter_GeoLocation_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.RefuelItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TruckId = c.Int(nullable: false),
                        StartTime = c.DateTime(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Temperature = c.Decimal(nullable: false, precision: 18, scale: 2),
                        StartNumber = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EndNumber = c.Decimal(nullable: false, precision: 18, scale: 2),
                        OriginalEndMeter = c.Decimal(precision: 18, scale: 2),
                        CreateType = c.Int(),
                        Completed = c.Boolean(nullable: false),
                        Printed = c.Boolean(nullable: false),
                        EndTime = c.DateTime(),
                        DriverId = c.Int(),
                        OperatorId = c.Int(),
                        DeviceStartTime = c.DateTime(),
                        DeviceEndTime = c.DateTime(),
                        Gallon = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ReturnAmount = c.Decimal(precision: 18, scale: 2),
                        InvoiceNumber = c.String(),
                        Status = c.Int(nullable: false),
                        ManualTemperature = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Density = c.Decimal(nullable: false, precision: 18, scale: 4),
                        FlightId = c.Int(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Currency = c.Int(nullable: false),
                        Unit = c.Int(),
                        QCNo = c.String(),
                        TaxRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ApprovalUserId = c.Int(),
                        ApprovalStatus = c.Int(nullable: false),
                        ApprovalNote = c.String(),
                        CreatedLocation = c.Int(nullable: false),
                        RefuelItemType = c.Int(nullable: false),
                        WeightNote = c.String(),
                        TechLog = c.Decimal(precision: 18, scale: 2),
                        Exported = c.Boolean(),
                        ExportExtract = c.Boolean(),
                        PrintTemplate = c.Int(),
                        InvoiceFormId = c.Int(),
                        ReturnInvoiceNumber = c.String(),
                        Volume = c.Decimal(precision: 18, scale: 2),
                        Weight = c.Decimal(precision: 18, scale: 2),
                        OriginalTemperature = c.Decimal(precision: 18, scale: 2),
                        OriginalDensity = c.Decimal(precision: 18, scale: 4),
                        OriginalGallon = c.Decimal(precision: 18, scale: 2),
                        BM2508Result = c.Int(),
                        UniqueId = c.Guid(),
                        ReceiptId = c.Int(),
                        ReceiptCount = c.Int(),
                        ReceiptNumber = c.String(),
                        ReceiptUniqueId = c.Guid(),
                        ReturnUnit = c.Int(),
                        LastUpdateDevice = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                        Refuel_Id = c.Int(),
                    },
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_RefuelItem_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.DriverId)
                .ForeignKey("dbo.Flights", t => t.FlightId, cascadeDelete: true)
                .ForeignKey("dbo.InvoiceForms", t => t.InvoiceFormId)
                .ForeignKey("dbo.Users", t => t.OperatorId)
                .ForeignKey("dbo.Receipts", t => t.ReceiptId)
                .ForeignKey("dbo.Trucks", t => t.TruckId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserUpdatedId)
                .ForeignKey("dbo.Refuels", t => t.Refuel_Id)
                .Index(t => t.TruckId)
                .Index(t => t.DriverId)
                .Index(t => t.OperatorId)
                .Index(t => t.FlightId)
                .Index(t => t.InvoiceFormId)
                .Index(t => t.ReceiptId)
                .Index(t => t.UserUpdatedId)
                .Index(t => t.Refuel_Id);
            
            CreateTable(
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
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_InvoiceForm_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                    { "DynamicFilter_InvoiceForm_UserAirport", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airports", t => t.AirportId, cascadeDelete: true)
                .Index(t => t.AirportId);
            
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
                        ImagePath = c.String(),
                        SignaturePath = c.String(),
                        SellerPath = c.String(),
                        TechLog = c.Decimal(precision: 18, scale: 2),
                        ReplaceNumber = c.String(),
                        IsReuse = c.Boolean(),
                        UniqueId = c.Guid(),
                        ReplacedId = c.String(),
                        IsThermal = c.Boolean(),
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
                        Temperature = c.Decimal(precision: 18, scale: 2),
                        Density = c.Decimal(precision: 18, scale: 2),
                        InvoiceType = c.Int(nullable: false),
                        Exported = c.Boolean(),
                        Currency = c.Int(nullable: false),
                        Unit = c.Int(nullable: false),
                        Price = c.Decimal(precision: 18, scale: 2),
                        FlightId = c.Int(nullable: false),
                        TaxRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SaleAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TotalAmount = c.Decimal(precision: 18, scale: 2),
                        GreenTax = c.Decimal(precision: 18, scale: 2),
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
                        ReplaceId = c.Int(),
                        RefuelCompany = c.Int(),
                        RouteName = c.String(),
                        AircraftType = c.String(),
                        AircraftCode = c.String(),
                        FlightCode = c.String(),
                        Manual = c.Boolean(),
                        IsElectronic = c.Boolean(),
                        FHSUniqueId = c.Guid(),
                        TechLog = c.Decimal(precision: 18, scale: 2),
                        CCID = c.String(),
                        SignType = c.Int(),
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
                .ForeignKey("dbo.Invoices", t => t.ReplaceId)
                .Index(t => t.FlightId)
                .Index(t => t.CustomerId)
                .Index(t => t.ReceiptId)
                .Index(t => t.ReplaceId);
            
            CreateTable(
                "dbo.InvoiceItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        InvoiceId = c.Int(nullable: false),
                        RefuelUniqueId = c.Guid(),
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
                .ForeignKey("dbo.RefuelItems", t => t.RefuelItemId)
                .ForeignKey("dbo.Trucks", t => t.TruckId)
                .Index(t => t.InvoiceId)
                .Index(t => t.RefuelItemId)
                .Index(t => t.TruckId)
                .Index(t => t.DriverId)
                .Index(t => t.OperatorId);
            
            CreateTable(
                "dbo.Trucks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DeviceId = c.Int(),
                        TabletId = c.String(),
                        Code = c.String(nullable: false),
                        MaxAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CurrentAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        AirportId = c.Int(),
                        Unit = c.String(),
                        DeviceIP = c.String(),
                        PrinterIP = c.String(),
                        TabletSerial = c.String(),
                        DeviceSerial = c.String(),
                        RefuelCompany = c.Int(),
                        ReceiptCount = c.Int(),
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
                    { "DynamicFilter_Truck_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airports", t => t.AirportId)
                .ForeignKey("dbo.Devices", t => t.DeviceId)
                .Index(t => t.DeviceId)
                .Index(t => t.AirportId);
            
            CreateTable(
                "dbo.Devices",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SerialNumber = c.String(),
                        Status = c.Int(nullable: false),
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
                    { "DynamicFilter_Device_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ReceiptItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TruckId = c.Int(),
                        RefuelUniqueId = c.Guid(),
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
                .ForeignKey("dbo.RefuelItems", t => t.RefuelId)
                .ForeignKey("dbo.Trucks", t => t.TruckId)
                .Index(t => t.TruckId)
                .Index(t => t.RefuelId)
                .Index(t => t.ReceiptId);
            
            CreateTable(
                "dbo.Reviews",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FlightId = c.Int(nullable: false),
                        Rate = c.Int(nullable: false),
                        BadReason = c.Int(nullable: false),
                        OtherReason = c.String(),
                        ReviewDate = c.DateTime(nullable: false),
                        UniqueId = c.Guid(nullable: false),
                        ImagePath = c.String(),
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
                    { "DynamicFilter_Review_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Flights", t => t.FlightId, cascadeDelete: true)
                .Index(t => t.FlightId);
            
            CreateTable(
                "dbo.BM2505Container",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
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
                    { "DynamicFilter_BM2505Container_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.BM2505",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ReportType = c.Int(),
                        TruckId = c.Int(nullable: false),
                        Time = c.DateTime(nullable: false),
                        FlightId = c.Int(),
                        TankNo = c.String(),
                        ContainerId = c.Int(),
                        Depot = c.String(),
                        RTCNo = c.String(),
                        Temperature = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Density = c.Decimal(nullable: false, precision: 18, scale: 4),
                        Density15 = c.Decimal(nullable: false, precision: 18, scale: 4),
                        DensityDiff = c.Decimal(precision: 18, scale: 4),
                        DensityCheck = c.Boolean(nullable: false),
                        AppearanceCheck = c.String(),
                        WaterCheck = c.Boolean(nullable: false),
                        PressureDiff = c.String(),
                        HosePressure = c.String(),
                        OperatorId = c.Int(nullable: false),
                        Note = c.String(),
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
                .ForeignKey("dbo.BM2505Container", t => t.ContainerId)
                .ForeignKey("dbo.Flights", t => t.FlightId)
                .ForeignKey("dbo.Users", t => t.OperatorId, cascadeDelete: true)
                .ForeignKey("dbo.Trucks", t => t.TruckId, cascadeDelete: true)
                .Index(t => t.TruckId)
                .Index(t => t.FlightId)
                .Index(t => t.ContainerId)
                .Index(t => t.OperatorId);
            
            CreateTable(
                "dbo.ChangeLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EntityName = c.String(),
                        EntityDisplay = c.String(),
                        PropertyName = c.String(),
                        KeyValues = c.String(),
                        OldValues = c.String(),
                        NewValues = c.String(),
                        DateChanged = c.DateTime(nullable: false),
                        UserUpdatedId = c.Int(),
                        UserUpdatedName = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CheckTrucks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Hours = c.DateTime(),
                        KmNumber = c.String(),
                        AirportId = c.Int(),
                        TruckId = c.Int(),
                        ShiftId = c.Int(),
                        DriverId = c.Int(),
                        OperatorId = c.Int(),
                        Result1 = c.String(),
                        Note1 = c.String(),
                        Result2 = c.String(),
                        Note2 = c.String(),
                        Result3 = c.String(),
                        Note3 = c.String(),
                        Result4 = c.String(),
                        Note4 = c.String(),
                        Result5 = c.String(),
                        Note5 = c.String(),
                        Result6 = c.String(),
                        Note6 = c.String(),
                        Result7 = c.String(),
                        Note7 = c.String(),
                        Result8 = c.String(),
                        Note8 = c.String(),
                        Result9 = c.String(),
                        Note9 = c.String(),
                        Result10 = c.String(),
                        Note10 = c.String(),
                        Result11 = c.String(),
                        Note11 = c.String(),
                        Result12 = c.String(),
                        Note12 = c.String(),
                        Result13 = c.String(),
                        Note13 = c.String(),
                        Result14 = c.String(),
                        Note14 = c.String(),
                        Result15 = c.String(),
                        Note15 = c.String(),
                        Result16 = c.String(),
                        Note16 = c.String(),
                        Result17 = c.String(),
                        Note17 = c.String(),
                        Result18 = c.String(),
                        Note18 = c.String(),
                        Result19 = c.String(),
                        Note19 = c.String(),
                        Result20 = c.String(),
                        Note20 = c.String(),
                        Result21 = c.String(),
                        Note21 = c.String(),
                        Result22 = c.String(),
                        Note22 = c.String(),
                        Result23 = c.String(),
                        Note23 = c.String(),
                        Result24 = c.String(),
                        Note24 = c.String(),
                        Result25 = c.String(),
                        Note25 = c.String(),
                        Result26 = c.String(),
                        Note26 = c.String(),
                        Result27 = c.String(),
                        Note27 = c.String(),
                        Result28 = c.String(),
                        Note28 = c.String(),
                        Result29 = c.String(),
                        Note29 = c.String(),
                        Result30 = c.String(),
                        Note30 = c.String(),
                        Result32 = c.String(),
                        Note32 = c.String(),
                        Result31 = c.String(),
                        Note31 = c.String(),
                        Result33 = c.String(),
                        Note33 = c.String(),
                        Result34 = c.String(),
                        Note34 = c.String(),
                        Result35 = c.String(),
                        Note35 = c.String(),
                        Result36 = c.String(),
                        Note36 = c.String(),
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
                    { "DynamicFilter_CheckTruck_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airports", t => t.AirportId)
                .ForeignKey("dbo.Users", t => t.DriverId)
                .ForeignKey("dbo.Users", t => t.OperatorId)
                .ForeignKey("dbo.Shifts", t => t.ShiftId)
                .ForeignKey("dbo.Trucks", t => t.TruckId)
                .Index(t => t.AirportId)
                .Index(t => t.TruckId)
                .Index(t => t.ShiftId)
                .Index(t => t.DriverId)
                .Index(t => t.OperatorId);
            
            CreateTable(
                "dbo.Shifts",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AirportId = c.Int(nullable: false),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(nullable: false),
                        Code = c.String(),
                        Name = c.String(),
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
                    { "DynamicFilter_Shift_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airports", t => t.AirportId, cascadeDelete: true)
                .Index(t => t.AirportId);
            
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
                        TotalGallon = c.Decimal(precision: 18, scale: 2),
                        TotalLiter = c.Decimal(precision: 18, scale: 2),
                        TotalKg = c.Decimal(precision: 18, scale: 2),
                        UserImportedId = c.Int(nullable: false),
                        DateImported = c.DateTime(nullable: false),
                        ResultCode = c.Int(nullable: false),
                        Result = c.String(),
                        UniqueId = c.String(),
                        LocalUniqueId = c.Guid(nullable: false),
                        UpdateId = c.Guid(),
                        Mode = c.Int(nullable: false),
                        ImagePath = c.String(),
                        RefuelCompany = c.Int(),
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
                        RefuelerNo = c.String(),
                        Gallon = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Liter = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Kg = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Temperature = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Density = c.Decimal(nullable: false, precision: 18, scale: 4),
                        CertNo = c.String(),
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
            
            CreateTable(
                "dbo.GreenTaxes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StartDate = c.DateTime(nullable: false),
                        TaxAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
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
                    { "DynamicFilter_GreenTax_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ProductPrices",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CustomerId = c.Int(),
                        ProductId = c.Int(nullable: false),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Currency = c.Int(nullable: false),
                        DepotType = c.Int(),
                        AirlineType = c.Int(),
                        BranchId = c.Int(),
                        Unit = c.Int(),
                        ExchangeRate = c.Decimal(precision: 18, scale: 2),
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
                    { "DynamicFilter_ProductPrice_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airlines", t => t.CustomerId)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.CustomerId)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.Products",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false),
                        Name = c.String(nullable: false),
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
                    { "DynamicFilter_Product_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
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
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Refuel_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Flights", t => t.FlightId, cascadeDelete: true)
                .Index(t => t.FlightId);
            
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
                        UserCreatedId = c.Int(),
                        UserUpdatedId = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        DateDeleted = c.DateTime(),
                        UserDeletedId = c.Int(),
                    },
                annotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Tablet_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TruckAssigns",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TruckId = c.Int(nullable: false),
                        DriverId = c.Int(nullable: false),
                        TechnicalerId = c.Int(nullable: false),
                        ShiftId = c.Int(nullable: false),
                        StartDate = c.DateTime(nullable: false),
                        AirportId = c.Int(nullable: false),
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
                    { "DynamicFilter_TruckAssign_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airports", t => t.AirportId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.DriverId, cascadeDelete: true)
                .ForeignKey("dbo.Shifts", t => t.ShiftId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.TechnicalerId, cascadeDelete: true)
                .ForeignKey("dbo.Trucks", t => t.TruckId, cascadeDelete: true)
                .Index(t => t.TruckId)
                .Index(t => t.DriverId)
                .Index(t => t.TechnicalerId)
                .Index(t => t.ShiftId)
                .Index(t => t.AirportId);
            
            CreateTable(
                "dbo.TruckFuels",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TruckId = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        QCNo = c.String(),
                        AccumulateRefuelAmount = c.Decimal(precision: 18, scale: 2),
                        Time = c.DateTime(nullable: false),
                        TankNo = c.String(),
                        TicketNo = c.String(),
                        OperatorId = c.Int(),
                        MaintenanceStaff = c.String(),
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
                    { "DynamicFilter_TruckFuel_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.OperatorId)
                .ForeignKey("dbo.Trucks", t => t.TruckId, cascadeDelete: true)
                .Index(t => t.TruckId)
                .Index(t => t.OperatorId);
            
            CreateTable(
                "dbo.TruckLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TruckId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        LogTime = c.DateTime(nullable: false),
                        LogType = c.String(),
                        LogText = c.String(),
                        TabletId = c.String(),
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
                    { "DynamicFilter_TruckLog_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Routes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DepartureId = c.Int(nullable: false),
                        ArrivalId = c.Int(nullable: false),
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
                    { "DynamicFilter_Route_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Airports", t => t.ArrivalId)
                .ForeignKey("dbo.Airports", t => t.DepartureId)
                .Index(t => t.DepartureId)
                .Index(t => t.ArrivalId);
            
            CreateTable(
                "dbo.RoleActionInfoes",
                c => new
                    {
                        Role_Id = c.Int(nullable: false),
                        ActionInfo_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Role_Id, t.ActionInfo_Id })
                .ForeignKey("dbo.Roles", t => t.Role_Id, cascadeDelete: true)
                .ForeignKey("dbo.Actions", t => t.ActionInfo_Id, cascadeDelete: true)
                .Index(t => t.Role_Id)
                .Index(t => t.ActionInfo_Id);
            
            CreateTable(
                "dbo.UserAirport",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        AirportId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.UserId, t.AirportId })
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.Airports", t => t.AirportId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.AirportId);
            
            CreateTable(
                "dbo.UserRoles",
                c => new
                    {
                        User_Id = c.Int(nullable: false),
                        Role_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.User_Id, t.Role_Id })
                .ForeignKey("dbo.Users", t => t.User_Id, cascadeDelete: true)
                .ForeignKey("dbo.Roles", t => t.Role_Id, cascadeDelete: true)
                .Index(t => t.User_Id)
                .Index(t => t.Role_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Routes", "DepartureId", "dbo.Airports");
            DropForeignKey("dbo.Routes", "ArrivalId", "dbo.Airports");
            DropForeignKey("dbo.TruckLogs", "UserId", "dbo.Users");
            DropForeignKey("dbo.TruckFuels", "TruckId", "dbo.Trucks");
            DropForeignKey("dbo.TruckFuels", "OperatorId", "dbo.Users");
            DropForeignKey("dbo.TruckAssigns", "TruckId", "dbo.Trucks");
            DropForeignKey("dbo.TruckAssigns", "TechnicalerId", "dbo.Users");
            DropForeignKey("dbo.TruckAssigns", "ShiftId", "dbo.Shifts");
            DropForeignKey("dbo.TruckAssigns", "DriverId", "dbo.Users");
            DropForeignKey("dbo.TruckAssigns", "AirportId", "dbo.Airports");
            DropForeignKey("dbo.RefuelItems", "Refuel_Id", "dbo.Refuels");
            DropForeignKey("dbo.Refuels", "FlightId", "dbo.Flights");
            DropForeignKey("dbo.ProductPrices", "ProductId", "dbo.Products");
            DropForeignKey("dbo.ProductPrices", "CustomerId", "dbo.Airlines");
            DropForeignKey("dbo.FHSImportItems", "FHSImport_Id", "dbo.FHSImports");
            DropForeignKey("dbo.CheckTrucks", "TruckId", "dbo.Trucks");
            DropForeignKey("dbo.CheckTrucks", "ShiftId", "dbo.Shifts");
            DropForeignKey("dbo.Shifts", "AirportId", "dbo.Airports");
            DropForeignKey("dbo.CheckTrucks", "OperatorId", "dbo.Users");
            DropForeignKey("dbo.CheckTrucks", "DriverId", "dbo.Users");
            DropForeignKey("dbo.CheckTrucks", "AirportId", "dbo.Airports");
            DropForeignKey("dbo.BM2505", "TruckId", "dbo.Trucks");
            DropForeignKey("dbo.BM2505", "OperatorId", "dbo.Users");
            DropForeignKey("dbo.BM2505", "FlightId", "dbo.Flights");
            DropForeignKey("dbo.BM2505", "ContainerId", "dbo.BM2505Container");
            DropForeignKey("dbo.Reviews", "FlightId", "dbo.Flights");
            DropForeignKey("dbo.RefuelItems", "UserUpdatedId", "dbo.Users");
            DropForeignKey("dbo.RefuelItems", "TruckId", "dbo.Trucks");
            DropForeignKey("dbo.RefuelItems", "ReceiptId", "dbo.Receipts");
            DropForeignKey("dbo.ReceiptItems", "TruckId", "dbo.Trucks");
            DropForeignKey("dbo.ReceiptItems", "RefuelId", "dbo.RefuelItems");
            DropForeignKey("dbo.ReceiptItems", "ReceiptId", "dbo.Receipts");
            DropForeignKey("dbo.Invoices", "ReplaceId", "dbo.Invoices");
            DropForeignKey("dbo.Invoices", "ReceiptId", "dbo.Receipts");
            DropForeignKey("dbo.InvoiceItems", "TruckId", "dbo.Trucks");
            DropForeignKey("dbo.Trucks", "DeviceId", "dbo.Devices");
            DropForeignKey("dbo.Trucks", "AirportId", "dbo.Airports");
            DropForeignKey("dbo.InvoiceItems", "RefuelItemId", "dbo.RefuelItems");
            DropForeignKey("dbo.InvoiceItems", "OperatorId", "dbo.Users");
            DropForeignKey("dbo.InvoiceItems", "InvoiceId", "dbo.Invoices");
            DropForeignKey("dbo.InvoiceItems", "DriverId", "dbo.Users");
            DropForeignKey("dbo.Invoices", "FlightId", "dbo.Flights");
            DropForeignKey("dbo.Invoices", "CustomerId", "dbo.Airlines");
            DropForeignKey("dbo.Receipts", "FlightId", "dbo.Flights");
            DropForeignKey("dbo.Receipts", "CustomerId", "dbo.Airlines");
            DropForeignKey("dbo.RefuelItems", "OperatorId", "dbo.Users");
            DropForeignKey("dbo.RefuelItems", "InvoiceFormId", "dbo.InvoiceForms");
            DropForeignKey("dbo.InvoiceForms", "AirportId", "dbo.Airports");
            DropForeignKey("dbo.RefuelItems", "FlightId", "dbo.Flights");
            DropForeignKey("dbo.RefuelItems", "DriverId", "dbo.Users");
            DropForeignKey("dbo.ParkingLots", "Location_Id", "dbo.GeoLocations");
            DropForeignKey("dbo.Flights", "ParkingLotId", "dbo.ParkingLots");
            DropForeignKey("dbo.ParkingLots", "AirportId", "dbo.Airports");
            DropForeignKey("dbo.Flights", "AirportId", "dbo.Airports");
            DropForeignKey("dbo.Flights", "AirlineId", "dbo.Airlines");
            DropForeignKey("dbo.Flights", "AircraftId", "dbo.Aircraft");
            DropForeignKey("dbo.Aircraft", "CustomerId", "dbo.Airlines");
            DropForeignKey("dbo.UserRoles", "Role_Id", "dbo.Roles");
            DropForeignKey("dbo.UserRoles", "User_Id", "dbo.Users");
            DropForeignKey("dbo.UserAirport", "AirportId", "dbo.Airports");
            DropForeignKey("dbo.UserAirport", "UserId", "dbo.Users");
            DropForeignKey("dbo.Users", "AirportId", "dbo.Airports");
            DropForeignKey("dbo.RoleActionInfoes", "ActionInfo_Id", "dbo.Actions");
            DropForeignKey("dbo.RoleActionInfoes", "Role_Id", "dbo.Roles");
            DropForeignKey("dbo.Actions", "ControllerId", "dbo.Controllers");
            DropIndex("dbo.UserRoles", new[] { "Role_Id" });
            DropIndex("dbo.UserRoles", new[] { "User_Id" });
            DropIndex("dbo.UserAirport", new[] { "AirportId" });
            DropIndex("dbo.UserAirport", new[] { "UserId" });
            DropIndex("dbo.RoleActionInfoes", new[] { "ActionInfo_Id" });
            DropIndex("dbo.RoleActionInfoes", new[] { "Role_Id" });
            DropIndex("dbo.Routes", new[] { "ArrivalId" });
            DropIndex("dbo.Routes", new[] { "DepartureId" });
            DropIndex("dbo.TruckLogs", new[] { "UserId" });
            DropIndex("dbo.TruckFuels", new[] { "OperatorId" });
            DropIndex("dbo.TruckFuels", new[] { "TruckId" });
            DropIndex("dbo.TruckAssigns", new[] { "AirportId" });
            DropIndex("dbo.TruckAssigns", new[] { "ShiftId" });
            DropIndex("dbo.TruckAssigns", new[] { "TechnicalerId" });
            DropIndex("dbo.TruckAssigns", new[] { "DriverId" });
            DropIndex("dbo.TruckAssigns", new[] { "TruckId" });
            DropIndex("dbo.Refuels", new[] { "FlightId" });
            DropIndex("dbo.ProductPrices", new[] { "ProductId" });
            DropIndex("dbo.ProductPrices", new[] { "CustomerId" });
            DropIndex("dbo.FHSImportItems", new[] { "FHSImport_Id" });
            DropIndex("dbo.Shifts", new[] { "AirportId" });
            DropIndex("dbo.CheckTrucks", new[] { "OperatorId" });
            DropIndex("dbo.CheckTrucks", new[] { "DriverId" });
            DropIndex("dbo.CheckTrucks", new[] { "ShiftId" });
            DropIndex("dbo.CheckTrucks", new[] { "TruckId" });
            DropIndex("dbo.CheckTrucks", new[] { "AirportId" });
            DropIndex("dbo.BM2505", new[] { "OperatorId" });
            DropIndex("dbo.BM2505", new[] { "ContainerId" });
            DropIndex("dbo.BM2505", new[] { "FlightId" });
            DropIndex("dbo.BM2505", new[] { "TruckId" });
            DropIndex("dbo.Reviews", new[] { "FlightId" });
            DropIndex("dbo.ReceiptItems", new[] { "ReceiptId" });
            DropIndex("dbo.ReceiptItems", new[] { "RefuelId" });
            DropIndex("dbo.ReceiptItems", new[] { "TruckId" });
            DropIndex("dbo.Trucks", new[] { "AirportId" });
            DropIndex("dbo.Trucks", new[] { "DeviceId" });
            DropIndex("dbo.InvoiceItems", new[] { "OperatorId" });
            DropIndex("dbo.InvoiceItems", new[] { "DriverId" });
            DropIndex("dbo.InvoiceItems", new[] { "TruckId" });
            DropIndex("dbo.InvoiceItems", new[] { "RefuelItemId" });
            DropIndex("dbo.InvoiceItems", new[] { "InvoiceId" });
            DropIndex("dbo.Invoices", new[] { "ReplaceId" });
            DropIndex("dbo.Invoices", new[] { "ReceiptId" });
            DropIndex("dbo.Invoices", new[] { "CustomerId" });
            DropIndex("dbo.Invoices", new[] { "FlightId" });
            DropIndex("dbo.Receipts", new[] { "FlightId" });
            DropIndex("dbo.Receipts", new[] { "CustomerId" });
            DropIndex("dbo.InvoiceForms", new[] { "AirportId" });
            DropIndex("dbo.RefuelItems", new[] { "Refuel_Id" });
            DropIndex("dbo.RefuelItems", new[] { "UserUpdatedId" });
            DropIndex("dbo.RefuelItems", new[] { "ReceiptId" });
            DropIndex("dbo.RefuelItems", new[] { "InvoiceFormId" });
            DropIndex("dbo.RefuelItems", new[] { "FlightId" });
            DropIndex("dbo.RefuelItems", new[] { "OperatorId" });
            DropIndex("dbo.RefuelItems", new[] { "DriverId" });
            DropIndex("dbo.RefuelItems", new[] { "TruckId" });
            DropIndex("dbo.ParkingLots", new[] { "Location_Id" });
            DropIndex("dbo.ParkingLots", new[] { "AirportId" });
            DropIndex("dbo.Flights", new[] { "AirportId" });
            DropIndex("dbo.Flights", new[] { "ParkingLotId" });
            DropIndex("dbo.Flights", new[] { "AircraftId" });
            DropIndex("dbo.Flights", new[] { "AirlineId" });
            DropIndex("dbo.Aircraft", new[] { "CustomerId" });
            DropIndex("dbo.Users", new[] { "AirportId" });
            DropIndex("dbo.Actions", new[] { "ControllerId" });
            DropTable("dbo.UserRoles");
            DropTable("dbo.UserAirport");
            DropTable("dbo.RoleActionInfoes");
            DropTable("dbo.Routes",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Route_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.TruckLogs",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_TruckLog_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.TruckFuels",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_TruckFuel_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.TruckAssigns",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_TruckAssign_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Tablets",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Tablet_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Refuels",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Refuel_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Products",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Product_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.ProductPrices",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_ProductPrice_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.GreenTaxes",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_GreenTax_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
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
            DropTable("dbo.Shifts",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Shift_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.CheckTrucks",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_CheckTruck_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.ChangeLogs");
            DropTable("dbo.BM2505",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_BM2505_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.BM2505Container",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_BM2505Container_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Reviews",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Review_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.ReceiptItems",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_ReceiptItem_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Devices",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Device_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Trucks",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Truck_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
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
            DropTable("dbo.InvoiceForms",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_InvoiceForm_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                    { "DynamicFilter_InvoiceForm_UserAirport", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.RefuelItems",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_RefuelItem_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.GeoLocations",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_GeoLocation_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.ParkingLots",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_ParkingLot_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Flights",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Flight_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                    { "DynamicFilter_Flight_UserAirport", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Airlines",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Airline_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Aircraft",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Aircraft_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Airports",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_Airport_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Users",
                removedAnnotations: new Dictionary<string, object>
                {
                    { "DynamicFilter_User_IsNotDeleted", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
                });
            DropTable("dbo.Roles");
            DropTable("dbo.Controllers");
            DropTable("dbo.Actions");
        }
    }
}
