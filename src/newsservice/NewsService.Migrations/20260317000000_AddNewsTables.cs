using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NewsService.Migrations
{
    public partial class AddNewsTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TopicId = table.Column<int>(type: "INTEGER", nullable: false),
                    RawTitle = table.Column<string>(type: "TEXT", nullable: true),
                    RawDescription = table.Column<string>(type: "TEXT", nullable: true),
                    RawUrl = table.Column<string>(type: "TEXT", nullable: true),
                    RawPublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceFeed = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_TopicId",
                table: "Items",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_ContentHash",
                table: "Topics",
                column: "ContentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_IsSent",
                table: "Topics",
                column: "IsSent");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_PublishedAt",
                table: "Topics",
                column: "PublishedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Items");
            migrationBuilder.DropTable(name: "Topics");
        }
    }
}
