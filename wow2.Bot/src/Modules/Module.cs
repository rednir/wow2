using Discord.Commands;

namespace wow2.Bot.Modules
{
    public abstract class Module : ModuleBase<SocketCommandContext>
    {
        public Module(BotService botService)
        {
            BotService = botService;
        }

        public BotService BotService { get; }
    }
}