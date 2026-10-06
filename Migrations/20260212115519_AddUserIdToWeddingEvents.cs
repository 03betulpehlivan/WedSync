using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dugunsalonu.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToWeddingEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "WeddingEvents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WeddingEvents");
        }
    }
}
