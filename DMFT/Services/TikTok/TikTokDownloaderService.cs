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
        private readonly ITikTokLinkParser _parser;
        public TikTokDownloaderService(ITikTokLinkParser parser)
        {
            _parser = parser;
        }

        public async Task<DMFT.Model.LinkInfo?> PrepareDownloadAsync(string url)
        {
            if (!_parser.IsTikTokUrl(url)) return null;
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
