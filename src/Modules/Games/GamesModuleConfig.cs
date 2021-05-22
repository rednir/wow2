using System.Text.Json.Serialization;
using wow2.Modules.Games.Counting;
using wow2.Modules.Games.VerbalMemory;

namespace wow2.Modules.Games
{
    public class GamesModuleConfig
    {
        [JsonIgnore]
        public CountingGameConfig Counting { get; set; } = new CountingGameConfig();

        [JsonIgnore]
        public VerbalMemoryGameConfig VerbalMemory { get; set; } = new VerbalMemoryGameConfig();
    }
}