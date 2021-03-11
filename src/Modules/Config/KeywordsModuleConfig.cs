using System.Collections.Generic;

namespace wow2.Modules.Config
{
    public class KeywordsModuleConfig
    {
        public Dictionary<string, List<string>> KeywordsDictionary { get; set; } = new Dictionary<string, List<string>>();
        
        public bool KeywordsReactToDelete { get; set; } = true;
    }
}