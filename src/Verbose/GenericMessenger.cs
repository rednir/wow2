using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Rest;

namespace wow2.Verbose
{
    /// <summary>Class containing methods used to send embeds that are not specific to one command module.</summary>
    public static class GenericMessenger
    {
        /// <summary>Notify a channel about a successful action.</summary>
        public static async Task<RestUserMessage> SendSuccessAsync(ISocketMessageChannel channel, string description, string title = null)
            => await channel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Description = $"{new Emoji("<:wowsuccess:823595458978512997>")} {description}",
                Color = Color.Green
            }
            .Build());

        /// <summary>Notify a channel about some infomation.</summary>
        public static async Task<RestUserMessage> SendInfoAsync(ISocketMessageChannel channel, string description, string title = null)
            => await channel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Description = $"{new Emoji("<:wowinfo:804732580423008297>")} {description}",
                Color = Color.Blue
            }
            .Build());

        /// <summary>Notify a channel about a handled unsuccessful action.</summary>
        public static async Task<RestUserMessage> SendWarningAsync(ISocketMessageChannel channel, string description, string title = null)
             => await channel.SendMessageAsync(embed: new EmbedBuilder()
             {
                 Description = $"{new Emoji("<:wowwarning:804732632751407174>")} {description}",
                 Color = Color.LightOrange
             }
            .Build());

        /// <summary>Notify a channel about an unhandled error.</summary>
        public static async Task<RestUserMessage> SendErrorAsync(ISocketMessageChannel channel, string description, string title = null)
            => await channel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Title = $"{new Emoji("<:wowerror:804732656721199144>")} Something bad happened...",
                Description = description,
                Color = Color.Red
            }
            .Build());

        /// <summary>Send a generic response to a command in a channel.</summary>
        public static async Task<RestUserMessage> SendResponseAsync(ISocketMessageChannel channel, string description = "", string title = "", List<EmbedFieldBuilder> fieldBuilders = null)
        {
            const int maxFields = 20;

            var embedBuilder = new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = Color.LightGrey,
                Fields = fieldBuilders ?? new List<EmbedFieldBuilder>()
            };

            if (fieldBuilders?.Count > maxFields)
            {
                embedBuilder.Footer = new EmbedFooterBuilder()
                {
                    IconUrl = $"https://cdn.discordapp.com/emojis/804732632751407174.png",
                    Text = $"{fieldBuilders.Count - maxFields} items were excluded"
                };

                // Truncate list to comply with the embed fields limit. 
                fieldBuilders.RemoveRange(maxFields, fieldBuilders.Count - maxFields);
            }
            return await channel.SendMessageAsync(embed: embedBuilder.Build());
        }
    }
}