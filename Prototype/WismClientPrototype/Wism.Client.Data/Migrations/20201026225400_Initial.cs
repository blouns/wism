using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Wism.Client.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Armies",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    X = table.Column<int>(nullable: false),
                    Y = table.Column<int>(nullable: false),
                    HitPoints = table.Column<int>(nullable: false),
                    Strength = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Armies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Commands",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    X = table.Column<int>(nullable: false),
                    Y = table.Column<int>(nullable: false),
                    Discriminator = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArmyCommand",
                columns: table => new
                {
                    ArmyId = table.Column<Guid>(nullable: false),
                    CommandId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArmyCommand", x => new { x.ArmyId, x.CommandId });
                    table.ForeignKey(
                        name: "FK_ArmyCommand_Armies_ArmyId",
                        column: x => x.ArmyId,
                        principalTable: "Armies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArmyCommand_Commands_CommandId",
                        column: x => x.CommandId,
                        principalTable: "Commands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Armies",
                columns: new[] { "Id", "HitPoints", "Name", "Strength", "X", "Y" },
                values: new object[] { new Guid("5771f514-edce-463b-9f54-d0bb30adeb57"), 2, "Hero", 5, 0, 0 });

            migrationBuilder.InsertData(
                table: "Armies",
                columns: new[] { "Id", "HitPoints", "Name", "Strength", "X", "Y" },
                values: new object[] { new Guid("ddc9272a-f93d-4bd2-9351-7877b40feada"), 2, "Light Infantry", 3, 0, 5 });

            migrationBuilder.CreateIndex(
                name: "IX_ArmyCommand_CommandId",
                table: "ArmyCommand",
                column: "CommandId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArmyCommand");

            migrationBuilder.DropTable(
                name: "Armies");

            migrationBuilder.DropTable(
                name: "Commands");
        }
    }
}
