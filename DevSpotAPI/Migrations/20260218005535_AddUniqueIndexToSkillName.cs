using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevSpotAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToSkillName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Skills_Name",
                table: "Skills",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Skills_Name",
                table: "Skills");
        }
    }
}
