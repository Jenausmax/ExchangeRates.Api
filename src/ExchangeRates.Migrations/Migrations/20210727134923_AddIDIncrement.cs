using Microsoft.EntityFrameworkCore.Migrations;

namespace ExchangeRates.Migrations.Migrations
{
    public partial class AddIDIncrement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Valutes_Time",
                table: "Valutes",
                column: "Time");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Valutes_Time",
                table: "Valutes");
        }
    }
}
