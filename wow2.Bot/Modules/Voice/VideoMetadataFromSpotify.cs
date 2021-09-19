using System.Linq;
using SpotifyAPI.Web;

namespace wow2.Bot.Modules.Voice
{
    public class VideoMetadataFromSpotify : VideoMetadata
    {
        public VideoMetadataFromSpotify(FullTrack spotifyTrack)
        {
            title = $"{spotifyTrack.Artists.FirstOrDefault()?.Name} - {spotifyTrack.Name}";
            uploader = "Spotify";
            webpage_url = "https://open.spotify.com/track/" + spotifyTrack.Id;
            duration = spotifyTrack.DurationMs / 1000;
        }
    }
}