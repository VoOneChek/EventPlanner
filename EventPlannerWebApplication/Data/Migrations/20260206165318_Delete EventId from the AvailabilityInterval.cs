using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPlannerWebApplication.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeleteEventIdfromtheAvailabilityInterval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilityIntervals_Events_EventId",
                table: "AvailabilityIntervals");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilityIntervals_EventId",
                table: "AvailabilityIntervals");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "AvailabilityIntervals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "AvailabilityIntervals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityIntervals_EventId",
                table: "AvailabilityIntervals",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilityIntervals_Events_EventId",
                table: "AvailabilityIntervals",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
