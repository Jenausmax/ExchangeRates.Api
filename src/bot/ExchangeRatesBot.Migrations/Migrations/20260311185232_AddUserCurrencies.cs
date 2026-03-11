using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeRatesBot.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currencies",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currencies",
                table: "Users");
        }
    }
}
