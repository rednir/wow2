using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using wow2.Modules;

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
            var embedBuilder = new EmbedBuilder()
            {
                Description = description
            };

            switch (severity)
            {
                case VerboseMessageSeverity.Info:
                    embedBuilder.Color = Color.Blue;
                    break;

                case VerboseMessageSeverity.Warning:
                    embedBuilder.Color = Color.LightOrange;
                    break;

                case VerboseMessageSeverity.Error:
                    embedBuilder.Title = "Something bad happened...";
                    embedBuilder.Color = Color.Red;
                    break;
            }

            return embedBuilder.Build();
        }

        /// <summary>Builds and returns an embed for responding to some user action.</summary>
        public static Embed GenericResponse(string description, string title = "")
        {
            var embedBuilder = new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = Color.LightGrey
            };

            return embedBuilder.Build();
        }

        /// <summary>Builds and returns an embed for showing a list of commands.</summary>
        public static Embed Help(List<EmbedFieldBuilder> fieldBuilders)
        {
            var embedBuilder = new EmbedBuilder()
            {
                Title = "Command Help",
                Fields = fieldBuilders,
                Color = Color.DarkGrey
            };

            return embedBuilder.Build();
        }
    }
}