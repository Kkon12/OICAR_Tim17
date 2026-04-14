using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartQueue.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultServiceMinutes",
                table: "Queues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinTicketsForStats",
                table: "Queues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "QueueStatSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QueueId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    HourOfDay = table.Column<int>(type: "integer", nullable: false),
                    AvgServiceMinutes = table.Column<double>(type: "double precision", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueStatSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueStatSnapshots_Queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "Queues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueueStatSnapshots_QueueId_DayOfWeek_HourOfDay",
                table: "QueueStatSnapshots",
                columns: new[] { "QueueId", "DayOfWeek", "HourOfDay" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueueStatSnapshots");

            migrationBuilder.DropColumn(
                name: "DefaultServiceMinutes",
                table: "Queues");

            migrationBuilder.DropColumn(
                name: "MinTicketsForStats",
                table: "Queues");
        }
    }
}
