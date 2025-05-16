using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class UpdateCoinTransaction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Giver_User_ID",
                table: "COIN_TRANSACTIONS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Receiver_User_ID",
                table: "COIN_TRANSACTIONS",
                type: "uniqueidentifier",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Giver_User_ID",
                table: "COIN_TRANSACTIONS");

            migrationBuilder.DropColumn(
                name: "Receiver_User_ID",
                table: "COIN_TRANSACTIONS");
        }
    }
}
