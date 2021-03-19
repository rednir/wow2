using System.Collections.Generic;

namespace wow2.Modules.Keywords
{
    public class KeywordsModuleConfig
    {
        /// <summary>Contains a list of message IDs sent by the bot that are responses to a keyword. Used for the react to delete option.</summary>
        public List<ulong> ListOfResponsesId { get; set; } = new List<ulong>();

        public Dictionary<string, List<KeywordValue>> KeywordsDictionary { get; set; } = new Dictionary<string, List<KeywordValue>>();
        public bool KeywordsReactToDelete { get; set; } = true;
    }

    public class KeywordValue
    {
        public string Content { get; set; }

        // TODO: find some way to make these properties useful
        /*public string ImageUrl { get; set; }
        public string Title { get; set; }*/
    }
}