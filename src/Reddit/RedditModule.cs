using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Reddit;
using Reddit.Controllers;
using Reddit.Exceptions;
using wow2.Data;

namespace wow2.Modules.Reddit
{
    [Name("Reddit")]
    [Group("reddit")]
    [Alias("r")]
    [Summary("Integrations with Reddit.")]
    public class RedditModule : Module
    {
        private static readonly RedditClient RedditClient = new(DataManager.Secrets.RedditAppId, DataManager.Secrets.RedditRefreshToken);

        public static Subreddit GetSubreddit(string subredditName)
        {
            if (subredditName.StartsWith("r/", StringComparison.OrdinalIgnoreCase))
                subredditName = subredditName.Remove(0, 2);

            Subreddit subreddit;
            try
            {
                subreddit = RedditClient.Subreddit(subredditName).About();
            }
            catch (RedditNotFoundException ex)
            {
                throw new CommandReturnException(ex, "Couldn't find a subreddit with that name.");
            }
            catch (RedditBadRequestException ex)
            {
                throw new CommandReturnException(ex, "Nah.");
            }
            catch (RedditForbiddenException ex)
            {
                throw new CommandReturnException(ex, "I can't access that subreddit, probably because it's private.");
            }

            return subreddit;
        }

        [Command("top")]
        [Summary("Gets the top post of a given subreddit.")]
        public async Task TopAsync([Name("SUBREDDIT")] string subredditName)
        {
            Subreddit subreddit;
            try
            {
                subreddit = GetSubreddit(subredditName);
            }
            catch (CommandReturnException ex)
            {
                throw new CommandReturnException(Context, ex.EmbedDescription, ex.EmbedTitle);
            }

            await new RedditPostMessage(subreddit.Posts.GetTop(limit: 1).FirstOrDefault()
                ?? throw new CommandReturnException(Context, "Doesn't seem like there's any posts here..."))
                    .SendAsync(Context.Channel);
        }
    }
}