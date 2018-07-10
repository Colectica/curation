// Copyright 2014 - 2018 Colectica.
// 
// This file is part of the Colectica Curation Tools.
// 
// The Colectica Curation Tools are free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// The Colectica Curation Tools are distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for
// more details.
// 
// You should have received a copy of the GNU Affero General Public License along
// with Colectica Curation Tools. If not, see <https://www.gnu.org/licenses/>.

namespace Colectica.Curation.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CatalogRecords",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Title = c.String(nullable: false),
                        StudyId = c.String(),
                        Number = c.Int(nullable: false),
                        Description = c.String(),
                        Keywords = c.String(),
                        CreatedDate = c.DateTime(),
                        Version = c.Long(nullable: false),
                        LastUpdatedDate = c.DateTime(),
                        PersistentId = c.String(),
                        Funding = c.String(),
                        ArchiveDate = c.DateTime(),
                        PublishDate = c.DateTime(),
                        DepositAgreement = c.String(),
                        AccessStatement = c.String(),
                        ConfidentialityStatement = c.String(),
                        EmbargoStatement = c.String(),
                        RelatedDatabase = c.String(),
                        RelatedPublications = c.String(),
                        RelatedProjects = c.String(),
                        ResearchDesign = c.String(),
                        ModeOfDataCollection = c.String(),
                        FieldDates = c.String(),
                        StudyTimePeriod = c.String(),
                        Location = c.String(),
                        LocationDetails = c.String(),
                        UnitOfObservation = c.String(),
                        SampleSize = c.String(),
                        InclusionExclusionCriteria = c.String(),
                        RandomizationProcedure = c.String(),
                        UnitOfRandomization = c.String(),
                        Treatment = c.String(),
                        TreatmentAdministration = c.String(),
                        OutcomeMeasures = c.String(),
                        DataType = c.String(),
                        DataSource = c.String(),
                        DataSourceInformation = c.String(),
                        ReviewType = c.String(),
                        CertifiedDate = c.DateTime(),
                        OperationLockId = c.Guid(),
                        OperationStatus = c.String(),
                        LastFileNumber = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        CreatedBy_Id = c.String(maxLength: 128),
                        Organization_Id = c.Guid(),
                        Owner_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.CreatedBy_Id)
                .ForeignKey("dbo.Organizations", t => t.Organization_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.Owner_Id)
                .Index(t => t.CreatedBy_Id)
                .Index(t => t.Organization_Id)
                .Index(t => t.Owner_Id);
            
            CreateTable(
                "dbo.AspNetUsers",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        FirstName = c.String(),
                        LastName = c.String(),
                        ContactInformation = c.String(),
                        Orcid = c.String(),
                        IsOrcidConfirmed = c.Boolean(nullable: false),
                        IsPlaceholder = c.Boolean(nullable: false),
                        IsAdministrator = c.Boolean(nullable: false),
                        NotifyOnCatalogRecordCreated = c.Boolean(nullable: false),
                        NotifyOnCatalogRecordSubmittedForCuration = c.Boolean(nullable: false),
                        NotifyOnAssignedToCatalogRecord = c.Boolean(nullable: false),
                        NotifyOnCatalogRecordPublishRequested = c.Boolean(nullable: false),
                        NotifyOnCatalogRecordPublishedOrRejected = c.Boolean(nullable: false),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Organizations",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Name = c.String(nullable: false),
                        ImageUrl = c.String(),
                        Hostname = c.String(),
                        AgencyID = c.String(),
                        IngestDirectory = c.String(),
                        ProcessingDirectory = c.String(),
                        ArchiveDirectory = c.String(),
                        ContactInformation = c.String(),
                        DepositAgreement = c.String(),
                        TermsOfService = c.String(),
                        OrganizationPolicy = c.String(),
                        ReplyToAddress = c.String(),
                        NotificationEmailClosing = c.String(),
                        IsAnonymousRegistrationAllowed = c.Boolean(nullable: false),
                        HandleServerEndpoint = c.String(),
                        HandleGroupName = c.String(),
                        HandleUserName = c.String(),
                        HandlePassword = c.String(),
                        LastCatalogRecordNumber = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.ManagedFiles",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Number = c.Int(nullable: false),
                        Title = c.String(),
                        Name = c.String(nullable: false),
                        PublicName = c.String(),
                        PersistentLink = c.String(),
                        PersistentLinkDate = c.DateTime(),
                        Version = c.Long(nullable: false),
                        Type = c.String(),
                        FormatName = c.String(),
                        FormatId = c.String(),
                        Size = c.Long(nullable: false),
                        CreationDate = c.DateTime(),
                        KindOfData = c.String(),
                        Source = c.String(),
                        SourceInformation = c.String(),
                        Rights = c.String(),
                        IsPublicAccess = c.Boolean(nullable: false),
                        UploadedDate = c.DateTime(),
                        ExternalDatabase = c.String(),
                        Software = c.String(),
                        SoftwareVersion = c.String(),
                        Hardware = c.String(),
                        Checksum = c.String(),
                        ChecksumMethod = c.String(),
                        ChecksumDate = c.DateTime(),
                        VirusCheckOutcome = c.String(),
                        VirusCheckMethod = c.String(),
                        VirusCheckDate = c.DateTime(),
                        AcceptedDate = c.DateTime(),
                        CertifiedDate = c.DateTime(),
                        Status = c.Int(nullable: false),
                        CatalogRecord_Id = c.Guid(),
                        Owner_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CatalogRecords", t => t.CatalogRecord_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.Owner_Id)
                .Index(t => t.CatalogRecord_Id)
                .Index(t => t.Owner_Id);
            
            CreateTable(
                "dbo.Events",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Timestamp = c.DateTime(nullable: false),
                        EventType = c.Guid(nullable: false),
                        Title = c.String(),
                        Details = c.String(),
                        RelatedCatalogRecord_Id = c.Guid(),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CatalogRecords", t => t.RelatedCatalogRecord_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.RelatedCatalogRecord_Id)
                .Index(t => t.User_Id);
            
            CreateTable(
                "dbo.TaskStatus",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        TaskId = c.Guid(nullable: false),
                        Name = c.String(),
                        StageName = c.String(),
                        Weight = c.Int(nullable: false),
                        IsComplete = c.Boolean(nullable: false),
                        CompletedDate = c.DateTime(),
                        CatalogRecord_Id = c.Guid(),
                        CompletedBy_Id = c.String(maxLength: 128),
                        File_Id = c.Guid(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CatalogRecords", t => t.CatalogRecord_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.CompletedBy_Id)
                .ForeignKey("dbo.ManagedFiles", t => t.File_Id)
                .Index(t => t.CatalogRecord_Id)
                .Index(t => t.CompletedBy_Id)
                .Index(t => t.File_Id);
            
            CreateTable(
                "dbo.Operations",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        LockId = c.Guid(nullable: false),
                        Status = c.Int(nullable: false),
                        ApplicationName = c.String(),
                        QueueName = c.String(),
                        OperationName = c.String(),
                        OperationType = c.Guid(nullable: false),
                        Data = c.String(),
                        QueuedOn = c.DateTime(nullable: false),
                        StartedOn = c.DateTime(),
                        CompletedOn = c.DateTime(),
                        CatalogRecordContext = c.Guid(nullable: false),
                        RequestingUserId = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Permissions",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Right = c.Int(nullable: false),
                        Organization_Id = c.Guid(),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Organizations", t => t.Organization_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.Organization_Id)
                .Index(t => t.User_Id);
            
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
            CreateTable(
                "dbo.Settings",
                c => new
                    {
                        Name = c.String(nullable: false, maxLength: 128),
                        Value = c.String(),
                    })
                .PrimaryKey(t => t.Name);
            
            CreateTable(
                "dbo.OrganizationApplicationUsers",
                c => new
                    {
                        Organization_Id = c.Guid(nullable: false),
                        ApplicationUser_Id = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.Organization_Id, t.ApplicationUser_Id })
                .ForeignKey("dbo.Organizations", t => t.Organization_Id, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUser_Id, cascadeDelete: true)
                .Index(t => t.Organization_Id)
                .Index(t => t.ApplicationUser_Id);
            
            CreateTable(
                "dbo.Approvers",
                c => new
                    {
                        CatalogRecordId = c.Guid(nullable: false),
                        ApplicationUserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.CatalogRecordId, t.ApplicationUserId })
                .ForeignKey("dbo.CatalogRecords", t => t.CatalogRecordId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUserId, cascadeDelete: true)
                .Index(t => t.CatalogRecordId)
                .Index(t => t.ApplicationUserId);
            
            CreateTable(
                "dbo.Authors",
                c => new
                    {
                        CatalogRecordId = c.Guid(nullable: false),
                        ApplicationUserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.CatalogRecordId, t.ApplicationUserId })
                .ForeignKey("dbo.CatalogRecords", t => t.CatalogRecordId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUserId, cascadeDelete: true)
                .Index(t => t.CatalogRecordId)
                .Index(t => t.ApplicationUserId);
            
            CreateTable(
                "dbo.Curators",
                c => new
                    {
                        CatalogRecordId = c.Guid(nullable: false),
                        ApplicationUserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.CatalogRecordId, t.ApplicationUserId })
                .ForeignKey("dbo.CatalogRecords", t => t.CatalogRecordId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUserId, cascadeDelete: true)
                .Index(t => t.CatalogRecordId)
                .Index(t => t.ApplicationUserId);
            
            CreateTable(
                "dbo.EventManagedFiles",
                c => new
                    {
                        Event_Id = c.Long(nullable: false),
                        ManagedFile_Id = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.Event_Id, t.ManagedFile_Id })
                .ForeignKey("dbo.Events", t => t.Event_Id, cascadeDelete: true)
                .ForeignKey("dbo.ManagedFiles", t => t.ManagedFile_Id, cascadeDelete: true)
                .Index(t => t.Event_Id)
                .Index(t => t.ManagedFile_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.Permissions", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Permissions", "Organization_Id", "dbo.Organizations");
            DropForeignKey("dbo.TaskStatus", "File_Id", "dbo.ManagedFiles");
            DropForeignKey("dbo.TaskStatus", "CompletedBy_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.TaskStatus", "CatalogRecord_Id", "dbo.CatalogRecords");
            DropForeignKey("dbo.CatalogRecords", "Owner_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.CatalogRecords", "Organization_Id", "dbo.Organizations");
            DropForeignKey("dbo.ManagedFiles", "Owner_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Events", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.EventManagedFiles", "ManagedFile_Id", "dbo.ManagedFiles");
            DropForeignKey("dbo.EventManagedFiles", "Event_Id", "dbo.Events");
            DropForeignKey("dbo.Events", "RelatedCatalogRecord_Id", "dbo.CatalogRecords");
            DropForeignKey("dbo.ManagedFiles", "CatalogRecord_Id", "dbo.CatalogRecords");
            DropForeignKey("dbo.Curators", "ApplicationUserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Curators", "CatalogRecordId", "dbo.CatalogRecords");
            DropForeignKey("dbo.CatalogRecords", "CreatedBy_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Authors", "ApplicationUserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Authors", "CatalogRecordId", "dbo.CatalogRecords");
            DropForeignKey("dbo.Approvers", "ApplicationUserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Approvers", "CatalogRecordId", "dbo.CatalogRecords");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.OrganizationApplicationUsers", "ApplicationUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.OrganizationApplicationUsers", "Organization_Id", "dbo.Organizations");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.EventManagedFiles", new[] { "ManagedFile_Id" });
            DropIndex("dbo.EventManagedFiles", new[] { "Event_Id" });
            DropIndex("dbo.Curators", new[] { "ApplicationUserId" });
            DropIndex("dbo.Curators", new[] { "CatalogRecordId" });
            DropIndex("dbo.Authors", new[] { "ApplicationUserId" });
            DropIndex("dbo.Authors", new[] { "CatalogRecordId" });
            DropIndex("dbo.Approvers", new[] { "ApplicationUserId" });
            DropIndex("dbo.Approvers", new[] { "CatalogRecordId" });
            DropIndex("dbo.OrganizationApplicationUsers", new[] { "ApplicationUser_Id" });
            DropIndex("dbo.OrganizationApplicationUsers", new[] { "Organization_Id" });
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropIndex("dbo.Permissions", new[] { "User_Id" });
            DropIndex("dbo.Permissions", new[] { "Organization_Id" });
            DropIndex("dbo.TaskStatus", new[] { "File_Id" });
            DropIndex("dbo.TaskStatus", new[] { "CompletedBy_Id" });
            DropIndex("dbo.TaskStatus", new[] { "CatalogRecord_Id" });
            DropIndex("dbo.Events", new[] { "User_Id" });
            DropIndex("dbo.Events", new[] { "RelatedCatalogRecord_Id" });
            DropIndex("dbo.ManagedFiles", new[] { "Owner_Id" });
            DropIndex("dbo.ManagedFiles", new[] { "CatalogRecord_Id" });
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.CatalogRecords", new[] { "Owner_Id" });
            DropIndex("dbo.CatalogRecords", new[] { "Organization_Id" });
            DropIndex("dbo.CatalogRecords", new[] { "CreatedBy_Id" });
            DropTable("dbo.EventManagedFiles");
            DropTable("dbo.Curators");
            DropTable("dbo.Authors");
            DropTable("dbo.Approvers");
            DropTable("dbo.OrganizationApplicationUsers");
            DropTable("dbo.Settings");
            DropTable("dbo.AspNetRoles");
            DropTable("dbo.Permissions");
            DropTable("dbo.Operations");
            DropTable("dbo.TaskStatus");
            DropTable("dbo.Events");
            DropTable("dbo.ManagedFiles");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.Organizations");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.CatalogRecords");
        }
    }
}
