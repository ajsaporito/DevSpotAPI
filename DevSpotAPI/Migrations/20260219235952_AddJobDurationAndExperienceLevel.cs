using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevSpotAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddJobDurationAndExperienceLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "Jobs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExperienceLevel",
                table: "Jobs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExperienceLevel",
                table: "Jobs");
        }
    }
}
