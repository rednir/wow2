using System.Text.Json.Serialization;
using Discord.Commands;
using wow2.Modules.Games.VerbalMemory;
using wow2.Modules.Games.Counting;
using wow2.Modules.Games.NumberMemory;

namespace wow2.Modules.Games
{
    public abstract class GameConfigBase
    {
        public ICommandContext InitalContext { get; set; }
    }

    public class GamesModuleConfig
    {
        [JsonIgnore]
        public CountingGameConfig Counting { get; set; } = new CountingGameConfig();

        [JsonIgnore]
        public VerbalMemoryGameConfig VerbalMemory { get; set; } = new VerbalMemoryGameConfig();

        [JsonIgnore]
        public NumberMemoryConfig NumberMemory { get; set; } = new NumberMemoryConfig();
    }
}