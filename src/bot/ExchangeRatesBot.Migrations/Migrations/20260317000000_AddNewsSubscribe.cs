using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeRatesBot.Migrations.Migrations
{
    public partial class AddNewsSubscribe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NewsSubscribe",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewsSubscribe",
                table: "Users");
        }
    }
}
