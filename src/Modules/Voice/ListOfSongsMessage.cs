using System.Collections.Generic;
using Discord;
using wow2.Verbose.Messages;

namespace wow2.Modules.Voice
{
    public class ListOfSongsMessage : PagedMessage
    {
        public ListOfSongsMessage(Queue<UserSongRequest> queue, string title = "List of songs", int page = -1)
            : base(new List<EmbedFieldBuilder>(), string.Empty, title)
        {
            float? totalDuration = 0;
            int i = 0;
            foreach (UserSongRequest songRequest in queue)
            {
                i++;
                totalDuration += songRequest.VideoMetadata.duration;
                var fieldBuilderForSongRequest = new EmbedFieldBuilder()
                {
                    Name = $"{i}) {songRequest.VideoMetadata.title}",
                    Value = $"[More details]({songRequest.VideoMetadata.webpage_url}) • {VoiceModule.DurationAsString(songRequest.VideoMetadata.duration)} \nRequested at {songRequest.TimeRequested:HH:mm} by {songRequest.RequestedBy?.Mention}",
                };
                AllFieldBuilders.Add(fieldBuilderForSongRequest);
            }

            Page = page;
            SetEmbedFields();
        }
    }
}