using System;
using Discord;

namespace wow2
{
    public enum VerboseMessageSeverity
    {
        Info, Warning, Error
    }

    public static class MessageEmbedPresets
    {
        /// <summary>Builds and returns an embed for letting the user know the result of an action</summary>
        public static Embed Verbose(string description, VerboseMessageSeverity severity = VerboseMessageSeverity.Info)
        {
            var embedBuilder = new EmbedBuilder();

            switch (severity)
            {
                case VerboseMessageSeverity.Info:
                    embedBuilder.WithTitle("Info");
                    embedBuilder.WithColor(Color.Blue);
                    break;

                case VerboseMessageSeverity.Warning:
                    embedBuilder.WithTitle("Warning");
                    embedBuilder.WithColor(Color.LightOrange);
                    break;

                case VerboseMessageSeverity.Error:
                    embedBuilder.WithTitle("Error");
                    embedBuilder.WithColor(Color.Red);
                    break;
            }
            embedBuilder.WithDescription(description);

            return embedBuilder.Build();
        }
    }
}