using System;
using Discord.Commands;
using wow2.Verbose.Messages;

namespace wow2.Modules
{
    /// <summary>The exception that is thrown when a command returns with a warning message. Usually isn't fatal, only used to quickly return out of a command with a reply.</summary>
    public class CommandReturnException : Exception
    {
        public CommandReturnException(SocketCommandContext context, string description, string title = null)
            : base(title + description)
        {
            EmbedTitle = title;
            EmbedDescription = description;
            _ = new WarningMessage(description, title)
                .SendAsync(context.Channel);
        }

        public CommandReturnException(Exception innerException, string description, string title = null)
            : base(title + description, innerException)
        {
            EmbedTitle = title;
            EmbedDescription = description;
        }

        public CommandReturnException(string description, string title = null)
            : base(title + description)
        {
            EmbedTitle = title;
            EmbedDescription = description;
        }

        public string EmbedTitle { get; }
        public string EmbedDescription { get; }
    }
}