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
    
    public partial class OperationProperties : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Operations", "RequestingUserId", c => c.String(maxLength: 128));
            CreateIndex("dbo.Operations", "CatalogRecordContext");
            CreateIndex("dbo.Operations", "RequestingUserId");
            AddForeignKey("dbo.Operations", "CatalogRecordContext", "dbo.CatalogRecords", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Operations", "RequestingUserId", "dbo.AspNetUsers", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Operations", "RequestingUserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Operations", "CatalogRecordContext", "dbo.CatalogRecords");
            DropIndex("dbo.Operations", new[] { "RequestingUserId" });
            DropIndex("dbo.Operations", new[] { "CatalogRecordContext" });
            AlterColumn("dbo.Operations", "RequestingUserId", c => c.String());
        }
    }
}
