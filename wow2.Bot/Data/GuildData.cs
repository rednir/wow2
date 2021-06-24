using System.Collections.Generic;
using System.Text.Json.Serialization;
using wow2.Bot.Modules.Games;
using wow2.Bot.Modules.Keywords;
using wow2.Bot.Modules.Main;
using wow2.Bot.Modules.Moderator;
using wow2.Bot.Modules.Osu;
using wow2.Bot.Modules.Timers;
using wow2.Bot.Modules.Voice;
using wow2.Bot.Modules.YouTube;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Data
{
    /// <summary>Contains all the data stored for a guild.</summary>
    public class GuildData
    {
        public string NameOfGuild { get; set; }

        // TODO: not sure why I don't just serialize/deserialize the DateTime object directly...
        public long DateTimeJoinedBinary { get; set; }

        [JsonIgnore]
        public List<PagedMessage> PagedMessages { get; set; } = new();

        [JsonIgnore]
        public List<ConfirmMessage> ConfirmMessages { get; set; } = new();

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