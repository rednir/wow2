using wow2.Bot.Modules.Games.Counting;
using wow2.Bot.Modules.Games.VerbalMemory;

namespace wow2.Bot.Modules.Games
{
    public class GamesModuleConfig
    {
        public CountingGameConfig Counting { get; set; } = new CountingGameConfig();

        public VerbalMemoryGameConfig VerbalMemory { get; set; } = new VerbalMemoryGameConfig();
    }
}