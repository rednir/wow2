using System.Collections.Generic;

namespace wow2.Modules.Keywords
{
    public class KeywordsModuleConfig
    {
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