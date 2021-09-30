using System.Collections.Generic;
using Discord;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Voice
{
    public class AddedRequestMessage : SuccessMessage
    {
        public AddedRequestMessage(string description, Queue<UserSongRequest> queue)
            : base(description)
        {
            Queue = queue;
        }

        protected override ActionButton[] ActionButtons => new[]
        {
            new ActionButton()
            {
                Label = "View queue",
                Style = ButtonStyle.Secondary,
                Action = async component => await component.FollowupAsync(embed: new ListOfSongsMessage(Queue).Embed, ephemeral: true),
            },
            new ActionButton()
            {
                Label = "Join voice channel",
                Style = ButtonStyle.Secondary,
                Disabled = true,
                Action = null,
            },
        };

        private Queue<UserSongRequest> Queue { get; }
    }
}