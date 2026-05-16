# AGENTS.md

## Key Commands

```bash
# Restore & build
dotnet restore DMFT/DMFT.csproj
dotnet build DMFT/DMFT.csproj -c Release

```

## Project Structure

- **Solution**: `DMFT.slnx` (not `.sln`)
- **Main app**: `DMFT/` - MAUI Blazor app targeting net10.0-android/ios/maccatalyst/windows

## Dependencies

- .NET 10.0 SDK + MAUI workload
- **yt-dlp**: Required in `./yt-dlp` folder (see `DMFT/Model/YtDlpConfig.cs` for path resolution)

## Entry Points

- `DMFT/MauiProgram.cs` - App startup
- `DMFT/Components/Pages/Main.razor` - Main page (add URLs, download)
- `DMFT/Components/Pages/History.razor` - Download history

## Models & Services

- `DMFT/Model/MainContainer.cs`, `HistoryContainer.cs` - Data persistence
- `DMFT/Model/LinkInfo.cs` - Link entity with DownloadMode, StatusMessage
- `DMFT/Services/TikTok/DownloadEngineAdapter.cs`, `MediaDownloader.cs` - Download orchestration

## Troubleshooting

- ** yt-dlp not found**: Place binary in `./yt-dlp` folder, ensure executable permissions

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **DMFT** (453 symbols, 844 relationships, 22 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> If any GitNexus tool warns the index is stale, run `npx gitnexus analyze` in terminal first.

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `gitnexus_impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `gitnexus_detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `gitnexus_query({query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `gitnexus_context({name: "symbolName"})`.

## Never Do

- NEVER edit a function, class, or method without first running `gitnexus_impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `gitnexus_rename` which understands the call graph.
- NEVER commit changes without running `gitnexus_detect_changes()` to check affected scope.

## Resources

| Resource | Use for |
|----------|---------|
| `gitnexus://repo/DMFT/context` | Codebase overview, check index freshness |
| `gitnexus://repo/DMFT/clusters` | All functional areas |
| `gitnexus://repo/DMFT/processes` | All execution flows |
| `gitnexus://repo/DMFT/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->
