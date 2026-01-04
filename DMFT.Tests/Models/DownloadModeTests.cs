using DMFT.Core.Model;
using Xunit;

namespace DMFT.Tests.Models
{
    public class DownloadModeTests
    {
        [Fact]
        public void DownloadMode_ShouldHaveFourValues()
        {
            var values = Enum.GetValues<DownloadMode>();
            Assert.Equal(4, values.Length);
        }

        [Theory]
        [InlineData(DownloadMode.Video)]
        [InlineData(DownloadMode.AudioOnly)]
        [InlineData(DownloadMode.AudioOriginOnly)]
        [InlineData(DownloadMode.VideoAndAudioOrigin)]
        public void DownloadMode_AllValues_ShouldBeParsable(DownloadMode mode)
        {
            var parsed = Enum.Parse<DownloadMode>(mode.ToString());
            Assert.Equal(mode, parsed);
        }

        [Fact]
        public void DownloadMode_Video_ShouldHaveCorrectValue()
        {
            Assert.Equal(0, (int)DownloadMode.Video);
        }

        [Fact]
        public void DownloadMode_AudioOnly_ShouldHaveCorrectValue()
        {
            Assert.Equal(1, (int)DownloadMode.AudioOnly);
        }

        [Fact]
        public void DownloadMode_AudioOriginOnly_ShouldHaveCorrectValue()
        {
            Assert.Equal(2, (int)DownloadMode.AudioOriginOnly);
        }

        [Fact]
        public void DownloadMode_VideoAndAudioOrigin_ShouldHaveCorrectValue()
        {
            Assert.Equal(3, (int)DownloadMode.VideoAndAudioOrigin);
        }
    }
}
