using DMFT.Core.Entities;
using DMFT.Core.Services;

namespace DMFT.Test.Core.Entities;

public class DownloadItemTests
{
    [Fact]
    public void DownloadVideo_Default_ReturnsFalse()
    {
        var item = new DownloadItem();

        var result = item.DownloadVideo;

        Assert.False(result);
    }

    [Fact]
    public void DownloadVideo_SetTrue_SetsDownloadModeBit()
    {
        var item = new DownloadItem();
        item.DownloadVideo = true;

        var result = item.DownloadVideo;
        var mode = item.DownloadMode & (int)DownloadMode.Video;

        Assert.True(result);
        Assert.Equal((int)DownloadMode.Video, mode);
    }

    [Fact]
    public void DownloadVideo_ClearAfterSet_ClearsBit()
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

        var result = item.DownloadAudio;

        Assert.False(result);
    }

    [Fact]
    public void DownloadAudio_SetTrue_SetsDownloadModeBit()
    {
        var item = new DownloadItem();
        item.DownloadAudio = true;

        var result = item.DownloadAudio;
        var mode = item.DownloadMode & (int)DownloadMode.Audio;

        Assert.True(result);
        Assert.Equal((int)DownloadMode.Audio, mode);
    }

    [Fact]
    public void DownloadOriginAudio_Default_ReturnsFalse()
    {
        var item = new DownloadItem();

        var result = item.DownloadOriginAudio;

        Assert.False(result);
    }

    [Fact]
    public void DownloadOriginAudio_SetTrue_SetsDownloadModeBit()
    {
        var item = new DownloadItem();
        item.DownloadOriginAudio = true;

        var result = item.DownloadOriginAudio;
        var mode = item.DownloadMode & (int)DownloadMode.OriginAudio;

        Assert.True(result);
        Assert.Equal((int)DownloadMode.OriginAudio, mode);
    }

    [Fact]
    public void MultipleFlags_SetAll_StoresCombination()
    {
        var item = new DownloadItem();
        item.DownloadVideo = true;
        item.DownloadAudio = true;
        item.DownloadOriginAudio = true;

        var result = item.DownloadMode;

        Assert.True(item.DownloadVideo);
        Assert.True(item.DownloadAudio);
        Assert.True(item.DownloadOriginAudio);
        Assert.Equal(
            (int)(DownloadMode.Video | DownloadMode.Audio | DownloadMode.OriginAudio),
            result);
    }

    [Fact]
    public void MultipleFlags_ClearOne_OthersRemain()
    {
        var item = new DownloadItem
        {
            DownloadMode = (int)(DownloadMode.Video | DownloadMode.Audio | DownloadMode.OriginAudio)
        };

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

        var id = item.Id;

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public void Time_NewInstance_IsRecentUtc()
    {
        var item = new DownloadItem();
        var before = DateTime.UtcNow.AddMinutes(-1);

        var time = item.Time;

        Assert.True(time <= DateTime.UtcNow);
        Assert.True(time > before);
    }
}
