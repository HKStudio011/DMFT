using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMFT.Core.Data.Migrations
{
    public partial class ConvertDownloadMode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old: 0=Video, 1=AudioOnly, 2=AudioOriginOnly, 3=VideoAndAudioOrigin
            // New: 1=Video, 2=Audio, 4=OriginAudio, 5=Video|OriginAudio
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 1 WHERE DownloadMode = 0");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 2 WHERE DownloadMode = 1");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 4 WHERE DownloadMode = 2");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 5 WHERE DownloadMode = 3");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: map new values back to old
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 0 WHERE DownloadMode = 1");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 1 WHERE DownloadMode = 2");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 2 WHERE DownloadMode = 4");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 3 WHERE DownloadMode = 5");
        }
    }
}
