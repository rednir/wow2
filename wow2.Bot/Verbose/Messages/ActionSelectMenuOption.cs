using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class ActionSelectMenuOption
    {
        public string Label { get; set; }

        public string Value => Label;

        public string Description { get; set; }

        public IEmote Emote { get; set; }

        public bool Default { get; set; }
    }
}