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
}
