using System;
using System.Collections.Generic;
using Discord;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Osu
{
    public class UserInfoMessage : Message
    {
        public UserInfoMessage(UserData userData, Score[] bestScores)
        {
            string mode = bestScores.Length > 0 ? bestScores[0].mode : null;
            var fieldBuildersForScores = new List<EmbedFieldBuilder>();

            foreach (Score score in bestScores)
            {
                fieldBuildersForScores.Add(new EmbedFieldBuilder()
                {
                    Name = OsuModuleService.MakeScoreTitle(score),
                    Value = OsuModuleService.MakeScoreDescription(score),
                });
            }

            string rank = userData.statistics.global_rank.HasValue ? $"#{userData.statistics.global_rank.Value.Humanize()}" : "unranked";
            EmbedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{userData.username} | {rank}",
                    IconUrl = userData.avatar_url.StartsWith("http") ? userData.avatar_url : null,
                    Url = $"https://osu.ppy.sh/users/{userData.id}",
                },
                Footer = new EmbedFooterBuilder()
                {
                    IconUrl = mode == null ? null : $"https://cdn.discordapp.com/emojis/{OsuModuleService.ModeEmoteIds[mode]}.png",
                    Text = $"{(mode == null ? "osu!" : OsuModuleService.ModeStandardNames[mode])} • Joined: {DateTime.Parse(userData.join_date)}",
                },
                Description = $"**Performance:** {Math.Round(userData.statistics.pp)}pp\n**Accuracy:** {Math.Round(userData.statistics.hit_accuracy, 2)}%\n**Time Played:** {userData.statistics.play_time / 3600}h",
                ImageUrl = userData.cover_url,
                Fields = fieldBuildersForScores,
                Color = Color.LightGrey,
            };
        }
    }
}