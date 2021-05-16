using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace wow2.Modules
{
    [Name("Reddit")]
    [Group("reddit")]
    [Alias("r")]
    [Summary("Integrations with the Reddit api")]
    public class RedditModule : Module
    {
        [Command("top")]
        [Summary("Get the top post of a given subreddit")]
        public async Task TopAsync(string subreddit)
        {
            throw new NotImplementedException();
        }
    }
}