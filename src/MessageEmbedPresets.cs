using System;
using Discord;

namespace wow2
{
    public class MessageEmbedPresets
    {
        public enum VerboseSeverity
        {
            Info, Warning, Error
        }

        /// <summary>Builds and returns an embed for letting the user know the result of an action</summary>
        public Embed Verbose(string description, VerboseSeverity severity = VerboseSeverity.Info)
        {
            var embedBuilder = new EmbedBuilder();

            switch (severity)
            {
                case VerboseSeverity.Info:
                    embedBuilder.WithTitle("Info");
                    embedBuilder.WithColor(Color.Blue);
                    break;
                
                case VerboseSeverity.Warning:
                    embedBuilder.WithTitle("Warning");
                    embedBuilder.WithColor(Color.LightOrange);
                    break;

                case VerboseSeverity.Error:
                    embedBuilder.WithTitle("Error");
                    embedBuilder.WithColor(Color.Red);
                    break;
            }
            embedBuilder.WithDescription(description);

            return embedBuilder.Build();
        }
    }
}