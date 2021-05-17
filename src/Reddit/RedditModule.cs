using System;
using System.Threading.Tasks;
using Discord.Commands;
using Reddit;
using Reddit.Controllers;
using wow2.Data;

namespace wow2.Modules
{
    [Name("Reddit")]
    [Group("reddit")]
    [Alias("r")]
    [Summary("Integrations with Reddit.")]
    public class RedditModule : Module
    {
        private static readonly RedditClient RedditClient = new(DataManager.Secrets.RedditAppId, DataManager.Secrets.RedditRefreshToken);

        [Command("top")]
        [Summary("Get the top post of a given subreddit")]
        public async Task TopAsync([Name("SUBREDDIT")] string subredditName)
        {
            Subreddit subreddit = await GetSubredditAsync(subredditName);
        }

        public static async Task<Subreddit> GetSubredditAsync(string subreddit)
        {
            return RedditClient.Subreddit(subreddit);
        }
    }
}