using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Rest;

namespace wow2.Verbose
{
    public static class Messenger
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
        public static async Task<RestUserMessage> SendGenericResponseAsync(ISocketMessageChannel channel, string description = "", string title = "", List<EmbedFieldBuilder> fieldBuilders = null)
            => await channel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = Color.LightGrey,
                Fields = fieldBuilders
            }
            .Build());
    }
}