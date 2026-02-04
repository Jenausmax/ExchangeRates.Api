using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeRates.Migrations.Migrations
{
    public partial class UpdateModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Valutes_Time",
                table: "Valutes");

            migrationBuilder.RenameColumn(
                name: "Time",
                table: "Valutes",
                newName: "TimeStampUpdateValute");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateSave",
                table: "Valutes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateValute",
                table: "Valutes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ValuteId",
                table: "Valutes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Valutes_CharCode",
                table: "Valutes",
                column: "CharCode");

            migrationBuilder.CreateIndex(
                name: "IX_Valutes_DateSave",
                table: "Valutes",
                column: "DateSave");

            migrationBuilder.CreateIndex(
                name: "IX_Valutes_ValuteId",
                table: "Valutes",
                column: "ValuteId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Valutes_CharCode",
                table: "Valutes");

            migrationBuilder.DropIndex(
                name: "IX_Valutes_DateSave",
                table: "Valutes");

            migrationBuilder.DropIndex(
                name: "IX_Valutes_ValuteId",
                table: "Valutes");

            migrationBuilder.DropColumn(
                name: "DateSave",
                table: "Valutes");

            migrationBuilder.DropColumn(
                name: "DateValute",
                table: "Valutes");

            migrationBuilder.DropColumn(
                name: "ValuteId",
                table: "Valutes");

            migrationBuilder.RenameColumn(
                name: "TimeStampUpdateValute",
                table: "Valutes",
                newName: "Time");

            migrationBuilder.CreateIndex(
                name: "IX_Valutes_Time",
                table: "Valutes",
                column: "Time");
        }
    }
}
