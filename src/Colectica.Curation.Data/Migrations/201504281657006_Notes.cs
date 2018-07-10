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
    
    public partial class Notes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Notes",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Timestamp = c.DateTime(nullable: false),
                        Text = c.String(),
                        CatalogRecord_Id = c.Guid(),
                        File_Id = c.Guid(),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CatalogRecords", t => t.CatalogRecord_Id)
                .ForeignKey("dbo.ManagedFiles", t => t.File_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.CatalogRecord_Id)
                .Index(t => t.File_Id)
                .Index(t => t.User_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Notes", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Notes", "File_Id", "dbo.ManagedFiles");
            DropForeignKey("dbo.Notes", "CatalogRecord_Id", "dbo.CatalogRecords");
            DropIndex("dbo.Notes", new[] { "User_Id" });
            DropIndex("dbo.Notes", new[] { "File_Id" });
            DropIndex("dbo.Notes", new[] { "CatalogRecord_Id" });
            DropTable("dbo.Notes");
        }
    }
}
