using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMFT.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DownloadItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VideoId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OriginalUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    TitleDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OriginalSoundUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    OriginalSoundName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SaveLocation = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    DownloadMode = table.Column<int>(type: "INTEGER", nullable: false),
                    DownloadedBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Speed = table.Column<double>(type: "REAL", nullable: false),
                    EtaSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentFileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DownloadSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "DownloadItems");

            migrationBuilder.DropTable(
                name: "DownloadSettings");
        }
    }
}
