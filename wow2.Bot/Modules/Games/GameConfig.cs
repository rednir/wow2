using System.Text.Json.Serialization;
using Discord.Commands;

namespace wow2.Bot.Modules.Games
{
    public abstract class GameConfig
    {
        [JsonIgnore]
        public ICommandContext InitalContext { get; set; }

        [JsonIgnore]
        public bool IsGameStarted { get; set; }
    }
}