namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixUserIDPK : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.BookingRequests", name: "User_Id", newName: "UserID");
            RenameIndex(table: "dbo.BookingRequests", name: "IX_User_Id", newName: "IX_UserID");
            DropColumn("dbo.BookingRequests", "BookerId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.BookingRequests", "BookerId", c => c.String());
            RenameIndex(table: "dbo.BookingRequests", name: "IX_UserID", newName: "IX_User_Id");
            RenameColumn(table: "dbo.BookingRequests", name: "UserID", newName: "User_Id");
        }
    }
}
