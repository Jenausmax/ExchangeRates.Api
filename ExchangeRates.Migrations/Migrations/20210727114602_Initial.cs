using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeRates.Migrations.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Valutes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    NumCode = table.Column<string>(type: "TEXT", nullable: true),
                    CharCode = table.Column<string>(type: "TEXT", nullable: true),
                    Nominal = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    Previous = table.Column<double>(type: "REAL", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Valutes", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Valutes");
        }
    }
}
