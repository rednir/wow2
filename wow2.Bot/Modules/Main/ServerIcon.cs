using System;
using Discord;

namespace wow2.Bot.Modules.Main
{
    public class ServerIcon
    {
        public string Url { get; set; }

        public Image Image { get; set; }

        public DateTime DateTimeAdded { get; set; }

        public string AddedByMention { get; set; }
    }
}