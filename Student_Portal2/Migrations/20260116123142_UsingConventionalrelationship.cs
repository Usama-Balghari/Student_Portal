using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Portal2.Migrations
{
    /// <inheritdoc />
    public partial class UsingConventionalrelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Student_Departments_DepartmentId",
                table: "Student");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Departments_DepartmentId",
                table: "Student",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Student_Departments_DepartmentId",
                table: "Student");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Departments_DepartmentId",
                table: "Student",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
