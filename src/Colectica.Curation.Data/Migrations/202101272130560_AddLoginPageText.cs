namespace Colectica.Curation.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLoginPageText : DbMigration
    {
        public override void Up()
        {
            //AddColumn("dbo.CatalogRecords", "AuthorsText", c => c.String());
            //AddColumn("dbo.CatalogRecords", "OwnerText", c => c.String());
            AddColumn("dbo.Organizations", "LoginPageText", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Organizations", "LoginPageText");
            //DropColumn("dbo.CatalogRecords", "OwnerText");
            //DropColumn("dbo.CatalogRecords", "AuthorsText");
        }
    }
}
