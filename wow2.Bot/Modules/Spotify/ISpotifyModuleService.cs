using SpotifyAPI.Web;

namespace wow2.Bot.Modules.Spotify
{
    public interface ISpotifyModuleService
    {
        public ISpotifyClient Client { get; set; }
    }
}