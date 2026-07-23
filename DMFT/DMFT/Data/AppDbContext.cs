using DMFT.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMFT.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DownloadItem> DownloadItems => Set<DownloadItem>();
    public DbSet<DownloadSetting> DownloadSettings => Set<DownloadSetting>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DownloadItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).HasMaxLength(2048);
            e.Property(x => x.Platform).HasMaxLength(50);
            e.Property(x => x.VideoId).HasMaxLength(100);
            e.Property(x => x.OriginalUrl).HasMaxLength(2048);
            e.Property(x => x.ThumbnailUrl).HasMaxLength(2048);
            e.Property(x => x.TitleDescription).HasMaxLength(500);
            e.Property(x => x.OriginalSoundUrl).HasMaxLength(2048);
            e.Property(x => x.OriginalSoundName).HasMaxLength(300);
            e.Property(x => x.SaveLocation).HasMaxLength(1000);
            e.Property(x => x.CurrentFileName).HasMaxLength(500);
        });

        modelBuilder.Entity<DownloadSetting>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DefaultPath).HasMaxLength(1000);
        });

        modelBuilder.Entity<AppSetting>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(4000);
        });
    }
}
