using System.Collections.Generic;

namespace wow2.Modules.Keywords
{
    public class KeywordsModuleConfig
    {
        /// <summary>Gets a list of message IDs sent by the bot that are responses to a keyword.</summary>
        public List<ulong> ListOfResponsesId { get; } = new List<ulong>();

        public Dictionary<string, List<KeywordValue>> KeywordsDictionary { get; set; } = new Dictionary<string, List<KeywordValue>>();
        public bool IsDeleteReactionOn { get; set; } = false;
        public bool IsLikeReactionOn { get; set; } = true;
    }
}