using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeRatesBot.Migrations.Migrations
{
    public partial class AddPersonalizedNewsSchedule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewsTimes",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastNewsDeliveredAt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            // Миграция данных: все подписчики новостей получают дефолтное расписание "09:00"
            migrationBuilder.Sql("UPDATE Users SET NewsTimes = '09:00' WHERE NewsSubscribe = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewsTimes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastNewsDeliveredAt",
                table: "Users");
        }
    }
}
