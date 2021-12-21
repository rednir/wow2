using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;

namespace wow2.Bot.Modules.YouTube
{
    public interface IYoutubeModuleService
    {
        Task<Channel> GetChannelAsync(string channelIdOrUsername);

        Task<IList<PlaylistItem>> GetChannelUploadsAsync(Channel channel, long maxResults = 5);

        Task<Video> GetVideoAsync(string id);

        Task<SearchResult> SearchForAsync(string term, string type, bool preferFallback = false);

        Task<string> GetYoutubeAudioUrlAsync(string videoId);

        Task CheckForNewVideosAsync();
    }
}