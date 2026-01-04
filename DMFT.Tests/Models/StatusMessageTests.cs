using DMFT.Core.Model;
using Xunit;

namespace DMFT.Tests.Models
{
    public class StatusMessageTests
    {
        [Fact]
        public void StatusMessage_Success_ShouldBeFour()
        {
            Assert.Equal(4, StatusMessage.Success);
        }

        [Fact]
        public void StatusMessage_New_ShouldBeZero()
        {
            Assert.Equal(0, StatusMessage.New);
        }

        [Fact]
        public void StatusMessage_Waiting_ShouldBeOne()
        {
            Assert.Equal(1, StatusMessage.Waiting);
        }

        [Fact]
        public void StatusMessage_Downloading_ShouldBeTwo()
        {
            Assert.Equal(2, StatusMessage.Downloading);
        }

        [Fact]
        public void StatusMessage_Canceled_ShouldBeThree()
        {
            Assert.Equal(3, StatusMessage.Canceled);
        }

        [Fact]
        public void StatusMessage_Error_ShouldBeNinetyNine()
        {
            Assert.Equal(99, StatusMessage.Error);
        }

        [Fact]
        public void StatusMessage_ErrorValues_ShouldBeGreaterThanRegularError()
        {
            Assert.True(StatusMessage.Error < StatusMessage.VideoAudioOriginError);
            Assert.True(StatusMessage.Error < StatusMessage.VideoError);
            Assert.True(StatusMessage.Error < StatusMessage.AudioOriginError);
            Assert.True(StatusMessage.Error < StatusMessage.AudioOnlyError);
        }

        [Fact]
        public void StatusMessage_IsError_ShouldReturnTrueForErrorStates()
        {
            Assert.True(StatusMessage.Error >= StatusMessage.Error);
            Assert.True(StatusMessage.VideoError >= StatusMessage.Error);
            Assert.True(StatusMessage.AudioOnlyError >= StatusMessage.Error);
            Assert.True(StatusMessage.AudioOriginError >= StatusMessage.Error);
            Assert.True(StatusMessage.VideoAudioOriginError >= StatusMessage.Error);
        }

        [Fact]
        public void StatusMessage_IsError_ShouldReturnFalseForNonErrorStates()
        {
            Assert.False(StatusMessage.Success >= StatusMessage.Error);
            Assert.False(StatusMessage.New >= StatusMessage.Error);
            Assert.False(StatusMessage.Waiting >= StatusMessage.Error);
            Assert.False(StatusMessage.Downloading >= StatusMessage.Error);
            Assert.False(StatusMessage.Canceled >= StatusMessage.Error);
        }

        [Fact]
        public void StatusMessage_ErrorValues_ShouldBeNegativeOrHighPositive()
        {
            Assert.Equal(99, StatusMessage.Error);
            Assert.True(StatusMessage.VideoError > StatusMessage.Error);
            Assert.True(StatusMessage.AudioOnlyError > StatusMessage.Error);
            Assert.True(StatusMessage.AudioOriginError > StatusMessage.Error);
            Assert.True(StatusMessage.VideoAudioOriginError > StatusMessage.Error);
        }
    }
}
