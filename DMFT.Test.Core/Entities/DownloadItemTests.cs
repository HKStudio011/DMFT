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
