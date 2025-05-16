using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class AddRewardAndUserReward : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "REWARDS",
                columns: table => new
                {
                    REWARD_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    REWARD_NAME = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PRICE = table.Column<int>(type: "int", nullable: false),
                    QUANTITY = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REWARDS", x => x.REWARD_ID);
                });

            migrationBuilder.CreateTable(
                name: "REWARD_IMAGES",
                columns: table => new
                {
                    REWARD_IMAGE_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    REWARD_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrls = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uploaded_At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REWARD_IMAGES", x => x.REWARD_IMAGE_ID);
                    table.ForeignKey(
                        name: "FK_REWARD_IMAGES_REWARDS_REWARD_ID",
                        column: x => x.REWARD_ID,
                        principalTable: "REWARDS",
                        principalColumn: "REWARD_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USER_REWARDS",
                columns: table => new
                {
                    USER_REWARD_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    A_USER_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    REWARD_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    STATUS = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    REDEEMED_AT = table.Column<DateTime>(type: "datetime2", nullable: false),
                    COLLECT_AT = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_REWARDS", x => x.USER_REWARD_ID);
                    table.ForeignKey(
                        name: "FK_USER_REWARDS_REWARDS_REWARD_ID",
                        column: x => x.REWARD_ID,
                        principalTable: "REWARDS",
                        principalColumn: "REWARD_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_REWARDS_USERS_A_USER_ID",
                        column: x => x.A_USER_ID,
                        principalTable: "USERS",
                        principalColumn: "A_USER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_REWARD_IMAGES_REWARD_ID",
                table: "REWARD_IMAGES",
                column: "REWARD_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_REWARDS_A_USER_ID",
                table: "USER_REWARDS",
                column: "A_USER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_REWARDS_REWARD_ID",
                table: "USER_REWARDS",
                column: "REWARD_ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "REWARD_IMAGES");

            migrationBuilder.DropTable(
                name: "USER_REWARDS");

            migrationBuilder.DropTable(
                name: "REWARDS");
        }
    }
}
