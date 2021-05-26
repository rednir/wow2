using Discord.Commands;

namespace wow2.Bot.Modules
{
    public abstract class Module : ModuleBase<SocketCommandContext>
    {
        protected Module(BotService botService)
        {
            BotService = botService;
        }

        protected BotService BotService { get; }
    }
}