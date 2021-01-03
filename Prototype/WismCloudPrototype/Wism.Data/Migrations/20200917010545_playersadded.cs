using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BranallyGames.Wism.Repository.Migrations
{
    public partial class playersadded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ShortName = table.Column<string>(maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(maxLength: 128, nullable: true),
                    IsHuman = table.Column<bool>(nullable: false),
                    WorldId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Players",
                columns: new[] { "Id", "DisplayName", "IsHuman", "ShortName", "WorldId" },
                values: new object[,]
                {
                    { new Guid("986c478b-6554-4a2c-805f-dc059632b707"), "Branally", false, "Brian", new Guid("517fed59-d8dc-4b59-b6ca-2052f75aabf7") },
                    { new Guid("3def6dbb-fa0c-4010-a73f-aad13b50697b"), "Danally", false, "Dan", new Guid("517fed59-d8dc-4b59-b6ca-2052f75aabf7") },
                    { new Guid("a0466732-e79e-4789-b883-7b43506795e3"), "Branally", false, "Brian", new Guid("b0113c1c-4ee8-421d-82ce-1b65207c9017") },
                    { new Guid("1b9991bf-e454-486a-8f5c-297dd69fe60c"), "Jake the Snake", false, "Jacob", new Guid("b0113c1c-4ee8-421d-82ce-1b65207c9017") },
                    { new Guid("b07173b3-290b-4490-bf9a-d3588a251854"), "Branally", false, "Brian", new Guid("ee082e96-71da-453b-9252-17c8b4f3be65") },
                    { new Guid("3ef6ba4b-3f46-46a5-8705-2a1ea045a381"), "Owen Little", false, "Owen", new Guid("ee082e96-71da-453b-9252-17c8b4f3be65") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_WorldId",
                table: "Players",
                column: "WorldId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
