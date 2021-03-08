using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.WebSocket;

namespace wow2
{
    public class GuildData
    {
        public Config Config = new Config();

        public Dictionary<string, List<string>> Keywords { get; set; } = new Dictionary<string, List<string>>();
    }
}