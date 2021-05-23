using wow2.Modules.Games;
using wow2.Modules.Keywords;
using wow2.Modules.Main;
using wow2.Modules.Moderator;
using wow2.Modules.Osu;
using wow2.Modules.Timers;
using wow2.Modules.Voice;
using wow2.Modules.YouTube;

namespace wow2.Data
{
    /// <summary>Contains all the data stored for a guild.</summary>
    public class GuildData
    {
        public string NameOfGuild { get; set; }
        public long DateTimeJoinedBinary { get; set; }

        // TODO: this doesnt seem too good.
        public MainModuleConfig Main { get; set; } = new();
        public KeywordsModuleConfig Keywords { get; set; } = new();
        public GamesModuleConfig Games { get; set; } = new();
        public VoiceModuleConfig Voice { get; set; } = new();
        public ModeratorModuleConfig Moderator { get; set; } = new();
        public OsuModuleConfig Osu { get; set; } = new();
        public YouTubeModuleConfig YouTube { get; set; } = new();
        public TimersModuleConfig Timers { get; set; } = new();
    }
}