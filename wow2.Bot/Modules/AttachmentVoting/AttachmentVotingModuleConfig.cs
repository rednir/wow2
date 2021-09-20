using System.Collections.Generic;

namespace wow2.Bot.Modules.AttachmentVoting
{
    public class AttachmentVotingModuleConfig
    {
        public List<ulong> VotingEnabledChannelIds { get; set; } = new();

        public List<VotingEnabledAttachment> VotingEnabledAttachments { get; set; } = new();
    }
}