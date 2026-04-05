using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Portal2.Migrations
{
    /// <inheritdoc />
    public partial class addUserLogTableBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateTable(
            //    name: "UserLogs",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        PreviousRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        CurrentRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        PerformedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_UserLogs", x => x.Id);
            //    });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLogs");
        }
    }
}
