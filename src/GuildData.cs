using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.WebSocket;
using wow2.Modules.Config;

namespace wow2
{
    public class GuildData
    {
        public KeywordsModuleConfig Keywords { get; set; } = new KeywordsModuleConfig();
    }
}