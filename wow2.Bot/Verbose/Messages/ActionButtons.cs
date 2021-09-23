using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace wow2.Bot.Verbose.Messages
{
    public class ActionButton
    {
        public string Label { get; set; }

        public ButtonStyle Style { get; set; }

        public string Url { get; set; }

        public IEmote Emote { get; set; }

        public bool Disabled { get; set; }

        public int Row { get; set; }

        public Func<SocketMessageComponent, Task> Action { get; set; }
    }
}