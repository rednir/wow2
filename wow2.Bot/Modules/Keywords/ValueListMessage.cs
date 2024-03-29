using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Keywords
{
    public class ValueListMessage : PagedMessage
    {
        public ValueListMessage(string keyword, List<KeywordValue> values, Func<Task> galleryButton, int? page = null)
            : base(new List<EmbedFieldBuilder>(), $"*There are {values.Count} values in total, as listed below.*", $"📒 Values for '{keyword}'", page)
        {
            foreach (KeywordValue value in values)
            {
                var user = value.AddedByUserId == 0 ?
                    "[UNKNOWN USER]" : BotService.Client.GetUser(value.AddedByUserId).Username;
                var date = value.DateTimeAddedBinary == 0 ?
                    "[UNKNOWN DATE]" : DateTime.FromBinary(value.DateTimeAddedBinary).ToDiscordTimestamp("D");

                AllFieldBuilders.Add(
                    new EmbedFieldBuilder()
                    {
                        Name = $"Added by {user} at {date}",
                        Value = $"{value.TimesLiked} times liked, {value.TimesDeleted} times deleted.\n```{value.Content}```",
                    });
            }

            GalleryButton = galleryButton;
        }

        protected override ActionButton[] ActionButtons => base.ActionButtons.Append(new ActionButton()
        {
            Label = UsernameWhoClickedButton == null ? "Show values in gallery" : $"Gallery requested by {UsernameWhoClickedButton}",
            Style = ButtonStyle.Secondary,
            Action = async component =>
            {
                UsernameWhoClickedButton = component.User.Username;
                await GalleryButton.Invoke();
                await StopAsync();
            },
        })
        .ToArray();

        private readonly Func<Task> GalleryButton;

        private string UsernameWhoClickedButton { get; set; }
    }
}