using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dugunsalonu.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanTypeToWeddingEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanType",
                table: "WeddingEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanType",
                table: "WeddingEvents");
        }
    }
}
