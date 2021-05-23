using Discord;
using Discord.WebSocket;
using wow2.Verbose.Messages;

namespace wow2.Modules.Moderator
{
    public class UserRecordMessage : Message
    {
        public UserRecordMessage(SocketGuildUser user, UserRecord record)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = user.ToString(),
                    IconUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(),
                },
                Title = "User record overview",
                Description = $"{record.Warnings.Count} warnings, {record.Mutes.Count} mutes.",
                Color = Color.LightGrey,
            };
        }
    }
}