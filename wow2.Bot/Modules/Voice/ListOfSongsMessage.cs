using System.Collections.Generic;
using Discord;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Voice
{
    public class ListOfSongsMessage : PagedMessage
    {
        public ListOfSongsMessage(Queue<UserSongRequest> queue, string title = "List of songs", int? page = null)
            : base(new List<EmbedFieldBuilder>(), string.Empty, title)
        {
            double totalDuration = 0;
            int i = 0;
            foreach (UserSongRequest songRequest in queue)
            {
                i++;
                totalDuration += songRequest.VideoMetadata.duration;
                var fieldBuilderForSongRequest = new EmbedFieldBuilder()
                {
                    Name = $"{i}) {songRequest.VideoMetadata.title}",
                    Value = $"[More details]({songRequest.VideoMetadata.webpage_url}) • {VoiceModule.DurationAsString(songRequest.VideoMetadata.duration)} \nRequested at {songRequest.TimeRequested:HH:mm} by {songRequest.RequestedByMention}",
                };
                AllFieldBuilders.Add(fieldBuilderForSongRequest);
            }

            Page = page;
            UpdateEmbed();
        }
    }
}