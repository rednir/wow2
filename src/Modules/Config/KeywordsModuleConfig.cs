using System.Collections.Generic;

namespace wow2.Modules.Config
{
    public class KeywordsModuleConfig
    {
        public Dictionary<string, List<KeywordValue>> KeywordsDictionary { get; set; } = new Dictionary<string, List<KeywordValue>>();
        public bool KeywordsReactToDelete { get; set; } = true;
    }

    public class KeywordValue
    {
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string Title { get; set; }
    }
}