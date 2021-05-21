using System.Timers;
using Discord.Commands;
using wow2.Data;
using wow2.Verbose.Messages;

namespace wow2.Modules.Timers
{
    public class UserTimer : Timer
    {
        /// <summary>Initializes a new instance of the <see cref="UserTimer"/> class, starting the timer.</summary>
        public UserTimer(SocketCommandContext context, double time, string message)
            : base(time)
        {
            Context = context;
            Config = DataManager.AllGuildData[Context.Guild.Id].Timers;
            AutoReset = false;

            Config.UserTimers.Add(this);
            Start();

            Elapsed += async (source, e) =>
            {
                Remove();
                await new InfoMessage(message, "Time up!")
                {
                    ReplyToMessageId = Context.Message.Id,
                    AllowMentions = true,
                }
                .SendAsync(Context.Channel);
            };
        }

        public string Message { get; }
        public SocketCommandContext Context { get; }
        private TimersModuleConfig Config { get; }

        /// <summary>Disposes of the timer and removes it from the guild's config.</summary>
        public void Remove()
        {
            Dispose();
            Config.UserTimers.Remove(this);
        }
    }
}