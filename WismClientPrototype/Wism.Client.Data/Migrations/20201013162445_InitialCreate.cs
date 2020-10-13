using Microsoft.EntityFrameworkCore.Migrations;

namespace Wism.Client.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Armies",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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
                        .Annotation("Sqlite:Autoincrement", true),
                    Discriminator = table.Column<string>(nullable: false),
                    X = table.Column<int>(nullable: true),
                    Y = table.Column<int>(nullable: true),
                    ArmyMoveCommand_X = table.Column<int>(nullable: true),
                    ArmyMoveCommand_Y = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commands", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Armies",
                columns: new[] { "Id", "HitPoints", "Name", "Strength", "X", "Y" },
                values: new object[] { 1, 2, "Hero", 5, 0, 0 });

            migrationBuilder.InsertData(
                table: "Armies",
                columns: new[] { "Id", "HitPoints", "Name", "Strength", "X", "Y" },
                values: new object[] { 2, 2, "Light Infantry", 3, 0, 5 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Armies");

            migrationBuilder.DropTable(
                name: "Commands");
        }
    }
}
