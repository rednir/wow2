using System.Collections.Generic;

namespace wow2.Modules.Main
{
    public class MainModuleConfig
    {
        public string CommandPrefix { get; set; } = "!wow";
        public Dictionary<string, string> AliasesDictionary { get; set; } = new Dictionary<string, string>();
    }
}