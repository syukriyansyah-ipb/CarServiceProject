using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MitsubishiApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameLicencePlateToLicensePlate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LicencePlate",
                table: "Vehicles",
                newName: "LicensePlate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LicensePlate",
                table: "Vehicles",
                newName: "LicencePlate");
        }
    }
}
