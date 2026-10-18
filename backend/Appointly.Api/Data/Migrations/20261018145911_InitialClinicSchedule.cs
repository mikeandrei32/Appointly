using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Appointly.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialClinicSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    OpeningTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    ClosingTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    IsOpen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicSchedules", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ClinicSchedules",
                columns: new[] { "Id", "ClosingTime", "DayOfWeek", "IsOpen", "OpeningTime" },
                values: new object[,]
                {
                    { 1, new TimeOnly(17, 0, 0), 0, false, new TimeOnly(9, 0, 0) },
                    { 2, new TimeOnly(17, 0, 0), 1, true, new TimeOnly(9, 0, 0) },
                    { 3, new TimeOnly(17, 0, 0), 2, true, new TimeOnly(9, 0, 0) },
                    { 4, new TimeOnly(17, 0, 0), 3, true, new TimeOnly(9, 0, 0) },
                    { 5, new TimeOnly(17, 0, 0), 4, true, new TimeOnly(9, 0, 0) },
                    { 6, new TimeOnly(17, 0, 0), 5, true, new TimeOnly(9, 0, 0) },
                    { 7, new TimeOnly(17, 0, 0), 6, false, new TimeOnly(9, 0, 0) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSchedules_DayOfWeek",
                table: "ClinicSchedules",
                column: "DayOfWeek",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicSchedules");
        }
    }
}
