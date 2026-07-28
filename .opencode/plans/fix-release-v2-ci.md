# Fix Release v2.0.0 CI

## Problem
Release CI fails at `Publish DMFT (Windows)` with error:
```
MSB3030: Could not copy file DMFT.Updater.exe — file not found at bin/Release/net10.0/DMFT.Updater.exe
```

## Root Cause
`dotnet publish` passes `RuntimeIdentifier=win-x64` (from `--self-contained`) to the MSBuild call for DMFT.Updater. Since DMFT.Updater.csproj now has `RuntimeIdentifiers=win-x64;win-x86`, output goes to `bin/Release/net10.0/win-x64/DMFT.Updater.exe`, but the Copy task in DMFT.csproj looks at `bin/Release/net10.0/DMFT.Updater.exe`.

## Steps

### 1. Revert DMFT.Updater.csproj
Remove `RuntimeIdentifiers` line added earlier — DMFT.Updater is a simple console app without MAUI, doesn't need it.

**File:** `DMFT.Updater/DMFT.Updater.csproj`
- Remove: `<RuntimeIdentifiers>win-x64;win-x86</RuntimeIdentifiers>`

### 2. Update release.yml
Replace the two restore steps (original + Updater with RID) with:
- Restore main project (as original)
- Build DMFT.Updater **before** publish (so output lands at expected path)

**File:** `.github/workflows/release.yml`
- Replace:
  ```yaml
      - name: Restore dependencies
        run: dotnet restore ${{ env.PROJECT_PATH }}

      - name: Restore DMFT.Updater with RID
        run: dotnet restore DMFT.Updater/DMFT.Updater.csproj --runtime win-x64
  ```
- With:
  ```yaml
      - name: Restore dependencies
        run: dotnet restore ${{ env.PROJECT_PATH }}

      - name: Build DMFT.Updater
        run: dotnet build DMFT.Updater/DMFT.Updater.csproj -c Release
  ```

### 3. Commit & push to master

### 4. Recreate tag + release
- Delete old v2.0.0 tag (local + remote)
- Delete old v2.0.0 release
- Create new tag v2.0.0 at HEAD
- Create new release v2.0.0

### 5. Wait for CI
- Watch GitHub Action run to completion
- Verify Release asset zip is attached
