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
        var release = new ReleaseInfo("v1.1.0", "", null, []);
        var service = new AppUpdateService(new Mock<HttpClient>().Object);

        var result = service.IsUpdateAvailable(release, "1.0.0");

        Assert.True(result);
    }

    [Fact]
    public void IsUpdateAvailable_SameVersion_ReturnsFalse()
    {
        var release = new ReleaseInfo("v1.0.0", "", null, []);
        var service = new AppUpdateService(new Mock<HttpClient>().Object);

        var result = service.IsUpdateAvailable(release, "1.0.0");

        Assert.False(result);
    }

    [Fact]
    public void IsUpdateAvailable_OlderVersion_ReturnsFalse()
    {
        var release = new ReleaseInfo("v0.9.0", "", null, []);
        var service = new AppUpdateService(new Mock<HttpClient>().Object);

        var result = service.IsUpdateAvailable(release, "1.0.0");

        Assert.False(result);
    }

    [Fact]
    public void IsUpdateAvailable_VersionWithoutVPrefix_HandlesCorrectly()
    {
        var release = new ReleaseInfo("1.2.0", "", null, []);
        var service = new AppUpdateService(new Mock<HttpClient>().Object);

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
        var release = new ReleaseInfo("v2.0.0",
            "https://github.com/owner/dmft/releases/v2.0.0", "Release body", []);
        var json = JsonSerializer.Serialize(release);
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        var http = new HttpClient(handlerMock.Object);
        var service = new AppUpdateService(http);

        var result = await service.CheckForUpdatesAsync("1.0.0");

        Assert.NotNull(result);
        Assert.Equal("v2.0.0", result.TagName);
    }

    [Fact]
    public async Task DownloadReleaseAsync_NoMatchingAsset_ReturnsNull()
    {
        var release = new ReleaseInfo("v1.0.0", "", null, []);
        var service = new AppUpdateService(new Mock<HttpClient>().Object);

        var result = await service.DownloadReleaseAsync(release, "dest");

        Assert.Null(result);
    }

    [Fact]
    public async Task DownloadReleaseAsync_FindsWinZipAsset_ReturnsPath()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([0x50, 0x4B, 0x05, 0x06]) // empty zip marker
            });
        var http = new HttpClient(handlerMock.Object);
        var service = new AppUpdateService(http);
        var destDir = Path.Combine(Path.GetTempPath(), $"DMFT_Test_{Guid.NewGuid()}");
        var release = new ReleaseInfo("v1.0.0", "", null,
        [
            new ReleaseAsset("DMFT-win-x64.zip", "https://example.com/dmft.zip"),
            new ReleaseAsset("DMFT-linux.tar.gz", "https://example.com/dmft.tar.gz")
        ]);

        try
        {
            var result = await service.DownloadReleaseAsync(release, destDir);

            Assert.NotNull(result);
            Assert.EndsWith("DMFT-win-x64.zip", result);
            Assert.True(File.Exists(result));
        }
        finally
        {
            if (Directory.Exists(destDir))
                Directory.Delete(destDir, recursive: true);
        }
    }
}
