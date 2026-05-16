using DMFT.Model;
using System.Threading.Tasks;

namespace DMFT.Services
{
    public interface ITikTokDownloaderService
    {
        Task<DMFT.Model.LinkInfo?> PrepareDownloadAsync(string url);
    }

    public class TikTokDownloaderService : ITikTokDownloaderService
    {
        private readonly IVideoLinkParser _parser;
        public TikTokDownloaderService(IVideoLinkParser parser)
        {
            _parser = parser;
        }

        public async Task<DMFT.Model.LinkInfo?> PrepareDownloadAsync(string url)
        {
            if (!_parser.IsSupportedUrl(url)) return null;
            if (!_parser.TryParseVideoId(url, out var videoId)) return null;

            var info = new DMFT.Model.LinkInfo
            {
                Url = url,
                OriginalUrl = url,
                VideoId = videoId ?? string.Empty,
            };
            return info;
        }
    }
}
