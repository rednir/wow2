using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace wow2.Bot.Verbose.Messages
{
    public class ActionSelectMenu
    {
        public string Placeholder { get; set; }

        public List<ActionSelectMenuOption> Options { get; set; }

        public int MinValues { get; set; } = 1;

        public int MaxValues { get; set; } = 1;

        public bool Disabled { get; set; }

        public int Row { get; set; }

        public Func<SocketMessageComponent, Task> Action { get; set; }
    }
}