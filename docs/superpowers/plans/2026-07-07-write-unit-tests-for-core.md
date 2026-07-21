# Write Unit Tests for Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Achieve 70%+ line coverage on `DMFT.Core` by writing focused unit tests for entities, parsers, services, and error-handling logic.

**Architecture:** The test project `DMFT.Test.Core` currently references only `DMFT.Shared`. We add a project reference to `DMFT.Core` so we can test business logic directly. Services with external dependencies (HTTP, Playwright, EF Core, process execution) are tested via interface mocking with Moq. Pure logic (entities, parsers, enums) gets straightforward Fact/Theory tests.

**Tech Stack:** xUnit v3.2.2, Moq 4.20.72, coverlet 10.0.1, Microsoft.Test.Sdk 18.7.0

## Global Constraints

- All test code in `DMFT.Test.Core/`
- Tests use xUnit `[Fact]` / `[Theory]` — no other frameworks
- Mock external dependencies with Moq — no integration tests against real DB/HTTP/process
- Each test class mirrors the source namespace: `namespace DMFT.Test.Core.Services;` for `DMFT.Core.Services.*`
- No test side-effects: no file I/O, no network calls
- Follow arrange-act-assert pattern with blank line separation
- Test names: `{MethodName}_{Scenario}_Returns{Expected}` (e.g. `ParseUrl_TikTokMobileUrl_ReturnsVideoId`)
- `dotnet test DMFT.Test.Core/DMFT.Test.Core.csproj` must pass after each task

---
## File Structure

| File | Responsibility |
|------|---------------|
| `DMFT.Test.Core/DMFT.Test.Core.csproj` | Add `<ProjectReference Include="..\DMFT.Core\DMFT.Core.csproj" />` |
| `DMFT.Test.Core/Entities/DownloadItemTests.cs` | Bit flag property tests for `DownloadVideo`, `DownloadAudio`, `DownloadOriginAudio` |
| `DMFT.Test.Core/Services/VideoLinkParserTests.cs` | URL parsing, platform detection, video ID extraction |
| `DMFT.Test.Core/Services/DownloadQueueTests.cs` | Enqueue, concurrency guard, event firing, initialization |
| `DMFT.Test.Core/Services/AppUpdateServiceTests.cs` | Version check, version comparison, download release |
| `DMFT.Test.Core/Services/AppSettingsReaderTests.cs` | Settings parsing, error handling when DB fails |

**Deferred to future plan (require complex mocking or Playwright):**
- `DownloadEngineTests.cs` — orchestrates IMediaDownloader + DownloadService + ITikTokSoundExtractor
- `YtDlpServiceTests.cs` — process execution, progress JSON parsing (needs Process abstraction)
- `DownloadServiceTests.cs` — EF Core CRUD (needs in-memory DB or mock)
- `YtDlpConfigProviderTests.cs` — file path resolution, DB init
- `TikTokSoundExtractorTests.cs` — requires Playwright browser

---

### Task 1: Add DMFT.Core reference to Test.Core

**Files:**
- Modify: `DMFT.Test.Core/DMFT.Test.Core.csproj`
- Modify: `DMFT.Test.Core/UnitTest1.cs` (remove template file)

- [ ] **Step 1: Add project reference and InMemory EF Core package**

Edit `DMFT.Test.Core/DMFT.Test.Core.csproj` — add the Core project reference and EF Core InMemory package:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.9" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\DMFT.Core\DMFT.Core.csproj" />
    <ProjectReference Include="..\DMFT\DMFT.Shared\DMFT.Shared.csproj" />
  </ItemGroup>
```

- [ ] **Step 2: Remove template test file**

```bash
del DMFT.Test.Core\UnitTest1.cs
```

- [ ] **Step 3: Verify build**

```bash
dotnet build DMFT.Test.Core/DMFT.Test.Core.csproj -c Release
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Verify test discovery**

```bash
dotnet test DMFT.Test.Core/DMFT.Test.Core.csproj --list-tests
```
Expected: "No test is available" (no tests written yet).

- [ ] **Step 5: Commit**

```bash
git add DMFT.Test.Core/DMFT.Test.Core.csproj DMFT.Test.Core/UnitTest1.cs
git commit -m "chore: add DMFT.Core + EF Core InMemory to Test.Core project"
```

---

### Task 2: DownloadItem bit-flag tests

**Files:**
- Create: `DMFT.Test.Core/Entities/DownloadItemTests.cs`

**Interfaces:**
- Consumes: `DMFT.Core.Entities.DownloadItem`, `DMFT.Core.Services.DownloadMode`
- Produces: Tests for `DownloadVideo`, `DownloadAudio`, `DownloadOriginAudio`, `SetFlag` behavior

- [ ] **Step 1: Create directory and test file**

Create directory `DMFT.Test.Core/Entities/`.

Write `DMFT.Test.Core/Entities/DownloadItemTests.cs`:

```csharp
using DMFT.Core.Entities;
using DMFT.Core.Services;

namespace DMFT.Test.Core.Entities;

public class DownloadItemTests
{
    [Fact]
    public void DownloadVideo_Default_ReturnsFalse()
    {
        var item = new DownloadItem();

        Assert.False(item.DownloadVideo);
    }

    [Fact]
    public void DownloadVideo_SetTrue_SetsDownloadModeBit()
    {
        var item = new DownloadItem();

        item.DownloadVideo = true;

        Assert.True(item.DownloadVideo);
        Assert.Equal((int)DownloadMode.Video, item.DownloadMode & (int)DownloadMode.Video);
    }

    [Fact]
    public void DownloadVideo_SetFalseThenTrue_TogglesCorrectly()
    {
        var item = new DownloadItem();
        item.DownloadVideo = true;

        item.DownloadVideo = false;

        Assert.False(item.DownloadVideo);
        Assert.Equal(0, item.DownloadMode & (int)DownloadMode.Video);
    }

    [Fact]
    public void DownloadAudio_Default_ReturnsFalse()
    {
        var item = new DownloadItem();

        Assert.False(item.DownloadAudio);
    }

    [Fact]
    public void DownloadAudio_SetTrue_SetsDownloadModeBit()
    {
        var item = new DownloadItem();

        item.DownloadAudio = true;

        Assert.True(item.DownloadAudio);
        Assert.Equal((int)DownloadMode.Audio, item.DownloadMode & (int)DownloadMode.Audio);
    }

    [Fact]
    public void DownloadOriginAudio_Default_ReturnsFalse()
    {
        var item = new DownloadItem();

        Assert.False(item.DownloadOriginAudio);
    }

    [Fact]
    public void DownloadOriginAudio_SetTrue_SetsDownloadModeBit()
    {
        var item = new DownloadItem();

        item.DownloadOriginAudio = true;

        Assert.True(item.DownloadOriginAudio);
        Assert.Equal((int)DownloadMode.OriginAudio, item.DownloadMode & (int)DownloadMode.OriginAudio);
    }

    [Fact]
    public void MultipleFlags_SetAll_StoresCombination()
    {
        var item = new DownloadItem();

        item.DownloadVideo = true;
        item.DownloadAudio = true;
        item.DownloadOriginAudio = true;

        Assert.True(item.DownloadVideo);
        Assert.True(item.DownloadAudio);
        Assert.True(item.DownloadOriginAudio);
        Assert.Equal(
            (int)(DownloadMode.Video | DownloadMode.Audio | DownloadMode.OriginAudio),
            item.DownloadMode);
    }

    [Fact]
    public void MultipleFlags_ClearOne_OthersRemain()
    {
        var item = new DownloadItem { DownloadMode = (int)(DownloadMode.Video | DownloadMode.Audio | DownloadMode.OriginAudio) };

        item.DownloadAudio = false;

        Assert.True(item.DownloadVideo);
        Assert.False(item.DownloadAudio);
        Assert.True(item.DownloadOriginAudio);
    }

    [Fact]
    public void DownloadMode_Zero_AllFlagsFalse()
    {
        var item = new DownloadItem { DownloadMode = 0 };

        Assert.False(item.DownloadVideo);
        Assert.False(item.DownloadAudio);
        Assert.False(item.DownloadOriginAudio);
    }

    [Fact]
    public void Id_NewInstance_IsNotEmpty()
    {
        var item = new DownloadItem();

        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Fact]
    public void Time_NewInstance_IsRecentUtc()
    {
        var item = new DownloadItem();

        Assert.True(item.Time <= DateTime.UtcNow);
        Assert.True(item.Time > DateTime.UtcNow.AddMinutes(-1));
    }
}
```

- [ ] **Step 2: Run tests to verify they pass**

```bash
dotnet test DMFT.Test.Core/DMFT.Test.Core.csproj --filter "FullyQualifiedName~DownloadItemTests"
```
Expected: Passed — 10 passed, 0 failed.

- [ ] **Step 3: Commit**

```bash
git add DMFT.Test.Core/Entities/DownloadItemTests.cs
git commit -m "test: add DownloadItem bit-flag property tests"
```

---

### Task 3: VideoLinkParser tests

**Files:**
- Create: `DMFT.Test.Core/Services/VideoLinkParserTests.cs`

**Interfaces:**
- Consumes: `DMFT.Core.Services.VideoLinkParser`, `DMFT.Core.Services.VideoPlatform`, `DMFT.Core.Services.IVideoLinkParser`
- Produces: Full coverage of URL parsing, platform detection, edge cases

- [ ] **Step 1: Write test file**

```csharp
using DMFT.Core.Services;

namespace DMFT.Test.Core.Services;

public class VideoLinkParserTests
{
    private readonly IVideoLinkParser _parser = new VideoLinkParser();

    // --- IsSupportedUrl ---

    [Theory]
    [InlineData("https://www.tiktok.com/@user/video/1234567890")]
    [InlineData("https://vm.tiktok.com/abc123/")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/abc123defgh")]
    [InlineData("https://m.tiktok.com/v/1234567890")]
    public void IsSupportedUrl_ValidUrls_ReturnsTrue(string url)
    {
        var result = _parser.IsSupportedUrl(url);

        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("https://www.facebook.com/watch")]
    [InlineData("https://vimeo.com/12345")]
    [InlineData("not-a-url")]
    public void IsSupportedUrl_InvalidUrls_ReturnsFalse(string url)
    {
        var result = _parser.IsSupportedUrl(url);

        Assert.False(result);
    }

    // --- GetPlatform ---

    [Fact]
    public void GetPlatform_TikTokUrl_ReturnsTikTok()
    {
        var result = _parser.GetPlatform("https://www.tiktok.com/@user/video/123");

        Assert.Equal(VideoPlatform.TikTok, result);
    }

    [Fact]
    public void GetPlatform_YouTubeWatchUrl_ReturnsYouTube()
    {
        var result = _parser.GetPlatform("https://www.youtube.com/watch?v=dQw4w9WgXcQ");

        Assert.Equal(VideoPlatform.YouTube, result);
    }

    [Fact]
    public void GetPlatform_YouTubeShortsUrl_ReturnsYouTubeShorts()
    {
        var result = _parser.GetPlatform("https://www.youtube.com/shorts/abc123defgh");

        Assert.Equal(VideoPlatform.YouTubeShorts, result);
    }

    [Fact]
    public void GetPlatform_YouTuBeUrl_ReturnsYouTubeShorts()
    {
        var result = _parser.GetPlatform("https://youtu.be/dQw4w9WgXcQ");

        Assert.Equal(VideoPlatform.YouTubeShorts, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://facebook.com")]
    [InlineData(null)]
    public void GetPlatform_UnknownUrl_ReturnsUnknown(string url)
    {
        var result = _parser.GetPlatform(url);

        Assert.Equal(VideoPlatform.Unknown, result);
    }

    // --- GetPlatformLabel ---

    [Fact]
    public void GetPlatformLabel_TikTok_ReturnsTikTok()
    {
        var result = _parser.GetPlatformLabel(VideoPlatform.TikTok);

        Assert.Equal("TikTok", result);
    }

    [Fact]
    public void GetPlatformLabel_YouTube_ReturnsYouTube()
    {
        var result = _parser.GetPlatformLabel(VideoPlatform.YouTube);

        Assert.Equal("YouTube", result);
    }

    [Fact]
    public void GetPlatformLabel_YouTubeShorts_ReturnsYouTubeShorts()
    {
        var result = _parser.GetPlatformLabel(VideoPlatform.YouTubeShorts);

        Assert.Equal("YouTube Shorts", result);
    }

    [Fact]
    public void GetPlatformLabel_Unknown_ReturnsUnknown()
    {
        var result = _parser.GetPlatformLabel(VideoPlatform.Unknown);

        Assert.Equal("Unknown", result);
    }

    // --- TryParseVideoId ---

    [Fact]
    public void TryParseVideoId_TikTokVideoUrl_ExtractsVideoId()
    {
        var result = _parser.TryParseVideoId("https://www.tiktok.com/@user/video/1234567890", out var videoId);

        Assert.True(result);
        Assert.Equal("1234567890", videoId);
    }

    [Fact]
    public void TryParseVideoId_YouTubeWatchUrl_ExtractsVideoId()
    {
        var result = _parser.TryParseVideoId("https://www.youtube.com/watch?v=dQw4w9WgXcQ", out var videoId);

        Assert.True(result);
        Assert.Equal("dQw4w9WgXcQ", videoId);
    }

    [Fact]
    public void TryParseVideoId_YouTubeShortsUrl_ExtractsVideoId()
    {
        var result = _parser.TryParseVideoId("https://www.youtube.com/shorts/abc123defgh", out var videoId);

        Assert.True(result);
        Assert.Equal("abc123defgh", videoId);
    }

    [Fact]
    public void TryParseVideoId_YouTuBeUrl_ExtractsVideoId()
    {
        var result = _parser.TryParseVideoId("https://youtu.be/dQw4w9WgXcQ", out var videoId);

        Assert.True(result);
        Assert.Equal("dQw4w9WgXcQ", videoId);
    }

    [Fact]
    public void TryParseVideoId_TikTokPhotoUrl_ExtractsPhotoId()
    {
        var result = _parser.TryParseVideoId("https://www.tiktok.com/@user/photo/9876543210", out var videoId);

        Assert.True(result);
        Assert.Equal("9876543210", videoId);
    }

    [Fact]
    public void TryParseVideoId_UnsupportedUrl_ReturnsFalse()
    {
        var result = _parser.TryParseVideoId("https://facebook.com/watch", out var videoId);

        Assert.False(result);
        Assert.Null(videoId);
    }

    [Fact]
    public void TryParseVideoId_NullUrl_ReturnsFalse()
    {
        var result = _parser.TryParseVideoId(null!, out var videoId);

        Assert.False(result);
        Assert.Null(videoId);
    }

    [Fact]
    public void TryParseVideoId_EmptyUrl_ReturnsFalse()
    {
        var result = _parser.TryParseVideoId("", out var videoId);

        Assert.False(result);
        Assert.Null(videoId);
    }

    [Fact]
    public void TryParseVideoId_TikTokMobileUrl_ExtractsVideoId()
    {
        var result = _parser.TryParseVideoId("https://m.tiktok.com/v/1234567890", out var videoId);

        Assert.True(result);
        Assert.Equal("1234567890", videoId);
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test DMFT.Test.Core/DMFT.Test.Core.csproj --filter "FullyQualifiedName~VideoLinkParserTests"
```
Expected: Passed — 20 passed, 0 failed.

- [ ] **Step 3: Commit**

```bash
git add DMFT.Test.Core/Services/VideoLinkParserTests.cs
git commit -m "test: add VideoLinkParser URL parsing and platform detection tests"
```

---

### Task 4: DownloadQueue tests

**Files:**
- Create: `DMFT.Test.Core/Services/DownloadQueueTests.cs`

**Interfaces:**
- Consumes: `DMFT.Core.Services.DownloadQueue`, `DMFT.Core.Services.IDownloadEngine`, `DMFT.Core.Services.DownloadService`, `DMFT.Core.Entities.DownloadItem`, `DMFT.Core.Services.StatusCodes`
- Produces: Tests for enqueue behavior, concurrency guard, event firing

- [ ] **Step 1: Write test file**

```csharp
using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using Moq;

namespace DMFT.Test.Core.Services
{
    public class DownloadQueueTests
    {
        private readonly Mock<IDownloadEngine> _engineMock;
        private readonly Mock<DownloadService> _serviceMock;
        private readonly DownloadQueue _queue;

        public DownloadQueueTests()
        {
            _engineMock = new Mock<IDownloadEngine>();
            _serviceMock = new Mock<DownloadService>(Mock.Of<IDbContextFactory<AppDbContext>>());
            _queue = new DownloadQueue(_engineMock.Object, _serviceMock.Object);
        }

    [Fact]
    public void MaxConcurrent_Default_ReturnsOne()
    {
        Assert.Equal(1, _queue.MaxConcurrent);
    }

    [Fact]
    public void MaxConcurrent_SetBelowOne_ClampsToOne()
    {
        _queue.MaxConcurrent = -5;

        Assert.Equal(1, _queue.MaxConcurrent);
    }

    [Fact]
    public void DelayBetweenMs_Default_Returns2000()
    {
        Assert.Equal(2000, _queue.DelayBetweenMs);
    }

    [Fact]
    public void DelayBetweenMs_SetBelow500_ClampsTo500()
    {
        _queue.DelayBetweenMs = 100;

        Assert.Equal(500, _queue.DelayBetweenMs);
    }

    [Fact]
    public void IsProcessing_Initially_ReturnsFalse()
    {
        Assert.False(_queue.IsProcessing);
    }

    [Fact]
    public void EnqueueDownloadAsync_NullItem_DoesNotThrow()
    {
        var ex = Record.Exception(() => _queue.EnqueueDownloadAsync(null!));

        Assert.Null(ex);
    }

    [Fact]
    public async Task EnqueueDownloadAsync_ValidItem_SetsStatusWaiting()
    {
        var item = new DownloadItem();

        await _queue.EnqueueDownloadAsync(item);

        Assert.Equal(StatusCodes.Waiting, item.Status);
    }

    [Fact]
    public async Task EnqueueDownloadAsync_ValidItem_FiresOnQueueUpdated()
    {
        var fired = false;
        _queue.OnQueueUpdated += () => fired = true;
        var item = new DownloadItem();

        await _queue.EnqueueDownloadAsync(item);

        Assert.True(fired);
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test DMFT.Test.Core/DMFT.Test.Core.csproj --filter "FullyQualifiedName~DownloadQueueTests"
```
Expected: Passed — 8 passed, 0 failed.

- [ ] **Step 3: Commit**

```bash
git add DMFT.Test.Core/Services/DownloadQueueTests.cs
git commit -m "test: add DownloadQueue enqueue and configuration tests"
```

---

### Task 5: AppUpdateService tests

**Files:**
- Create: `DMFT.Test.Core/Services/AppUpdateServiceTests.cs`

**Interfaces:**
- Consumes: `DMFT.Core.Services.AppUpdateService`, `DMFT.Core.Services.ReleaseInfo`, `DMFT.Core.Services.ReleaseAsset`
- Produces: Tests for version comparison, HTTP-based update check (mocked), download

- [ ] **Step 1: Write test file**

```csharp
using System.Net;
using System.Text.Json;
using DMFT.Core.Services;
using Moq;
using Moq.Protected;

namespace DMFT.Test.Core.Services;

public class AppUpdateServiceTests
{
    [Fact]
    public void IsUpdateAvailable_NewerVersion_ReturnsTrue()
    {
        var httpMock = new Mock<HttpClient>();
        var service = new AppUpdateService(httpMock.Object);
        var release = new ReleaseInfo("v1.1.0", "", null, []);

        var result = service.IsUpdateAvailable(release, "1.0.0");

        Assert.True(result);
    }

    [Fact]
    public void IsUpdateAvailable_SameVersion_ReturnsFalse()
    {
        var httpMock = new Mock<HttpClient>();
        var service = new AppUpdateService(httpMock.Object);
        var release = new ReleaseInfo("v1.0.0", "", null, []);

        var result = service.IsUpdateAvailable(release, "1.0.0");

        Assert.False(result);
    }

    [Fact]
    public void IsUpdateAvailable_OlderVersion_ReturnsFalse()
    {
        var httpMock = new Mock<HttpClient>();
        var service = new AppUpdateService(httpMock.Object);
        var release = new ReleaseInfo("v0.9.0", "", null, []);

        var result = service.IsUpdateAvailable(release, "1.0.0");

        Assert.False(result);
    }

    [Fact]
    public void IsUpdateAvailable_VersionWithoutVPrefix_HandlesCorrectly()
    {
        var httpMock = new Mock<HttpClient>();
        var service = new AppUpdateService(httpMock.Object);
        var release = new ReleaseInfo("1.2.0", "", null, []);

        var result = service.IsUpdateAvailable(release, "1.1.0");

        Assert.True(result);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_HttpError_ReturnsNull()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var http = new HttpClient(handlerMock.Object);
        var service = new AppUpdateService(http);

        var result = await service.CheckForUpdatesAsync("1.0.0");

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_SuccessResponse_ReturnsReleaseInfo()
    {
        var release = new ReleaseInfo("v2.0.0", "https://github.com/owner/dmft/releases/v2.0.0", "Release body", []);
        var json = JsonSerializer.Serialize(release);
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        var http = new HttpClient(handlerMock.Object);
        var service = new AppUpdateService(http);

        var result = await service.CheckForUpdatesAsync("1.0.0");

        Assert.NotNull(result);
        Assert.Equal("v2.0.0", result.TagName);
    }

    [Fact]
    public async Task DownloadReleaseAsync_NoMatchingAsset_ReturnsNull()
    {
        var handlerMock = new Mock<HttpClient>();
        var service = new AppUpdateService(handlerMock.Object);
        var release = new ReleaseInfo("v1.0.0", "", null, []);

        var result = await service.DownloadReleaseAsync(release, "dest");

        Assert.Null(result);
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test DMFT.Test.Core/DMFT.Test.Core.csproj --filter "FullyQualifiedName~AppUpdateServiceTests"
```
Expected: Passed — 7 passed, 0 failed.

- [ ] **Step 3: Commit**

```bash
git add DMFT.Test.Core/Services/AppUpdateServiceTests.cs
git commit -m "test: add AppUpdateService version comparison and HTTP tests"
```

---

### Task 6: AppSettingsReader tests

**Files:**
- Create: `DMFT.Test.Core/Services/AppSettingsReaderTests.cs`

**Interfaces:**
- Consumes: `DMFT.Core.Services.AppSettingsReader`, `DMFT.Core.Data.AppDbContext`
- Produces: Tests for settings parsing with mocked DbContext

- [ ] **Step 1: Write test file**

```csharp
using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DMFT.Test.Core.Services;

public class AppSettingsReaderTests
{
    [Fact]
    public async Task ReadYtDlpConfigAsync_SettingsExist_ReturnsValues()
    {
        using var context = CreateDbContextWithSettings();
        var dbFactory = CreateDbFactory(context);

        var (extraArgs, outputTemplate, formatString) = await AppSettingsReader.ReadYtDlpConfigAsync(dbFactory.Object);

        Assert.Equal("--no-warnings", extraArgs);
        Assert.Equal("%(title)s.%(ext)s", outputTemplate);
        Assert.Equal("bestvideo+bestaudio", formatString);
    }

    [Fact]
    public async Task ReadYtDlpConfigAsync_NoSettings_ReturnsNulls()
    {
        using var context = CreateEmptyDbContext();
        var dbFactory = CreateDbFactory(context);

        var (extraArgs, outputTemplate, formatString) = await AppSettingsReader.ReadYtDlpConfigAsync(dbFactory.Object);

        Assert.Null(extraArgs);
        Assert.Null(outputTemplate);
        Assert.Null(formatString);
    }

    [Fact]
    public async Task ReadQueueSettingsAsync_SettingsExist_ReturnsValues()
    {
        using var context = CreateDbContextWithSettings();
        var dbFactory = CreateDbFactory(context);

        var (maxConcurrent, delayBetweenMs) = await AppSettingsReader.ReadQueueSettingsAsync(dbFactory.Object);

        Assert.Equal(3, maxConcurrent);
        Assert.Equal(5000, delayBetweenMs);
    }

    [Fact]
    public async Task ReadQueueSettingsAsync_NoSettings_ReturnsNulls()
    {
        using var context = CreateEmptyDbContext();
        var dbFactory = CreateDbFactory(context);

        var (maxConcurrent, delayBetweenMs) = await AppSettingsReader.ReadQueueSettingsAsync(dbFactory.Object);

        Assert.Null(maxConcurrent);
        Assert.Null(delayBetweenMs);
    }

    private static AppDbContext CreateEmptyDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static AppDbContext CreateDbContextWithSettings()
    {
        var ctx = CreateEmptyDbContext();
        ctx.AppSettings.AddRange(
            new AppSetting { Id = "ytdlp_extra_args", Value = "--no-warnings" },
            new AppSetting { Id = "ytdlp_output_template", Value = "%(title)s.%(ext)s" },
            new AppSetting { Id = "ytdlp_format", Value = "bestvideo+bestaudio" },
            new AppSetting { Id = "maxConcurrent", Value = "3" },
            new AppSetting { Id = "delayBetweenMs", Value = "5000" }
        );
        ctx.SaveChanges();
        return ctx;
    }

    private static Mock<IDbContextFactory<AppDbContext>> CreateDbFactory(AppDbContext context)
    {
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);
        return factory;
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test DMFT.Test.Core/DMFT.Test.Core.csproj --filter "FullyQualifiedName~AppSettingsReaderTests"
```
Expected: Passed — 4 passed, 0 failed.

- [ ] **Step 3: Commit**

```bash
git add DMFT.Test.Core/Services/AppSettingsReaderTests.cs
git commit -m "test: add AppSettingsReader settings parsing tests"
```

---

### Task 7: Run full test suite and verify coverage

- [ ] **Step 1: Run all tests**

```bash
dotnet test DMFT.Test.Core/DMFT.Test.Core.csproj -c Release --collect:"XPlat Code Coverage" --results-directory TestResults
```

Expected: All 49 tests pass, 0 failed.

- [ ] **Step 2: Generate coverage report**

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool --ignore-failed-sources
reportgenerator "-reports:TestResults/**/coverage.cobertura.xml" "-targetdir:TestResults/CoverageReport" -reporttypes:Html
```

Or if reportgenerator is unavailable, inspect the raw coverage XML:
```bash
Get-ChildItem TestResults -Recurse -Filter *.xml | Select-Object -First 1 | ForEach-Object { Get-Content $_.FullName -Head 50 }
```

- [ ] **Step 3: Commit final coverage pass**

```bash
git add -A && git commit -m "test: add AppSettingsReader with in-memory DB tests and finalize coverage"
```

- [ ] **Step 4: Print summary**

```bash
Write-Host "=== Coverage Summary ==="
Get-ChildItem TestResults -Recurse -Filter *.xml | Select-Object -First 1 | ForEach-Object {
    [xml]$xml = Get-Content $_.FullName
    $packages = $xml.CoverageSession.Summary
    Write-Host "Line coverage: $($packages.LineCoverage) ($($packages.LineCovered)/$($packages.LineTotal))"
    Write-Host "Branch coverage: $($packages.BranchCoverage)%"
}
```
