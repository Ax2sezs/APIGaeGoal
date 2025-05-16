using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KAEAGoalWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USER_MISSIONS",
                columns: table => new
                {
                    USER_MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    A_USER_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Verification_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Accepted_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Completed_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_MISSIONS", x => x.USER_MISSION_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_MISSIONS");
        }
    }
}
