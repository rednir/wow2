using System;
using Discord;
using Discord.WebSocket;

namespace ExtentionMethods
{
    public static class Main
    {
        // Add overloads if necessary.
        public static SocketGuild GetGuild(this SocketMessage socketMessage) => ((SocketGuildChannel)socketMessage.Channel).Guild;
    }
}