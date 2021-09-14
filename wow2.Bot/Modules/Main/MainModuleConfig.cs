using System.Collections.Generic;
using System.Timers;
using Discord;

namespace wow2.Bot.Modules.Main
{
    public class MainModuleConfig
    {
        public string CommandPrefix { get; set; } = "!wow";

        public Dictionary<string, string> AliasesDictionary { get; set; } = new();

        public List<ulong> VotingEnabledChannelIds { get; set; } = new();

        public List<VotingEnabledAttachment> VotingEnabledAttachments { get; set; } = new();

        public Timer IconRotateTimer { get; set; }

        public double? IconRotateTimerInterval { get; set; }

        public List<Image> IconsToRotate { get; set; } = new();

        public int IconsToRotateIndex { get; set; }
    }
}