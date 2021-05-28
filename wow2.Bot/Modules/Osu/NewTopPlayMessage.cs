using Discord;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Osu
{
    public class NewTopPlayMessage : Message
    {
        public NewTopPlayMessage(UserData userData, Score score)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{userData.username} set a new top play!",
                    IconUrl = userData.avatar_url,
                    Url = $"https://osu.ppy.sh/users/{userData.id}",
                },
                Title = OsuModule.MakeScoreTitle(score),
                Description = OsuModule.MakeScoreDescription(score),
                ImageUrl = score.beatmapSet.covers.cover,
                Color = Color.LightGrey,
            };
        }
    }
}