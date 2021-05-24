using System.Collections.Generic;
using System.Timers;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Timers
{
    public class UserTimer : Timer
    {
        /// <summary>Initializes a new instance of the <see cref="UserTimer"/> class, starting the timer.</summary>
        public UserTimer(SocketCommandContext context, List<UserTimer> userTimerList, double time, string message)
            : base(time)
        {
            Context = context;
            UserTimerList = userTimerList;
            AutoReset = false;

            userTimerList.Add(this);
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
        private List<UserTimer> UserTimerList { get; }

        /// <summary>Disposes of the timer and removes it from the guild's config.</summary>
        public void Remove()
        {
            Dispose();
            UserTimerList.Remove(this);
        }
    }
}