using System.Text.Json.Serialization;
using Discord.Commands;

namespace wow2.Modules.Games
{
    public abstract class GameConfig
    {
        [JsonIgnore]
        public ICommandContext InitalContext { get; set; }

        [JsonIgnore]
        public bool IsGameStarted { get; set; }
    }
}