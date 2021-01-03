using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BranallyGames.Wism.Repository.Migrations
{
    public partial class initialcreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Worlds",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ShortName = table.Column<string>(maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worlds", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Worlds",
                columns: new[] { "Id", "DisplayName", "ShortName" },
                values: new object[] { new Guid("517fed59-d8dc-4b59-b6ca-2052f75aabf7"), "Etheria", "Etheria" });

            migrationBuilder.InsertData(
                table: "Worlds",
                columns: new[] { "Id", "DisplayName", "ShortName" },
                values: new object[] { new Guid("b0113c1c-4ee8-421d-82ce-1b65207c9017"), "Britannia", "Britannia" });

            migrationBuilder.InsertData(
                table: "Worlds",
                columns: new[] { "Id", "DisplayName", "ShortName" },
                values: new object[] { new Guid("ee082e96-71da-453b-9252-17c8b4f3be65"), "United States of America", "USA" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Worlds");
        }
    }
}
