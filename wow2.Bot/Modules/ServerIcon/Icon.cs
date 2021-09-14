using System;
using Discord;

namespace wow2.Bot.Modules.ServerIcon
{
    public class Icon
    {
        public string Url { get; set; }

        public DateTime DateTimeAdded { get; set; }

        public string AddedByMention { get; set; }
    }
}