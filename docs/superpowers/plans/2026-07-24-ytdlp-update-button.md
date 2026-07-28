# yt-dlp Update Button Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an "Update yt-dlp" button to the Settings page that shows the current version and lets the user update yt-dlp with one click.

**Architecture:** The service layer already exists (`IYtDlpUpdateService` / `YtDlpUpdateService` with `GetCurrentVersionAsync()` and `UpdateAsync()`, registered in DI at `MauiProgram.cs:62`). Only Settings.razor needs UI + logic.

**Tech Stack:** .NET 10, MAUI Blazor, xUnit v3 + Moq

## Global Constraints

- Blazor InteractiveServer render mode
- ToastService for notifications (not MAUI CommunityToolkit)
- Follow existing Settings.razor patterns (same style of loading state, error handling, button styling)
- All new strings in English (matches existing code — no i18n)

---

### Task 1: Add yt-dlp update UI to Settings.razor

**Files:**
- Modify: `DMFT/DMFT/Components/Pages/Settings.razor` (whole file)
- Rebuild: `DMFT/DMFT/Components/vite-project` (frontend build)

**Interfaces:**
- Consumes: `IYtDlpUpdateService` (injected), `ToastService` (already injected via `Toast` property from `_Imports.razor`)
- Produces: UI with current yt-dlp version display, update button, loading/result state

- [ ] **Step 1: Add inject and state variables**

Add `@inject IYtDlpUpdateService YtDlpUpdateSvc` after line 3.

Add these fields after `_checking` (line 148):

```csharp
private string? _ytDlpVersion;
private bool _ytDlpUpdating;
private string? _ytDlpUpdateMessage;
private bool _ytDlpUpdateError;
```

- [ ] **Step 2: Add `LoadYtDlpVersion` method and call it on init**

Add after `LoadSettings()` (line 189):

```csharp
private async Task LoadYtDlpVersion()
{
    var version = await YtDlpUpdateSvc.GetCurrentVersionAsync();
    if (version != null)
    {
        var parts = version.Split('\n');
        _ytDlpVersion = parts.FirstOrDefault()?.Trim();
    }
}
```

Call it at the end of `OnInitializedAsync()`, after `await LoadSettings()`:

```csharp
    await LoadYtDlpVersion();
```

- [ ] **Step 3: Add `UpdateYtDlp` method**

Add after `LoadYtDlpVersion()`:

```csharp
private async Task UpdateYtDlp()
{
    _ytDlpUpdating = true;
    _ytDlpUpdateMessage = null;
    try
    {
        var newVersion = await YtDlpUpdateSvc.UpdateAsync();
        if (newVersion != null)
        {
            _ytDlpVersion = newVersion;
            _ytDlpUpdateMessage = $"Updated to {newVersion}";
            _ytDlpUpdateError = false;
            Toast.Show($"yt-dlp updated to {newVersion}", ToastLevel.Success);
        }
        else
        {
            _ytDlpUpdateMessage = "Update failed";
            _ytDlpUpdateError = true;
            Toast.Show("yt-dlp update failed", ToastLevel.Error);
        }
    }
    catch (Exception ex)
    {
        _ytDlpUpdateMessage = ex.Message;
        _ytDlpUpdateError = true;
        Toast.Show($"yt-dlp update error: {ex.Message}", ToastLevel.Error);
    }
    finally
    {
        _ytDlpUpdating = false;
    }
}
```

- [ ] **Step 4: Add yt-dlp version + update UI to the razor markup**

Insert after the Output Template div (closing `</div>` at line 76), before Download Quality section (line 79):

```razor
    <div class="pt-2 border-t border-border">
        <div class="flex items-center gap-3 flex-wrap">
            <span class="text-sm text-secondary">
                yt-dlp version:
                @if (_ytDlpVersion != null)
                {
                    <strong>@_ytDlpVersion</strong>
                }
                else
                {
                    <em class="text-danger">not found</em>
                }
            </span>
            <button class="btn btn-primary text-sm"
                    @onclick="UpdateYtDlp" disabled="@_ytDlpUpdating">
                @(_ytDlpUpdating ? "Updating..." : "Update yt-dlp")
            </button>
        </div>
        @if (_ytDlpUpdateMessage != null)
        {
            <div class="mt-2 text-sm @(_ytDlpUpdateError ? "text-danger" : "text-success")">
                @_ytDlpUpdateMessage
            </div>
        }
    </div>
```

- [ ] **Step 5: Rebuild frontend**

```bash
cd DMFT/DMFT/Components/vite-project
npm run build
```

Expected: build succeeds (Tailwind picks up any new class usage in the modified .razor).

- [ ] **Step 6: Build .NET project to verify**

```bash
dotnet build DMFT/DMFT/DMFT.csproj -c Release --no-restore
```

Expected: Build succeeds with no errors.

- [ ] **Step 7: Commit**

```bash
git add DMFT/DMFT/Components/Pages/Settings.razor DMFT/DMFT/Components/vite-project/wwwroot/build/
git commit -m "feat: add yt-dlp version display and update button to settings"
```