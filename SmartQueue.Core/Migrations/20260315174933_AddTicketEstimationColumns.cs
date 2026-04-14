using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartQueue.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketEstimationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualWaitMinutes",
                table: "Tickets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedWaitMinutes",
                table: "Tickets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualWaitMinutes",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EstimatedWaitMinutes",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Tickets");
        }
    }
}
