using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Portal2.Migrations
{
    /// <inheritdoc />
    public partial class removeCourseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Student");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "Student",
                type: "int",
                nullable: true);
        }
    }
}
