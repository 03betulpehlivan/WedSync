using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dugunsalonu.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestSessionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestSessionId",
                table: "GuestEntries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestSessionId",
                table: "GuestEntries");
        }
    }
}
