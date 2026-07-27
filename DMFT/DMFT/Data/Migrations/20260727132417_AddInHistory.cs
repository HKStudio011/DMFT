using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMFT.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InHistory",
                table: "DownloadItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InHistory",
                table: "DownloadItems");
        }
    }
}
