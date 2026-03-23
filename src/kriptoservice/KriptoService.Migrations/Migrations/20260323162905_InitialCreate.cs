using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KriptoService.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Change24h = table.Column<decimal>(type: "TEXT", nullable: false),
                    ChangePct24h = table.Column<decimal>(type: "TEXT", nullable: false),
                    High24h = table.Column<decimal>(type: "TEXT", nullable: false),
                    Low24h = table.Column<decimal>(type: "TEXT", nullable: false),
                    Volume24h = table.Column<decimal>(type: "TEXT", nullable: false),
                    MarketCap = table.Column<decimal>(type: "TEXT", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prices_FetchedAt",
                table: "Prices",
                column: "FetchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Prices_Symbol_Currency_FetchedAt",
                table: "Prices",
                columns: new[] { "Symbol", "Currency", "FetchedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Prices");
        }
    }
}
