using Discord;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Osu
{
    public class ScoreMessage : Message
    {
        public ScoreMessage(UserData userData, Score score)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"Played by {userData.username}",
                    IconUrl = userData.avatar_url,
                    Url = $"https://osu.ppy.sh/users/{userData.id}",
                },
                Footer = new EmbedFooterBuilder()
                {
                    Text = "osu!standard",
                    IconUrl = $"https://cdn.discordapp.com/emojis/{OsuModule.StdEmoteId}.png",
                },
                Title = OsuModule.MakeScoreTitle(score),
                Description = OsuModule.MakeScoreDescription(score),
                ImageUrl = score.beatmapSet.covers.cover,
                Color = Color.LightGrey,
                Timestamp = score.created_at,
            };
        }
    }
}