using System;
using Discord.Commands;

namespace wow2.Modules
{
    /// <summary>The exception that is thrown when a command returns with a warning message.</summary>
    public class CommandReturnException : Exception
    {
        public CommandReturnException(string message, SocketCommandContext context) : base(message)
        {
            context.Channel.SendMessageAsync(
                embed: MessageEmbedPresets.Verbose(message, VerboseMessageSeverity.Warning)
            );
        }
    }
}