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
            EmbedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"r/{post.Subreddit} • u/{post.Author}",
                },
                Title = post.Title,
                Description = post.Listing.SelfText.Truncate(1024, true),
                Color = Color.LightGrey,
            };
        }
    }
}