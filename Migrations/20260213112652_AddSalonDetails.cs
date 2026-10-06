using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dugunsalonu.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "WeddingEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalonName",
                table: "WeddingEvents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "WeddingEvents");

            migrationBuilder.DropColumn(
                name: "SalonName",
                table: "WeddingEvents");
        }
    }
}
