using Discord.Commands;

namespace wow2.Modules.Games
{
    public abstract class GameConfig
    {
        public ICommandContext InitalContext { get; set; }
        public bool IsGameStarted { get; set; }
    }
}