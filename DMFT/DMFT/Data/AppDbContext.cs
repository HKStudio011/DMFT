using DMFT.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMFT.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<DownloadItem> DownloadItems => Set<DownloadItem>();
    public DbSet<DownloadSetting> DownloadSettings => Set<DownloadSetting>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
}
