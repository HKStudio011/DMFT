# DMFT Constitution

## Core Principles

### I. Code Quality — Clean, Idiomatic, Maintainable

Every contribution must leave the codebase cleaner than it was found.

- **No dead code**: Unused methods, parameters, or variables must be removed. If it compiles without a reference, delete it.
- **No comments in implementation code**: Use expressive naming and small methods instead of comments. XML doc comments on public APIs are the sole exception.
- **Consistent style**: Follow the existing C# conventions — file-scoped namespaces, primary constructors where appropriate, `var` when type is obvious, pattern matching over casts.
- **Small surfaces**: Prefer many small single-responsibility methods over one large method. If a method exceeds 30 lines, extract.
- **Interface-first**: All public capabilities must be behind an interface (`IMediaDownloader`, `IDownloadEngine`, `IVideoLinkParser`, etc.) to enable testability and future swapping.
- **No `null!` suppressions**: If a value can be null, make the type nullable and handle it. Suppressions are only acceptable in test code for deliberate null-argument tests.

### II. Testing Standards — Unit-Test Everything Non-Trivial (NON-NEGOTIABLE)

Every service, engine, and parser must have a corresponding test class in the matching test project.

- **Cover new code**: Every new method or significant logic path must have at least one fact/theory proving it works and one proving it handles failure.
- **Framework**: xUnit (`[Fact]`, `[Theory]`) + Moq for all unit tests. In-memory EF Core (`UseInMemoryDatabase`) for data-layer tests — never mock `DbContext`.
- **Isolation**: Each test must create its own in-memory database (unique name via `Guid.NewGuid()`) — no shared state between tests.
- **Naming convention**: `{MethodName}_{Scenario}_{ExpectedResult}` — e.g. `StartDownloadAsync_VideoOnly_CallsMediaDownloader`.
- **No test side effects**: Tests must not write to disk, make network calls, or depend on environment. yt-dlp calls must be mocked.
- **Test structure**: Arrange-Act-Assert with blank-line separation. One assertion per logical concern (use multiple facts over one giant assert).
- **Error status codes must be tested**: Every error branch in `DownloadEngine` (`VideoError`, `AudioOriginError`, `VideoAudioOriginError`, `AudioOnlyError`) must have at least one test.
- **Browser/Web tests**: Use Playwright for UI integration tests (`DMFT.Test.Web/`). Page-level tests navigate, interact, and assert DOM state.

### III. User Experience Consistency — Predictable, Responsive, Informative

The app targets a technical user doing a focused task — downloading media. Every interaction must feel snappy and predictable.

- **Status transparency**: Every download item must show its exact status (New, Waiting, Downloading, Completed, Error variant) at all times with no ambiguity.
- **Progress feedback**: The progress bar must update every 5 seconds on the UI. When progress is 0% or 100%, the bar must be hidden.
- **Error granularity**: Error codes must distinguish which media type failed (Video vs Audio vs Origin Audio) so the user can retry selectively. Never show a generic "Error".
- **Platform badges**: Each URL row must display its detected platform (YouTube, TikTok, etc.) as a visible badge — never hide it.
- **Batch actions**: "Apply to All" must respect the three toggles (Video, Audio, Origin Audio) and apply them atomically to every item in the current list.
- **Toast feedback**: All user-initiated actions (add URLs, start download, remove item) must produce a toast with success/info/error level. Toasts auto-dismiss.
- **Loading state**: Long-running operations must show `LoadingModal`. Modal must not block the UI from polling progress updates.
- **Consistent layout**: Card-based layout per download item. Use Material/System colors from the MAUI theme (`--surface`, `--primary`, `--error`, etc.) — never hardcode colors.

### IV. Performance Requirements — No Blocking, No Busy-Waiting

Download management is I/O-bound. The UI must never freeze.

- **Async everywhere**: All database, process, and network I/O must be `async`/`await`. No `.Result`, no `.Wait()`, no `Task.WaitAll`.
- **Progress polling**: Use a `Timer` (not a loop) with 5-second interval to refresh download list. Dispose the timer in `Dispose()`.
- **Bound yt-dlp**: All yt-dlp processes must be cancellable via `CancellationToken` or `Process.Kill`. Never leave orphan processes.
- **Progress persistence**: DownloadEngine writes progress to SQLite every 500ms. This is the only hot write path — keep it light (no joins, no complex queries in progress handler).
- **No eager loading**: Use `ToListAsync()` only after applying filters. Streaming data that grows unbounded is forbidden.
- **Startup**: DB migration + initialization must complete within 5 seconds. If it fails, the app must still start (degraded mode) — log the error and continue.

### V. Security — No Secrets, No Injection, No Escape

The app processes user-supplied URLs and shell commands. This is the highest-risk surface.

- **No secret storage**: API keys, tokens, or credentials must never be committed. Use `IYtDlpConfigProvider` for configurable settings — they live in the SQLite DB, not in source.
- **URL validation**: `VideoLinkParser` must reject any URL that doesn't match a known platform pattern before it reaches yt-dlp. This is the primary injection defense.
- **Process argument safety**: All user-supplied values (URLs, video IDs) passed to yt-dlp must be double-quoted in the argument string. Never use string interpolation without quoting.
- **Output path validation**: `SaveLocation` must be constrained to the user's chosen download directory. Path traversal (`../`, `..\\`) must be rejected.
- **EF Core safety**: Use parameterized queries exclusively (LINQ over DbSet). Never use `FromSqlRaw` or `ExecuteSqlRaw`.
- **Dependency integrity**: yt-dlp binary is resolved via `IYtDlpConfigProvider` from a configurable path (default `./yt-dlp/`). The app must not download or execute binaries automatically.
- **Playwright isolation**: TikTok sound extraction runs in a separate browser context. Context must be disposed after extraction.

## Governance

- This constitution supersedes all other development guidelines. Any conflict must be resolved by updating this document.
- All PRs must be reviewed against these five principles. A reviewer may block a PR for violating any principle.
- Amendments require a documented proposal, team approval, and an update to the version line below.
- Complexity must be justified. If a change requires more than 3 new files or 200 lines, it must be preceded by a brief design note.
- New external dependencies require justification in the PR body.

**Version**: 1.0.0 | **Ratified**: 2026-07-16 | **Last Amended**: 2026-07-16
