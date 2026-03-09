using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Portal2.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentUserLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Student",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Student_UserId",
                table: "Student",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_AspNetUsers_UserId",
                table: "Student",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Student_AspNetUsers_UserId",
                table: "Student");

            migrationBuilder.DropIndex(
                name: "IX_Student_UserId",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Student");
        }
    }
}
