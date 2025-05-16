using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KAEAGoalWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MISSIONS",
                columns: table => new
                {
                    MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MISSION_NAME = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MISSION_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Coin_Reward = table.Column<int>(type: "int", nullable: false),
                    Mission_Point = table.Column<int>(type: "int", nullable: false),
                    Start_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Expire_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MISSIONS", x => x.MISSION_ID);
                });

            migrationBuilder.CreateTable(
                name: "CODE_MISSIONS",
                columns: table => new
                {
                    MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code_Mission_Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CODE_MISSIONS", x => x.MISSION_ID);
                    table.ForeignKey(
                        name: "FK_CODE_MISSIONS_MISSIONS_MISSION_ID",
                        column: x => x.MISSION_ID,
                        principalTable: "MISSIONS",
                        principalColumn: "MISSION_ID",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CODE_MISSIONS");

            migrationBuilder.DropTable(
                name: "MISSIONS");
        }
    }
}
