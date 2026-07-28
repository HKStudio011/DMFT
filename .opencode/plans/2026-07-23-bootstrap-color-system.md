# Bootstrap Color System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Replace MD3 color system + `data-color` accent switching with Bootstrap-style semantic colors.

**Architecture:** Flatten CSS variables to Bootstrap-style names (`--primary`, `--secondary`, `--success`, `--danger`, `--warning`, `--info`, `--light`, `--dark`) + structural tokens (`--surface`, `--surface-variant`, `--background`, `--outline`, `--outline-variant`, `--muted`). Tailwind v4 `@theme inline` auto-generates `bg-*`, `text-*`, `border-*`, `outline-*` classes. Remove `data-color` JS + accent picker from Settings.

**Tech Stack:** Tailwind CSS v4, CSS custom properties, .NET MAUI Blazor

## Global Constraints

- Keep `data-theme` light/dark switching intact
- No Bootstrap library import — just Tailwind v4 with Bootstrap-inspired names
- Light/dark theme modes remain; only `data-color` accent switching is removed
- `--color-muted` must be defined for `text-muted` utility class

---

### Task 1: Rewrite theme.css — new color variables, remove `data-color`

**Files:**
- Modify: `DMFT/DMFT/Components/vite-project/src/css/theme.css`

**Interfaces:**
- Consumes: existing light/dark theme structure
- Produces: new CSS variables consumed by Tailwind `@theme inline` and all .razor files

- [ ] **Step 1: Replace `@theme inline` block**

Old (MD3 — 43 lines):
```css
@theme inline {
    --color-primary: var(--primary);
    --color-primary-container: var(--primary-container);
    ...
}
```

New:
```css
@theme inline {
    --color-primary: var(--primary);
    --color-secondary: var(--secondary);
    --color-success: var(--success);
    --color-danger: var(--danger);
    --color-warning: var(--warning);
    --color-info: var(--info);
    --color-light: var(--light);
    --color-dark: var(--dark);

    --color-surface: var(--surface);
    --color-surface-variant: var(--surface-variant);
    --color-background: var(--background);
    --color-outline: var(--outline);
    --color-outline-variant: var(--outline-variant);
    --color-muted: var(--muted);
}
```

- [ ] **Step 2: Replace light theme variables**

Old (lines 46-88):
```css
:root, [data-theme="light"] {
    --primary: #1d4ed8;
    --primary-container: #3b82f6;
    ...
}
```

New — Bootstrap-inspired light values:
```css
:root, [data-theme="light"] {
    --primary: #0d6efd;
    --secondary: #6c757d;
    --success: #198754;
    --danger: #dc3545;
    --warning: #ffc107;
    --info: #0dcaf0;
    --light: #f8f9fa;
    --dark: #212529;

    --surface: #ffffff;
    --surface-variant: #f0f0f0;
    --background: #ffffff;
    --outline: #dee2e6;
    --outline-variant: #e9ecef;
    --muted: #6c757d;
}
```

- [ ] **Step 3: Replace dark theme variables**

Old (lines 90-133) → New — Bootstrap-inspired dark values:
```css
[data-theme="dark"] {
    --primary: #6ea8fe;
    --secondary: #adb5bd;
    --success: #75b798;
    --danger: #ea868f;
    --warning: #ffda6a;
    --info: #6edff6;
    --light: #f8f9fa;
    --dark: #212529;

    --surface: #212529;
    --surface-variant: #343a40;
    --background: #1a1d20;
    --outline: #495057;
    --outline-variant: #343a40;
    --muted: #adb5bd;
}
```

- [ ] **Step 4: Remove `[data-color="..."]` accent blocks (lines 135-189)**

Delete the 5 `[data-color="gold"]`, `[data-color="blue"]`, `[data-color="green"]`, `[data-color="purple"]`, `[data-color="red"]` blocks completely.

- [ ] **Step 5: Rebuild frontend and verify**

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
```

Expected: No errors, `styles.css` generated with new CSS variables.

- [ ] **Step 6: Commit**

```bash
git add DMFT/DMFT/Components/vite-project/src/css/theme.css
git commit -m "refactor(theme): replace MD3 colors with Bootstrap-style palette, remove data-color"
```

---

### Task 2: Remove `data-color` from JavaScript

**Files:**
- Modify: `DMFT/DMFT/Components/vite-project/src/ts/main.ts`

**Interfaces:**
- Consumes: `applyTheme` contract updated (no color param)
- Produces: JS module that only handles `data-theme`

- [ ] **Step 1: Update `applyTheme` function**

Remove `color` parameter and `data-color` attribute setter:

```typescript
function applyTheme(theme: string) {
    currentThemeSetting = theme;
    const html = document.documentElement;
    const resolvedTheme = theme === 'system'
        ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
        : theme;
    html.setAttribute('data-theme', resolvedTheme);
}
```

- [ ] **Step 2: Update `dmftTheme` object and boot code**

```typescript
(window as any).dmftTheme = { applyTheme };

const metaTheme = document.querySelector('meta[name="dmft-theme"]')?.getAttribute('content') || 'system';
applyTheme(metaTheme);
```

- [ ] **Step 3: Rebuild frontend**

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
```

- [ ] **Step 4: Commit**

```bash
git add DMFT/DMFT/Components/vite-project/src/ts/main.ts
git commit -m "refactor(js): remove data-color from theme switch"
```

---

### Task 3: Update C# backend — remove accentColor

**Files:**
- Modify: `DMFT/DMFT/Services/Implements/AppSettingsService.cs`

**Interfaces:**
- Consumes: `IAppSettingsService` interface (no change needed — signature stays `ApplyThemeAsync(IJSRuntime js)`)
- Produces: updated `ApplyThemeAsync` that only passes theme

- [ ] **Step 1: Update `ApplyThemeAsync` in AppSettingsService.cs**

Change line 51 from:
```csharp
var color = Get("accentColor") ?? "blue";
await js.InvokeVoidAsync("dmftTheme.applyTheme", theme, color);
```
To:
```csharp
await js.InvokeVoidAsync("dmftTheme.applyTheme", theme);
```

- [ ] **Step 2: Verify build**

```bash
dotnet build DMFT/DMFT/DMFT.csproj -c Release
```

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT/Services/Implements/AppSettingsService.cs
git commit -m "refactor(core): remove accentColor from ApplyThemeAsync"
```

---

### Task 4: Remove accent color picker from Settings page

**Files:**
- Modify: `DMFT/DMFT/Components/Pages/Settings.razor`

- [ ] **Step 1: Remove Accent Color select HTML (lines 25-35)**

Delete:
```razor
            <div>
                <label class="block text-sm font-medium text-on-surface mb-1">Accent Color</label>
                <select class="w-full px-3 py-2 border border-outline rounded bg-surface text-on-surface focus:outline-none focus:ring-2 focus:ring-primary"
                        @bind="AccentColor">
                    <option value="blue">Blue</option>
                    <option value="gold">Gold</option>
                    <option value="green">Green</option>
                    <option value="purple">Purple</option>
                    <option value="red">Red</option>
                </select>
            </div>
```

Also remove the `col-span-2` grid wrapper — keep the Theme section as a single column or keep grid with just Mode.

- [ ] **Step 2: Remove `AccentColor` property (line 136)**

Remove `private string AccentColor = "blue";`

- [ ] **Step 3: Remove `accentColor` load from `LoadSettings` (lines 173-174)**

Delete:
```csharp
var color = await db.AppSettings.FindAsync("accentColor");
if (color != null) AccentColor = color.Value;
```

- [ ] **Step 4: Remove `accentColor` save from `SaveSettings` (line 242)**

Delete: `await SetAppSettingAsync(db, "accentColor", AccentColor);`

- [ ] **Step 5: Remove `AccentColor = "blue"` from `ResetSettings` (line 283)**

Delete: `AccentColor = "blue";`

- [ ] **Step 6: Update `ApplyTheme()` method (line 199-202)**

Change from:
```csharp
private async Task ApplyTheme()
{
    await JS.InvokeVoidAsync("dmftTheme.applyTheme", ThemeMode, AccentColor);
}
```
To:
```csharp
private async Task ApplyTheme()
{
    await JS.InvokeVoidAsync("dmftTheme.applyTheme", ThemeMode);
}
```

- [ ] **Step 7: Verify build**

```bash
dotnet build DMFT/DMFT/DMFT.csproj -c Release
```

- [ ] **Step 8: Commit**

```bash
git add DMFT/DMFT/Components/Pages/Settings.razor
git commit -m "refactor(ui): remove accent color picker from settings"
```

---

### Task 5: Update Layout files — MainLayout + NavMenu

**Files:**
- Modify: `DMFT/DMFT/Components/Layout/MainLayout.razor`
- Modify: `DMFT/DMFT/Components/Layout/NavMenu.razor`

**Class changes needed:**

| File | Old class | New class |
|------|-----------|-----------|
| MainLayout.razor:2 | `text-on-surface` | `text-surface` |
| MainLayout.razor:4 | `border-outline-variant` | `border-outline-variant` (no change) |
| NavMenu.razor:6 | `text-primary` | `text-primary` (no change) |
| NavMenu.razor:10,15,20 | `text-on-surface-variant` | `text-surface-variant` |
| NavMenu.razor:10,15,20 | `hover:bg-surface-variant` | `hover:bg-surface-variant` (no change) |
| NavMenu.razor:27 | `text-on-surface-dim` | `text-muted` |
| NavMenu.razor:27 | `border-outline-variant` | `border-outline-variant` (no change) |

- [ ] **Step 1: Update MainLayout.razor**

Change `text-on-surface` to `text-surface` on line 2.

- [ ] **Step 2: Update NavMenu.razor**

Change `text-on-surface-variant` → `text-surface-variant` on lines 10, 15, 20.
Change `text-on-surface-dim` → `text-muted` on line 27.

- [ ] **Step 3: Rebuild frontend and verify**

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
dotnet build DMFT/DMFT/DMFT.csproj -c Release
```

- [ ] **Step 4: Commit**

```bash
git add DMFT/DMFT/Components/Layout/
git commit -m "refactor(ui): update layout color classes to Bootstrap style"
```

---

### Task 6: Update Main.razor color classes

**Files:**
- Modify: `DMFT/DMFT/Components/Pages/Main.razor`

**Class changes:**

| Line | Old | New |
|------|-----|-----|
| 11 | `text-on-surface` | `text-surface` |
| 12 | `text-on-primary` | `text-light` |
| 22 | `text-on-surface-dim` | `text-muted` |
| 30 | `text-on-surface` | `text-surface` |
| 43 | `bg-primary-container` | `bg-primary/20` |
| 43 | `text-on-primary-container` | `text-primary` |
| 52 | `text-on-primary` | `text-light` |
| 53 | `text-on-surface` | `text-surface` |
| 54 | `text-on-surface-variant` | `text-surface-variant` |
| 57 | `bg-error` | `bg-danger` |
| 57 | `text-on-error` | `text-light` |
| 61 | `text-on-surface-variant` | `text-surface-variant` |
| 80 | `bg-surface-variant` | `bg-surface-variant` (no change) |
| 83 | `text-on-surface-dim` | `text-muted` |
| 86 | `text-on-primary` | `text-light` |
| 88 | `bg-error` | `bg-danger` |
| 88 | `text-on-error` | `text-light` |
| 92 | `bg-primary-container` | `bg-primary/20` |
| 92 | `text-on-primary-container` | `text-primary` |

- [ ] **Step 1: Apply class replacements in Main.razor**

- [ ] **Step 2: Rebuild frontend**

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
```

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT/Components/Pages/Main.razor
git commit -m "refactor(ui): update Main.razor color classes"
```

---

### Task 7: Update History.razor color classes

**Files:**
- Modify: `DMFT/DMFT/Components/Pages/History.razor`

**Class changes:**

| Line | Old | New |
|------|-----|-----|
| 10 | `text-on-surface` | `text-surface` |
| 14 | `text-on-surface-dim` | `text-muted` |
| 20 | `border-outline-variant` | `border-outline-variant` (no change) |
| 21 | `text-on-surface` | `text-surface` |
| 22 | `bg-surface-container-low` | `bg-surface` |
| 22 | `text-on-surface-variant` | `text-surface-variant` |
| 22 | `border-outline-variant` | `border-outline-variant` (no change) |
| 34 | `border-outline-variant` | `border-outline-variant` (no change) |
| 34 | `hover:bg-surface-container-low` | `hover:bg-surface` |
| 36 | `text-on-primary` | `text-light` |
| 38 | `text-on-surface-dim` | `text-muted` |
| 40 | `text-on-surface-dim` | `text-muted` |
| 42 | `text-on-surface-dim` | `text-muted` |
| 44 | `text-on-primary` | `text-light` |
| 46 | `bg-error` | `bg-danger` |
| 46 | `text-on-error` | `text-light` |

- [ ] **Step 1: Apply class replacements in History.razor**

- [ ] **Step 2: Rebuild frontend**

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
```

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT/Components/Pages/History.razor
git commit -m "refactor(ui): update History.razor color classes"
```

---

### Task 8: Update NotFound.razor color classes

**Files:**
- Modify: `DMFT/DMFT/Components/Pages/NotFound.razor`

**Class changes:**

| Line | Old | New |
|------|-----|-----|
| 6 | `text-on-surface-dim` | `text-muted` |
| 7 | `text-on-surface` | `text-surface` |
| 8 | `text-on-surface-variant` | `text-surface-variant` |
| 9 | `text-on-primary` | `text-light` |

- [ ] **Step 1: Apply class replacements in NotFound.razor**

- [ ] **Step 2: Rebuild frontend**

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
```

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT/Components/Pages/NotFound.razor
git commit -m "refactor(ui): update NotFound.razor color classes"
```

---

### Task 9: Update ModalBase.razor color classes

**Files:**
- Modify: `DMFT/DMFT/Components/Components/ModalBase.razor`

**Class changes:**

| Line | Old | New |
|------|-----|-----|
| 4 | `border-outline-variant` | `border-outline-variant` (no change) |
| 5 | `text-on-surface` | `text-surface` |
| 8 | `text-on-surface-variant` | `text-surface-variant` |
| 8 | `hover:text-on-surface` | `hover:text-surface` |
| 14 | `border-outline-variant` | `border-outline-variant` (no change) |
| 14 | `bg-surface-container-lowest` | `bg-surface` |
| 21 | `bg-surface-variant` | `bg-surface-variant` (no change) |
| 21 | `text-on-surface` | `text-surface` |
| 21 | `hover:bg-surface-container-high` | `hover:bg-surface-variant` |

- [ ] **Step 1: Apply class replacements in ModalBase.razor**

- [ ] **Step 2: Rebuild frontend**

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
```

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT/Components/Components/ModalBase.razor
git commit -m "refactor(ui): update ModalBase.razor color classes"
```

---

### Task 10: Update AddModal.razor color classes

**Files:**
- Modify: `DMFT/DMFT/Components/Components/AddModal.razor`

**Class changes:**

| Line | Old | New |
|------|-----|-----|
| 3 | `text-on-surface` | `text-surface` |
| 3 | `border-outline` | `border-outline` (no change) |
| 3 | `placeholder-on-surface-dim` | `placeholder-muted` |
| 3 | `focus:ring-primary` | `focus:ring-primary` (no change) |
| 9 | `text-on-primary` | `text-light` |

- [ ] **Step 1: Apply class replacements in AddModal.razor**

- [ ] **Step 2: Rebuild frontend**

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
```

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT/Components/Components/AddModal.razor
git commit -m "refactor(ui): update AddModal.razor color classes"
```

---

### Task 11: Update ToastContainer.razor color classes

**Files:**
- Modify: `DMFT/DMFT/Components/Components/ToastContainer.razor`

**Class changes:**

| Line | Old | New |
|------|-----|-----|
| 7 | `bg-error text-on-error` | `bg-danger text-light` |
| 7 | `bg-primary text-on-primary` | `bg-primary text-light` |
| 7 | `bg-yellow-500 text-white` | `bg-warning text-dark` |
| 7 | `bg-surface text-on-surface` | `bg-surface text-surface` |

- [ ] **Step 1: Apply class replacements in ToastContainer.razor**

The line 7 conditional classes change as follows:

Old:
```razor
@(t.Level == ToastLevel.Error ? "bg-error text-on-error" : t.Level == ToastLevel.Success ? "bg-primary text-on-primary" : t.Level == ToastLevel.Warning ? "bg-yellow-500 text-white" : "bg-surface text-on-surface")
```

New:
```razor
@(t.Level == ToastLevel.Error ? "bg-danger text-light" : t.Level == ToastLevel.Success ? "bg-primary text-light" : t.Level == ToastLevel.Warning ? "bg-warning text-dark" : "bg-surface text-surface")
```

- [ ] **Step 2: Rebuild frontend**

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
```

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT/Components/Components/ToastContainer.razor
git commit -m "refactor(ui): update ToastContainer.razor color classes"
```

---

### Task 12: Update Settings.razor remaining color classes

**Files:**
- Modify: `DMFT/DMFT/Components/Pages/Settings.razor`

(Class changes only — accent picker removal already done in Task 4)

**Class changes:**

| Line | Old | New |
|------|-----|-----|
| 8 | `text-on-surface` | `text-surface` |
| 13 | `border-outline-variant` | `border-outline-variant` (no change) |
| 14 | `text-on-surface` | `text-surface` |
| 17 | `text-on-surface` | `text-surface` |
| 18 | `text-on-surface` | `text-surface` |
| 18 | `focus:ring-primary` | `focus:ring-primary` (no change) |
| 27 | `text-on-surface` | `text-surface` |
| 27 | `focus:ring-primary` | `focus:ring-primary` (no change) |
| 41 | `text-on-surface` | `text-surface` |
| 45 | `text-on-surface` | `text-surface` |
| 46 | `text-on-surface` | `text-surface` |
| 46 | `focus:ring-primary` | `focus:ring-primary` (no change) |
| 50 | `text-on-surface` | `text-surface` |
| 51 | `text-on-surface` | `text-surface` |
| 51 | `focus:ring-primary` | `focus:ring-primary` (no change) |
| 56 | `text-on-surface` | `text-surface` |
| 57 | `text-on-surface` | `text-surface` |
| 57 | `focus:ring-primary` | `focus:ring-primary` (no change) |
| 61 | `text-on-surface` | `text-surface` |
| 63 | `text-on-surface` | `text-surface` |
| 63 | `focus:ring-primary` | `focus:ring-primary` (no change) |
| 66 | `text-on-surface-dim` | `text-muted` |
| 71 | `text-on-surface` | `text-surface` |
| 73 | `text-on-surface` | `text-surface` |
| 73 | `focus:ring-primary` | `focus:ring-primary` (no change) |
| 76 | `text-on-surface-dim` | `text-muted` |
| 85 | `text-on-surface` | `text-surface` |
| 88 | `text-on-surface` | `text-surface` |
| 89 | `text-on-surface` | `text-surface` |
| 89 | `focus:ring-primary` | `focus:ring-primary` (no change) |
| 102 | `text-on-surface` | `text-surface` |
| 108 | `text-primary` | `text-primary` (no change) |
| 112 | `text-on-surface-dim` | `text-muted` |
| 117 | `text-on-primary` | `text-light` |
| 126 | `text-on-primary` | `text-light` |
| 128 | `bg-surface-variant` | `bg-surface-variant` (no change) |
| 128 | `text-on-surface` | `text-surface` |
| 128 | `hover:bg-surface-container-high` | `hover:bg-surface-variant` |

- [ ] **Step 1: Apply class replacements in Settings.razor**

- [ ] **Step 2: Rebuild frontend**

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
```

- [ ] **Step 3: Verify full solution build**

```bash
dotnet build DMFT.slnx -c Release
```

- [ ] **Step 4: Commit**

```bash
git add DMFT/DMFT/Components/Pages/Settings.razor
git commit -m "refactor(ui): update Settings.razor color classes"
```

---

### Task 13: Full rebuild and verify

- [ ] **Step 1: Frontend rebuild**

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
```

- [ ] **Step 2: Solution build**

```bash
dotnet build DMFT.slnx -c Release
```

- [ ] **Step 3: Final commit with any remaining changes**

```bash
git status
git add -A
git commit -m "refactor(theme): complete Bootstrap color system migration"
```
