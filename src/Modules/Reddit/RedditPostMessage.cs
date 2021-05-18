using Discord;
using Reddit.Controllers;
using wow2.Extentions;
using wow2.Verbose.Messages;

namespace wow2.Modules.Reddit
{
    public class RedditPostMessage : Message
    {
        public RedditPostMessage(Post post)
        {
            // TODO: show subreddit icon.
            EmbedBuilder = new EmbedBuilder()
            {
                Author = new()
                {
                    Name = $"r/{post.Subreddit} • u/{post.Author}",
                    Url = "https://www.reddit.com/r/" + post.Listing.Subreddit,
                },
                Footer = new()
                {
                    Text = $"{post.Score} points, {post.UpvoteRatio * 100}% upvoted",
                },
                Title = post.Title,
                Description = post.Listing.SelfText.Truncate(800, true) + $"\n\n**[See post on Reddit]({"https://www.reddit.com" + post.Listing.Permalink})**",
                Url = post.Listing.URL,
                ThumbnailUrl = post.Listing.Thumbnail.StartsWith("http") ? post.Listing.Thumbnail : null,
                Timestamp = post.Created,
                Color = Color.LightGrey,
            };
        }
    }
}