using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.WebSocket;

namespace wow2
{
    public class GuildData
    {
        public Dictionary<string, List<string>> Keywords { get; set; } = new Dictionary<string, List<string>>()
        {
            {"test", new List<string>() {"a", "b"}},
            {"another test", new List<string>() {"c", "d", "e"}}
        };
    }
}