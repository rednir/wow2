using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Reddit;
using Reddit.Controllers;
using Reddit.Exceptions;
using wow2.Bot.Data;

namespace wow2.Bot.Modules.Reddit
{
    [Name("Reddit")]
    [Group("reddit")]
    [Alias("r")]
    [Summary("View Reddit posts and other content.")]
    public class RedditModule : Module
    {
        public RedditModule()
        {
            RedditClient = new RedditClient(
                BotService.Secrets.RedditAppId, BotService.Secrets.RedditRefreshToken);
        }

        public BotService BotService { get; set; }

        private RedditClient RedditClient { get; }

        public static Subreddit GetSubreddit(RedditClient client, string subredditName)
        {
            if (subredditName.StartsWith("r/", StringComparison.OrdinalIgnoreCase))
                subredditName = subredditName.Remove(0, 2);

            Subreddit subreddit;
            try
            {
                subreddit = client.Subreddit(subredditName).About();
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
        [Summary("Gets the top post of all time from a given subreddit.")]
        public async Task TopAsync([Name("SUBREDDIT")] string subredditName) =>
            await GetSubredditCommandCommonAsync(subredditName, s => s.GetTop(limit: 1));

        [Command("new")]
        [Alias("newest")]
        [Summary("Gets the newest post from a given subreddit.")]
        public async Task NewAsync([Name("SUBREDDIT")] string subredditName) =>
            await GetSubredditCommandCommonAsync(subredditName, s => s.GetNew(limit: 1));

        [Command("hot")]
        [Alias("best")]
        [Summary("Gets the first post in hot from a given subreddit.")]
        public async Task HotAsync([Name("SUBREDDIT")] string subredditName) =>
            await GetSubredditCommandCommonAsync(subredditName, s => s.GetHot(limit: 4));

        [Command("cont")]
        [Alias("controversial")]
        [Summary("Gets the most controversial of all time from given subreddit.")]
        public async Task ControversialAsync([Name("SUBREDDIT")] string subredditName) =>
            await GetSubredditCommandCommonAsync(subredditName, s => s.GetControversial(limit: 1));

        public async Task GetSubredditCommandCommonAsync(string subredditName, Func<SubredditPosts, List<Post>> action)
        {
            Subreddit subreddit;
            try
            {
                subreddit = GetSubreddit(RedditClient, subredditName);
            }
            catch (CommandReturnException ex)
            {
                throw new CommandReturnException(Context, ex.EmbedDescription, ex.EmbedTitle);
            }

            await new RedditPostMessage(action.Invoke(subreddit.Posts).Find(p => !p.Listing.Pinned)
                ?? throw new CommandReturnException(Context, "Doesn't seem like there's any posts here..."))
                    .SendAsync(Context.Channel);
        }
    }
}