using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KAEAGoalWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMissionIs_Collectfield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Is_Collect",
                table: "USER_MISSIONS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_USER_MISSIONS_MISSION_ID",
                table: "USER_MISSIONS",
                column: "MISSION_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_USER_MISSIONS_MISSIONS_MISSION_ID",
                table: "USER_MISSIONS",
                column: "MISSION_ID",
                principalTable: "MISSIONS",
                principalColumn: "MISSION_ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USER_MISSIONS_MISSIONS_MISSION_ID",
                table: "USER_MISSIONS");

            migrationBuilder.DropIndex(
                name: "IX_USER_MISSIONS_MISSION_ID",
                table: "USER_MISSIONS");

            migrationBuilder.DropColumn(
                name: "Is_Collect",
                table: "USER_MISSIONS");
        }
    }
}
