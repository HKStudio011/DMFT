using DMFT.Core.Model;
using Xunit;

namespace DMFT.Tests.Models
{
    public class TikTokTypesTests
    {
        [Fact]
        public void WatermarkPreference_ShouldHaveThreeValues()
        {
            var values = Enum.GetValues<WatermarkPreference>();
            Assert.Equal(3, values.Length);
        }

        [Fact]
        public void WatermarkPreference_Watermarked_ShouldBeFirst()
        {
            Assert.Equal(WatermarkPreference.Watermarked, Enum.GetValues<WatermarkPreference>()[0]);
        }

        [Fact]
        public void DownloadFormat_ShouldHaveThreeValues()
        {
            var values = Enum.GetValues<DownloadFormat>();
            Assert.Equal(3, values.Length);
        }

        [Fact]
        public void TikTokVideoInfo_DefaultConstructor_ShouldInitializeProperties()
        {
            var info = new TikTokVideoInfo();

            Assert.NotNull(info.VideoId);
            Assert.Equal(string.Empty, info.VideoId);
            Assert.Equal(string.Empty, info.Author);
            Assert.Equal(string.Empty, info.Title);
            Assert.Equal(string.Empty, info.MusicTitle);
            Assert.NotNull(info.QualityOptions);
            Assert.Empty(info.QualityOptions);
        }

        [Fact]
        public void TikTokVideoInfo_CanSetProperties()
        {
            var info = new TikTokVideoInfo
            {
                VideoId = "123456",
                Author = "testuser",
                Title = "Test Video",
                DurationSeconds = 30,
                MusicTitle = "Test Music",
                HasWatermark = true
            };

            Assert.Equal("123456", info.VideoId);
            Assert.Equal("testuser", info.Author);
            Assert.Equal("Test Video", info.Title);
            Assert.Equal(30, info.DurationSeconds);
            Assert.Equal("Test Music", info.MusicTitle);
            Assert.True(info.HasWatermark);
        }

        [Fact]
        public void TikTokVideoInfo_CanStoreQualityOptions()
        {
            var info = new TikTokVideoInfo();
            info.QualityOptions.Add("720p");
            info.QualityOptions.Add("1080p");
            info.QualityOptions.Add("4k");

            Assert.Equal(3, info.QualityOptions.Count);
            Assert.Contains("720p", info.QualityOptions);
            Assert.Contains("1080p", info.QualityOptions);
            Assert.Contains("4k", info.QualityOptions);
        }
    }
}
