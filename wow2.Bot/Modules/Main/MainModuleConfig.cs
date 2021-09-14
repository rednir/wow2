using System;
using System.Collections.Generic;
using Discord;

namespace wow2.Bot.Modules.Main
{
    public class MainModuleConfig
    {
        public string CommandPrefix { get; set; } = "!wow";

        public Dictionary<string, string> AliasesDictionary { get; set; } = new();

        public List<ulong> VotingEnabledChannelIds { get; set; } = new();

        public List<VotingEnabledAttachment> VotingEnabledAttachments { get; set; } = new();

        public bool IsIconRotateOn { get; set; }

        public TimeSpan IconRotateTimeSpan { get; set; }

        public List<Image> IconsToRotate { get; set; }
    }
}