using System.Timers;
using Discord.Commands;
using wow2.Data;
using wow2.Verbose.Messages;

namespace wow2.Modules.Timers
{
    public class UserTimer : Timer
    {
        /// <summary>Initializes a new instance of the <see cref="UserTimer"/> class, starting the timer.</summary>
        public UserTimer(double time, SocketCommandContext context)
            : base(time)
        {
            Config = DataManager.DictionaryOfGuildData[context.Guild.Id].Timers;
            AutoReset = false;
            Config.UserTimers.Add(this);
            Start();

            Elapsed += async (source, e) =>
            {
                Remove();
                await new InfoMessage("Time up!")
                {
                    ReplyToMessageId = context.Message.Id,
                    AllowMentions = true,
                }
                .SendAsync(context.Channel);
            };
        }

        private TimersModuleConfig Config { get; }

        /// <summary>Disposes of the timer and removes it from the guild's config.</summary>
        public void Remove()
        {
            Dispose();
            Config.UserTimers.Remove(this);
        }
    }
}