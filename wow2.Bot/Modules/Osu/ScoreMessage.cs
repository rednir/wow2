using Discord;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Osu
{
    public class ScoreMessage : Message
    {
        public ScoreMessage(UserData userData, Score score)
        {
            // todo: put this in property get method.
            string rank = userData.statistics.global_rank.HasValue ? $"#{userData.statistics.global_rank.Value.Humanize()}" : "unranked";
            EmbedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"Played by {userData.username} ({rank})",
                    IconUrl = userData.avatar_url,
                    Url = $"https://osu.ppy.sh/users/{userData.id}/{score.mode}",
                },
                Footer = new EmbedFooterBuilder()
                {
                    Text = OsuModuleService.ModeStandardNames[score.mode],
                    IconUrl = $"https://cdn.discordapp.com/emojis/{OsuModuleService.ModeEmoteIds[score.mode]}.png",
                },
                Title = OsuModuleService.MakeScoreTitle(score),
                Description = OsuModuleService.MakeScoreDescription(score),
                ImageUrl = score.beatmapSet.covers.cover,
                Color = Color.LightGrey,
                Timestamp = score.created_at,
            };
        }
    }
}