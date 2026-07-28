# Auto Database Migration at Startup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure the SQLite database schema is auto-created/applied when either the MAUI app or the Web app starts, so users don't have to run `dotnet ef database update` manually.

**Architecture:** Both `DMFT/DMFT/MauiProgram.cs` and `DMFT/DMFT.Web/Program.cs` register `IDbContextFactory<AppDbContext>` via DI but never call `MigrateAsync()`. We add a post-build startup hook that resolves the factory, creates a context, and calls `database.MigrateAsync()`. Using `MigrateAsync()` (not `EnsureCreated()`) because there's already an EF Core migration (`InitialCreate`) and `MigrateAsync()` is idempotent — it applies only pending migrations.

**Tech Stack:** .NET 10.0, EF Core 10.0.8, SQLite, MAUI, ASP.NET Core

## Global Constraints

- Target both entry points: `DMFT/DMFT/MauiProgram.cs` and `DMFT/DMFT.Web/Program.cs`
- Use `IDbContextFactory<AppDbContext>` (already registered in DI) to create the context
- Use `MigrateAsync()` — not `EnsureCreated()` — to stay compatible with future migrations
- Wrap in `try/catch` to log but not crash the app if migration fails (first run on read-only FS, etc.)
- Keep `AppDbContextFactory.cs` unchanged — it's design-time only for CLI `dotnet ef`

---

## File Structure

| File | Responsibility | Action |
|------|----------------|--------|
| `DMFT/DMFT/MauiProgram.cs` | MAUI app entry point — add startup migration | Modify |
| `DMFT/DMFT.Web/Program.cs` | Web app entry point — add startup migration | Modify |

---

## Task 1: Add Auto Migration to MAUI App

**Files:**
- Modify: `DMFT/DMFT/MauiProgram.cs:61`

**Interfaces:**
- Consumes: `IDbContextFactory<AppDbContext>` (already registered at line 31-35)
- Produces: Database schema created on MAUI app startup

- [ ] **Step 1: Read the current file to verify structure**

```bash
cat "DMFT/DMFT/MauiProgram.cs"
```

Expected: File ends at line 63 with `return builder.Build();`

- [ ] **Step 2: Extract builder.Build into a variable, add migration block, return app**

Replace the bottom of `MauiProgram.cs` from line 54 onward:

Old:
```csharp
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

New:
```csharp
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Auto-apply pending EF Core migrations on startup
        try
        {
            using var scope = app.Services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database migration failed: {ex.Message}");
        }

        return app;
    }
}
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build DMFT/DMFT/DMFT.csproj -c Release
```

Expected: Build succeeds with no errors

- [ ] **Step 4: Commit**

```bash
git add DMFT/DMFT/MauiProgram.cs
git commit -m "fix: auto-apply EF Core migrations on MAUI startup"
```

---

## Task 2: Add Auto Migration to Web App

**Files:**
- Modify: `DMFT/DMFT.Web/Program.cs:46`

**Interfaces:**
- Consumes: `IDbContextFactory<AppDbContext>` (already registered at line 23-27)
- Produces: Database schema created on Web app startup

- [ ] **Step 1: Read the current file to verify structure**

```bash
cat "DMFT/DMFT.Web/Program.cs"
```

Expected: File ends at line 68 with `app.Run();`

- [ ] **Step 2: Add migration block between `var app = builder.Build();` and the environment check**

Old (lines 46-48):
```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
```

New:
```csharp
var app = builder.Build();

// Auto-apply pending EF Core migrations on startup
try
{
    using var scope = app.Services.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.Migrate();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Database migration failed (non-fatal)");
}

if (app.Environment.IsDevelopment())
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build DMFT/DMFT.Web/DMFT.Web.csproj -c Release
```

Expected: Build succeeds with no errors

- [ ] **Step 4: Commit**

```bash
git add DMFT/DMFT.Web/Program.cs
git commit -m "fix: auto-apply EF Core migrations on Web startup"
```

---

## Self-Review

**1. Spec coverage:**
- ✅ MAUI entry point gets auto-migration
- ✅ Web entry point gets auto-migration
- ✅ Uses `MigrateAsync()` (idempotent, migration-compatible)
- ✅ Uses existing `IDbContextFactory<AppDbContext>` from DI
- ✅ `AppDbContextFactory.cs` left unchanged (design-time only)
- ✅ try/catch prevents crash if migration fails

**2. Placeholder scan:** No placeholders found. All code is complete.

**3. Type consistency:**
- `IDbContextFactory<AppDbContext>` — same type registered in both `MauiProgram.cs:31` and `Program.cs:23`
- `database.Migrate()` — method on `Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions`
- `IMediaDownloader` not involved — no cross-task type issues
