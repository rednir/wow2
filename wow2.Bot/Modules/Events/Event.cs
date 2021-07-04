using System;
using System.Collections.Generic;

namespace wow2.Bot.Modules.Events
{
    public class Event
    {
        public string Description { get; set; }

        public DateTime ForDateTime { get; set; }

        public string CreatedByMention { get; set; }

        public List<string> AttendeeMentions { get; set; } = new();
    }
}