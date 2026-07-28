# SQLite EF Core Migration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace JSON file-based storage with SQLite database using Entity Framework Core for DMFT's Main and History pages.

**Architecture:** Use EF Core with SQLite to replace `BaseContainer.LoadContainerAsync/SaveContainerAsync` which currently reads/writes JSON files. Keep the same `LinkInfo` model but add an `Id` primary key. Existing page code (`Main.razor`, `History.razor`) will continue to work as the container classes maintain the same interface.

**Tech Stack:**
- .NET 10.0 + MAUI
- Microsoft.EntityFrameworkCore.Sqlite (latest compatible)
- Existing: CommunityToolkit.Maui, Bootstrap 5

---

## File Structure

| File | Responsibility |
|------|----------------|
| `DMFT/Data/DmftDbContext.cs` | EF Core DbContext for SQLite |
| `DMFT/Model/LinkInfo.cs` | Add `Id` primary key |
| `DMFT/Model/BaseContainer.cs` | Migrate to EF Core operations |
| `DMFT/Model/MainContainer.cs` | Inherit from BaseContainer |
| `DMFT/Model/HistoryContainer.cs` | Inherit from BaseContainer |
| `DMFT/MauiProgram.cs` | Register DbContext and Scoped lifetime |
| `DMFT/DMFT.csproj` | Add EF Core packages |

---

## Migration Steps

### Task 1: Add EF Core SQLite Packages

**Files:**
- Modify: `DMFT/DMFT.csproj`

- [ ] **Step 1: Add SQLite and EF Core packages**

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
</ItemGroup>
```

- [ ] **Step 2: Build to verify packages work**

Run: `dotnet restore DMFT/DMFT.csproj && dotnet build DMFT/DMFT.csproj`
Expected: Build succeeds with no errors

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT.csproj
git commit -m "feat: add EF Core SQLite packages"
```

---

### Task 2: Create DbContext

**Files:**
- Create: `DMFT/Data/DmftDbContext.cs`

- [ ] **Step 1: Create DbContext class**

```csharp
using Microsoft.EntityFrameworkCore;
using DMFT.Model;

namespace DMFT.Data
{
    public class DmftDbContext : DbContext
    {
        public DbSet<LinkInfo> Links { get; set; } = null!;

        private readonly string _dbPath;

        public DmftDbContext()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _dbPath = Path.Combine(folder, "dmft.db");
        }

        public DmftDbContext(DbContextOptions<DmftDbContext> options) : base(options)
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _dbPath = Path.Combine(folder, "dmft.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={_dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LinkInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Url).IsRequired();
                entity.Property(e => e.Time).IsRequired();
            });
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add DMFT/Data/DmftDbContext.cs
git commit -m "feat: create DmftDbContext for SQLite"
```

---

### Task 3: Update LinkInfo with Primary Key

**Files:**
- Modify: `DMFT/Model/LinkInfo.cs:1-28`

- [ ] **Step 1: Add Id property to LinkInfo**

Change:
```csharp
public class LinkInfo
{
    public string Url { get; set; } = string.Empty;
```

To:
```csharp
public class LinkInfo
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
```

- [ ] **Step 2: Commit**

```bash
git add DMFT/Model/LinkInfo.cs
git commit -m "feat: add Id primary key to LinkInfo"
```

---

### Task 4: Migrate BaseContainer to EF Core

**Files:**
- Modify: `DMFT/Model/BaseContainer.cs:1-99`

- [ ] **Step 1: Replace BaseContainer with EF Core implementation**

Replace the entire `BaseContainer.cs` content:

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DMFT.Data;

namespace DMFT.Model
{
    public class BaseContainer
    {
        public List<LinkInfo> Links { get; protected set; } = new List<LinkInfo>();
        public bool IsLoading { get; protected set; } = false;
        public event System.Action? OnLoadingStateChanged;
        
        public DMFT.Model.ToastService? Toast { get; set; }
        public string ToastScope { get; set; } = "Main";

        protected readonly DmftDbContext _context;
        protected readonly string _containerType;

        public BaseContainer(DmftDbContext context, string containerType)
        {
            _context = context;
            _containerType = containerType;
            IsLoading = true;
        }

        public async Task LoadContainerAsync()
        {
            IsLoading = true;
            OnLoadingStateChanged?.Invoke();

            try
            {
                Links = await _context.Links
                    .Where(l => l.Url.StartsWith(_containerType))
                    .OrderByDescending(l => l.Time)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex, "LoadContainer");
            }
            finally
            {
                IsLoading = false;
                OnLoadingStateChanged?.Invoke();
            }
        }

        public async Task SaveContainerAsync()
        {
            try
            {
                IsLoading = true;
                OnLoadingStateChanged?.Invoke();

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex, "SaveContainer");
            }
            finally
            {
                IsLoading = false;
                OnLoadingStateChanged?.Invoke();
            }
        }

        protected void ShowError(Exception ex, string context)
        {
            string msg = $"{context} error: {ex.GetType().Name} - {ex.Message}";
            if (ex.InnerException != null)
            {
                msg += $" (Inner: {ex.InnerException.Message})";
            }
#if DEBUG
            msg += $" | StackTrace: {ex.StackTrace}";
#endif
            Toast?.Show(msg, ToastLevel.Error, ToastScope);
            System.Diagnostics.Debug.WriteLine($"{context} error: {ex}");
        }

        public async Task LoadContainer() => await LoadContainerAsync();
        public async Task SaveContainer() => await SaveContainerAsync();
    }
}
```

> **Note:** This is a simplified version. The actual implementation may need to handle filtering differently. Let's refine this after seeing how the current containers work.

- [ ] **Step 2: Commit**

```bash
git add DMFT/Model/BaseContainer.cs
git commit -m "feat: migrate BaseContainer to use EF Core"
```

---

### Task 5: Update MainContainer

**Files:**
- Modify: `DMFT/Model/MainContainer.cs:1-36`

- [ ] **Step 1: Update MainContainer to use EF Core**

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DMFT.Data;

namespace DMFT.Model
{
    public class MainContainer : BaseContainer
    {
        public MainContainer(DmftDbContext context) : base(context, "main")
        {
            ToastScope = "Main";
        }

        public async Task ClearAllFromMainAsync()
        {
            var mainLinks = _context.Links.Where(l => l.Url.StartsWith("main")).ToList();
            _context.Links.RemoveRange(mainLinks);
            Links.Clear();
            await SaveContainerAsync();
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add DMFT/Model/MainContainer.cs
git commit -m "feat: update MainContainer for EF Core"
```

---

### Task 6: Update HistoryContainer

**Files:**
- Modify: `DMFT/Model/HistoryContainer.cs:1-72`

- [ ] **Step 1: Update HistoryContainer to use EF Core**

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DMFT.Data;

namespace DMFT.Model
{
    public class HistoryContainer : BaseContainer
    {
        public int MaxRecords { get; set; } = 1000;

        public HistoryContainer(DmftDbContext context) : base(context, "history")
        {
            ToastScope = "History";
        }

        public async Task EnforceCapacityAsync()
        {
            var historyLinks = await _context.Links
                .Where(l => l.Url.StartsWith("history"))
                .OrderByDescending(l => l.Time)
                .ToListAsync();

            if (historyLinks.Count <= MaxRecords) return;

            int excess = historyLinks.Count - MaxRecords;
            var toRemove = historyLinks.Take(excess).ToList();
            _context.Links.RemoveRange(toRemove);
            await SaveContainerAsync();
        }

        public async Task ClearAllAsync()
        {
            var historyLinks = _context.Links.Where(l => l.Url.StartsWith("history")).ToList();
            _context.Links.RemoveRange(historyLinks);
            Links.Clear();
            await SaveContainerAsync();
        }

        public async Task ReInstallAsync(LinkInfo item, MainContainer mainContainer)
        {
            if (item == null || mainContainer == null) return;

            bool removed = _context.Links.Remove(item);
            if (!removed)
            {
                var match = await _context.Links.FirstOrDefaultAsync(l => 
                    l.Url == item.Url && l.Time == item.Time);
                if (match != null)
                {
                    _context.Links.Remove(match);
                    item = match;
                }
            }

            if (!mainContainer.Links.Contains(item))
            {
                item.Status = StatusMessage.Waiting;
                mainContainer.Links.Add(item);
            }

            await SaveContainerAsync();
            await mainContainer.SaveContainerAsync();
        }

        public async Task ReInstall(LinkInfo item, MainContainer mainContainer) 
            => await ReInstallAsync(item, mainContainer);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add DMFT/Model/HistoryContainer.cs
git commit -m "feat: update HistoryContainer for EF Core"
```

---

### Task 7: Update MauiProgram.cs for DI

**Files:**
- Modify: `DMFT/MauiProgram.cs:1-59`

- [ ] **Step 1: Register DbContext and update container registrations**

Replace the container registration section:

```csharp
// Add DbContext
builder.Services.AddDbContext<DmftDbContext>(options =>
{
    var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var dbPath = Path.Combine(folder, "dmft.db");
    options.UseSqlite($"Data Source={dbPath}");
});

// Toast service
builder.Services.AddSingleton<ToastService>();

// Containers - now require DbContext
builder.Services.AddScoped<MainContainer>(provider =>
{
    var context = provider.GetRequiredService<DmftDbContext>();
    var container = new MainContainer(context);
    container.Toast = provider.GetService<ToastService>();
    return container;
});

builder.Services.AddScoped<HistoryContainer>(provider =>
{
    var context = provider.GetRequiredService<DmftDbContext>();
    var container = new HistoryContainer(context);
    container.Toast = provider.GetService<ToastService>();
    return container;
});
```

- [ ] **Step 2: Add using statement**

Add at top:
```csharp
using DMFT.Data;
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build DMFT/DMFT.csproj`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add DMFT/MauiProgram.cs
git commit -m "feat: register DbContext in DI container"
```

---

### Task 8: Create Database on Startup

**Files:**
- Modify: `DMFT/MauiProgram.cs`

- [ ] **Step 1: Ensure database is created on app start**

After `builder.Build()`, add:

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DmftDbContext>();
    context.Database.EnsureCreated();
}
```

- [ ] **Step 2: Commit**

```bash
git add DMFT/MauiProgram.cs
git commit -m "feat: ensure database created on startup"
```

---

### Task 9: Run Tests and Verify

**Files:**
- Test: `DMFT.Tests/DMFT.Tests.csproj`

- [ ] **Step 1: Run existing tests**

Run: `dotnet test DMFT.Tests/DMFT.Tests.csproj`
Expected: All 39 tests pass

- [ ] **Step 2: If tests fail, fix issues**

- [ ] **Step 3: Commit**

```bash
git add .
git commit -m "test: verify existing tests pass after EF Core migration"
```

---

## Questions for Clarification

Before proceeding, I need your input:

1. **Data Strategy**: Currently URLs don't have prefixes like "main:" or "history:". Should we:
   - A) Add a `ContainerType` field to `LinkInfo` to distinguish main vs history?
   - B) Use a separate DbSet for each container (MainLinks, HistoryLinks)?
   - C) Keep all links in one table and filter in the container's `LoadContainerAsync`?

2. **Migration of Existing Data**: Should we migrate the existing JSON files to SQLite, or start fresh with an empty database?

3. **Transaction Strategy**: The current code does multiple saves (MainContainer + HistoryContainer). Should we wrap these in a database transaction for consistency?

---

## Plan Complete

**Two execution options:**

1. **Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

2. **Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?