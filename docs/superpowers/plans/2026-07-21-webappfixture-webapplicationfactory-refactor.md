# WebAppFixture → WebApplicationFactory Refactor

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the manual `WebApplication.CreateBuilder` + port 0 pattern in `WebAppFixture` with .NET 10's `WebApplicationFactory<Program>.UseKestrel(0)` API, eliminating middleware duplication and aligning with Microsoft's recommended test infrastructure.

**Architecture:** `WebAppFixture` currently duplicates the entire middleware pipeline (`UseHsts`, `MapRazorComponents`, etc.) from `DMFT.Web/Program.cs`. `WebApplicationFactory<Program>` with `UseKestrel(0)` inherits the real app's full pipeline — DI, middleware, routing — with zero overrides. No `ConfigureWebHost` needed. The factory uses Kestrel on a dynamic port, giving Playwright a real HTTP URL to navigate to.

**Tech Stack:** .NET 10, `Microsoft.AspNetCore.Mvc.Testing` (v10.0.0), `Microsoft.Playwright.Xunit.v3`, xUnit

## Global Constraints

- Package `Microsoft.AspNetCore.Mvc.Testing` version must be `10.0.0`
- `UseKestrel(0)` only available in .NET 10
- All existing test classes keep their `[Collection("WebApp")]` and `WebAppFixture` constructor injection unchanged
- The `BaseUrl` property must return the actual Kestrel port after the server starts
- All 5 existing `Seed*`/`ResetDatabaseAsync` helper methods must maintain identical signatures, with `_app.Services` replaced by `Services` (inherited from `WebApplicationFactory`)

---

### Task 1: Add `public partial class Program` + restore `IFormFactor` registration

**Files:**
- Modify: `DMFT/DMFT.Web/Program.cs` (append 1 line + 1 DI line)

**Interfaces:**
- Consumes: nothing
- Produces: `WebApplicationFactory<Program>` can resolve the entry point assembly; `IFormFactor` resolved at runtime

**Why `IFormFactor`?** `NavMenu.razor` injects `IFormFactor` but the registration was dropped during a previous edit. Without it the fixture's `Program.cs` pipeline will throw at runtime.

- [ ] **Step 1: Add `IFormFactor` DI + `partial class Program`**

After `builder.Services.AddRazorComponents()...` and before platform services, insert `IFormFactor`. Append `public partial class Program { }` at the end:

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Platform services
builder.Services.AddSingleton<IFormFactor>(new FormFactor());
builder.Services.AddSingleton<IStoragePathProvider>(sp => {
```

Wait — that's already in the middle of the file. Show the exact diffs:

Line 17 area — insert after `builder.Services.AddRazorComponents()` block (line 14):

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Platform services
builder.Services.AddSingleton<IFormFactor>(new FormFactor());
builder.Services.AddSingleton<IStoragePathProvider>(sp => {
```

End of file — append:

```csharp

app.Run();

public partial class Program { }
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build DMFT/DMFT.Web/DMFT.Web.csproj -c Release`
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT.Web/Program.cs
git commit -m "refactor: add partial class Program + restore missing IFormFactor DI"
```

---

### Task 2: Add Microsoft.AspNetCore.Mvc.Testing package

**Files:**
- Modify: `DMFT.Test.Web/DMFT.Test.Web.csproj` (add 1 package reference)

- [ ] **Step 1: Add package reference**

Insert after `Microsoft.Playwright.Xunit.v3`:

```xml
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
```

- [ ] **Step 2: Restore and build**

Run: `dotnet restore DMFT.Test.Web/DMFT.Test.Web.csproj && dotnet build DMFT.Test.Web/DMFT.Test.Web.csproj -c Release`
Expected: Restore + Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add DMFT.Test.Web/DMFT.Test.Web.csproj
git commit -m "chore: add Microsoft.AspNetCore.Mvc.Testing for WebApplicationFactory"
```

---

### Task 3: Rewrite WebAppFixture — no ConfigureWebHost

**Files:**
- Rewrite: `DMFT.Test.Web/WebAppFixture.cs`
- Unchanged: `DMFT.Test.Web/AppCollectionFixture.cs`

**Design:** `WebApplicationFactory<Program>` inherits everything from `Program.cs` DI + middleware. No `ConfigureWebHost` override at all. Only changes:
- `UseKestrel(0)` in constructor
- `InitializeAsync` → `CreateDefaultClient()` + read bound address from `IServerAddressesFeature`
- `DisposeAsync` → call base `DisposeAsync()`
- Helper methods → `_app.Services` → `Services` (inherited from factory)

- [ ] **Step 1: Write the new WebAppFixture**

```csharp
using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DMFT.Test.Web;

public class WebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string BaseUrl { get; private set; } = null!;

    public WebAppFixture()
    {
        UseKestrel(0);
    }

    public async ValueTask InitializeAsync()
    {
        CreateDefaultClient();
        var server = Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        BaseUrl = addresses?.First() ?? "http://localhost";

        try
        {
            using var scope = Services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var context = await factory.CreateDbContextAsync();
            await context.Database.MigrateAsync();
            var settings = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();
            await settings.InitAsync();
        }
        catch (Exception ex)
        {
            var logger = Services.GetRequiredService<ILogger<WebAppFixture>>();
            logger.LogWarning(ex, "Database migration failed (non-fatal)");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = await factory.CreateDbContextAsync();
        context.Database.EnsureCreated();
        context.RemoveRange(context.Set<DownloadItem>());
        await context.SaveChangesAsync();
    }

    public async Task SeedDownloadItemAsync(DownloadItem item)
    {
        var svc = Services.GetRequiredService<DownloadService>();
        await svc.AddDownloadAsync(item);
    }

    public async Task SeedMainItemAsync(string url, string platform = "YouTube", int mode = 1, string videoId = "test123")
    {
        var svc = Services.GetRequiredService<DownloadService>();
        await svc.AddDownloadAsync(new DownloadItem
        {
            Id = Guid.NewGuid(),
            Url = url,
            Platform = platform,
            VideoId = videoId,
            Status = StatusCodes.New,
            DownloadMode = mode,
            Time = DateTime.UtcNow
        });
    }

    public async Task SeedHistoryItemAsync(string url, string platform = "YouTube", int statusCode = 4)
    {
        var svc = Services.GetRequiredService<DownloadService>();
        await svc.AddDownloadAsync(new DownloadItem
        {
            Id = Guid.NewGuid(),
            Url = url,
            Platform = platform,
            VideoId = Guid.NewGuid().ToString()[..8],
            Status = statusCode,
            DownloadMode = 1,
            Time = DateTime.UtcNow.AddHours(-1)
        });
    }

    public async Task SeedAppSettingAsync(string key, string value)
    {
        var svc = Services.GetRequiredService<DownloadService>();
        await svc.SetAppSettingAsync(key, value);
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build DMFT.Test.Web/DMFT.Test.Web.csproj -c Release`
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 3: Run existing tests**

Run: `dotnet test DMFT.Test.Web/DMFT.Test.Web.csproj -c Release --no-build`
Expected: Tests pass (navigation/empty-state at minimum).

- [ ] **Step 4: Commit**

```bash
git add DMFT.Test.Web/WebAppFixture.cs
git commit -m "refactor: replace manual WebApplication with WebApplicationFactory<Program>.UseKestrel(0)"
```

---

### Task 4: Full solution build and test run

**Files:**
- None — verification only

- [ ] **Step 1: Build all**

Run: `dotnet build DMFT.slnx -c Release`
Expected: 0 errors.

- [ ] **Step 2: Test**

Run: `dotnet test DMFT.Test.Web/DMFT.Test.Web.csproj -c Release --no-build`
Expected: Passing.

- [ ] **Step 3: Final commit**

```bash
git add -A && git commit -m "refactor: migrate WebAppFixture to WebApplicationFactory<Program>"
```

---

## Self-Review Checklist

**Spec coverage:**
- ✅ Add `public partial class Program { }` — Task 1
- ✅ Restore `IFormFactor` DI in `Program.cs` — Task 1
- ✅ Add `Microsoft.AspNetCore.Mvc.Testing` package — Task 2
- ✅ No `ConfigureWebHost` — Task 3 fixture has zero DI overrides
- ✅ `UseKestrel(0)` in constructor — Task 3
- ✅ Keep all 5 helper methods with identical signatures — Task 3
- ✅ `BaseUrl` from `IServerAddressesFeature` — Task 3
- ✅ Remove middleware duplication — implicit (no manual pipeline building)

**Placeholder scan:** No TBD, TODO, or placeholder content.

**Type consistency:**
- `SeedMainItemAsync(string, string, int, string)` — same as original
- `SeedHistoryItemAsync(string, string, int)` — same as original
- `_fixture.Services.GetRequiredService<T>()` replaces `_app.Services.GetRequiredService<T>()`
- `DisposeAsync()` calls base `DisposeAsync()` (no `_app?.StopAsync()`)
