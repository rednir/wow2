using System.Timers;
using Discord.Commands;
using wow2.Verbose.Messages;

namespace wow2.Modules.Timers
{
    public class UserTimer : Timer
    {
        /// <summary>Initializes a new instance of the <see cref="UserTimer"/> class, starting the timer.</summary>
        public UserTimer(double time, SocketCommandContext context)
            : base(time)
        {
            AutoReset = false;
            Elapsed += async (source, e) =>
            {
                Dispose();
                await new InfoMessage("Time up!")
                {
                    ReplyToMessageId = context.Message.Id,
                    AllowMentions = true,
                }
                .SendAsync(context.Channel);
            };
            Start();
        }
    }
}