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
    
    public partial class ManualStudyNumbers : DbMigration
    {
        public override void Up()
        {
            Sql(@"DECLARE @con nvarchar(128)
 SELECT @con = name
 FROM sys.default_constraints
 WHERE parent_object_id = object_id('dbo.CatalogRecords')
 AND col_name(parent_object_id, parent_column_id) = 'Number';
 IF @con IS NOT NULL
     EXECUTE('ALTER TABLE [dbo].[CatalogRecords] DROP CONSTRAINT ' + @con)");

            Sql(@"DECLARE @conf nvarchar(128)
 SELECT @conf = name
 FROM sys.default_constraints
 WHERE parent_object_id = object_id('dbo.ManagedFiles')
 AND col_name(parent_object_id, parent_column_id) = 'Number';
 IF @conf IS NOT NULL
     EXECUTE('ALTER TABLE [dbo].[ManagedFiles] DROP CONSTRAINT ' + @conf)");

            AlterColumn("dbo.CatalogRecords", "Number", c => c.String());
            AlterColumn("dbo.ManagedFiles", "Number", c => c.String());
            DropColumn("dbo.CatalogRecords", "LastFileNumber");
            DropColumn("dbo.Organizations", "LastCatalogRecordNumber");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Organizations", "LastCatalogRecordNumber", c => c.Int(nullable: false));
            AddColumn("dbo.CatalogRecords", "LastFileNumber", c => c.Int(nullable: false));
            AlterColumn("dbo.ManagedFiles", "Number", c => c.Int(nullable: false));
            AlterColumn("dbo.CatalogRecords", "Number", c => c.Int(nullable: false));
        }
    }
}
