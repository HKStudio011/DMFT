using DMFT.Core.Model;
using Xunit;

namespace DMFT.Tests.Models
{
    public class LinkInfoTests
    {
        [Fact]
        public void LinkInfo_DefaultConstructor_ShouldInitializeProperties()
        {
            var link = new LinkInfo();

            Assert.NotNull(link.Url);
            Assert.Equal(string.Empty, link.Url);
            Assert.Equal(string.Empty, link.VideoId);
            Assert.Equal(string.Empty, link.OriginalUrl);
            Assert.Equal(string.Empty, link.ThumbnailUrl);
            Assert.Equal(string.Empty, link.TitleDescription);
            Assert.Equal(string.Empty, link.SaveLocation);
            Assert.Equal(string.Empty, link.OriginalSoundUrl);
            Assert.Equal(string.Empty, link.OriginalSoundName);
        }

        [Fact]
        public void LinkInfo_Time_ShouldBeSetToNowByDefault()
        {
            var before = DateTime.Now.AddSeconds(-1);
            var link = new LinkInfo();
            var after = DateTime.Now.AddSeconds(1);

            Assert.True(link.Time >= before);
            Assert.True(link.Time <= after);
        }

        [Fact]
        public void LinkInfo_Status_ShouldBeDefaultValue()
        {
            var link = new LinkInfo();
            Assert.Equal(0, link.Status);
        }

        [Fact]
        public void LinkInfo_DownloadMode_ShouldDefaultToVideo()
        {
            var link = new LinkInfo();
            Assert.Equal(DownloadMode.Video, link.DownloadMode);
        }

        [Fact]
        public void LinkInfo_WatermarkPreference_ShouldDefaultToNoPreference()
        {
            var link = new LinkInfo();
            Assert.Equal(WatermarkPreference.NoPreference, link.WatermarkPreference);
        }

        [Fact]
        public void LinkInfo_DownloadFormat_ShouldDefaultToBoth()
        {
            var link = new LinkInfo();
            Assert.Equal(DownloadFormat.Both, link.DownloadFormat);
        }

        [Fact]
        public void LinkInfo_TikTokMetadata_ShouldNotBeNull()
        {
            var link = new LinkInfo();
            Assert.NotNull(link.TikTokMetadata);
        }

        [Fact]
        public void LinkInfo_WithUrl_ShouldStoreCorrectly()
        {
            var link = new LinkInfo
            {
                Url = "https://www.tiktok.com/@user/video/1234567890"
            };

            Assert.Equal("https://www.tiktok.com/@user/video/1234567890", link.Url);
        }

        [Fact]
        public void LinkInfo_WithVideoId_ShouldStoreCorrectly()
        {
            var link = new LinkInfo
            {
                VideoId = "1234567890"
            };

            Assert.Equal("1234567890", link.VideoId);
        }

        [Fact]
        public void LinkInfo_WithSaveLocation_ShouldStoreCorrectly()
        {
            var link = new LinkInfo
            {
                SaveLocation = "/downloads/video123"
            };

            Assert.Equal("/downloads/video123", link.SaveLocation);
        }

        [Fact]
        public void LinkInfo_WithDownloadMode_ShouldStoreCorrectly()
        {
            var link = new LinkInfo
            {
                DownloadMode = DownloadMode.AudioOnly
            };

            Assert.Equal(DownloadMode.AudioOnly, link.DownloadMode);
        }

        [Fact]
        public void LinkInfo_CanChangeStatus()
        {
            var link = new LinkInfo();
            link.Status = StatusMessage.Downloading;

            Assert.Equal(StatusMessage.Downloading, link.Status);

            link.Status = StatusMessage.Success;
            Assert.Equal(StatusMessage.Success, link.Status);
        }

        [Fact]
        public void LinkInfo_CanUpdateTime()
        {
            var link = new LinkInfo();
            var newTime = new DateTime(2025, 1, 1, 12, 0, 0);
            link.Time = newTime;

            Assert.Equal(newTime, link.Time);
        }

        [Fact]
        public void LinkInfo_WithAllProperties_ShouldStoreCorrectly()
        {
            var link = new LinkInfo
            {
                Url = "https://www.tiktok.com/@user/video/1234567890",
                VideoId = "1234567890",
                OriginalUrl = "https://www.tiktok.com/@user/video/1234567890",
                ThumbnailUrl = "https://thumbnail.url/image.jpg",
                TitleDescription = "Test video description",
                SaveLocation = "/downloads/video123",
                OriginalSoundUrl = "https://sound.url/audio.mp3",
                OriginalSoundName = "Original Sound",
                DownloadMode = DownloadMode.VideoAndAudioOrigin,
                Status = StatusMessage.New
            };

            Assert.Equal("https://www.tiktok.com/@user/video/1234567890", link.Url);
            Assert.Equal("1234567890", link.VideoId);
            Assert.Equal("https://www.tiktok.com/@user/video/1234567890", link.OriginalUrl);
            Assert.Equal("https://thumbnail.url/image.jpg", link.ThumbnailUrl);
            Assert.Equal("Test video description", link.TitleDescription);
            Assert.Equal("/downloads/video123", link.SaveLocation);
            Assert.Equal("https://sound.url/audio.mp3", link.OriginalSoundUrl);
            Assert.Equal("Original Sound", link.OriginalSoundName);
            Assert.Equal(DownloadMode.VideoAndAudioOrigin, link.DownloadMode);
            Assert.Equal(StatusMessage.New, link.Status);
        }

        [Fact]
        public void LinkInfo_OriginalSoundNamePriority_ShouldBeSetCorrectly()
        {
            var link = new LinkInfo
            {
                VideoId = "12345",
                OriginalSoundName = "Test Sound Name"
            };

            // This tests that properties are stored correctly which is the pre-requisite 
            // for the naming logic in DownloadEngineAdapter to work
            Assert.Equal("12345", link.VideoId);
            Assert.Equal("Test Sound Name", link.OriginalSoundName);
        }
    }
}
