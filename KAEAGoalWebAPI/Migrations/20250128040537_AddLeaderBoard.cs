using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class AddLeaderBoard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LEADERBOARDS",
                columns: table => new
                {
                    LEADERBOARD_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    A_USER_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Point = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: true),
                    MonthYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Create_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LEADERBOARDS", x => x.LEADERBOARD_ID);
                    table.ForeignKey(
                        name: "FK_LEADERBOARDS_USERS_A_USER_ID",
                        column: x => x.A_USER_ID,
                        principalTable: "USERS",
                        principalColumn: "A_USER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LEADERBOARDS_A_USER_ID",
                table: "LEADERBOARDS",
                column: "A_USER_ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LEADERBOARDS");
        }
    }
}
