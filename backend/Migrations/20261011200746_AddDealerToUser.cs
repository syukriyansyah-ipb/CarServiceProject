using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MitsubishiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDealerToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DealerId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DealerId",
                table: "Users",
                column: "DealerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Dealers_DealerId",
                table: "Users",
                column: "DealerId",
                principalTable: "Dealers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Dealers_DealerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DealerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DealerId",
                table: "Users");
        }
    }
}
