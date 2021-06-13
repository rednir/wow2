using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;

namespace wow2.Bot.Extensions
{
    public static class DiscordExtensions
    {
        public static SocketGuild GetGuild(this IMessage userMessage)
            => ((SocketGuildChannel)userMessage.Channel).Guild;

        public static SocketGuild GetGuild(this ISocketMessageChannel messageChannel)
            => ((SocketGuildChannel)messageChannel).Guild;

        public static string GetCommandPrefix(this IGuild guild) =>
            DataManager.AllGuildData[guild.Id].Main.CommandPrefix;

        /// <summary>Creates a string from a list of commands, with newlines placed between each command.</summary>
        /// <returns>A string representing the list of commands.</returns>
        public static string MakeReadableString(this IEnumerable<CommandInfo> commands, string commandPrefix)
        {
            string result = string.Empty;
            int index = 0;
            foreach (var command in commands)
            {
                result += command.MakeFullCommandString(commandPrefix) + "\n";
                if (index >= 5)
                    break;
                index++;
            }

            return result.TrimEnd('\n');
        }

        /// <summary>Creates a string from a list of parameters.</summary>
        /// <returns>A string representing the list of parameters.</returns>
        public static string MakeReadableString(this IEnumerable<ParameterInfo> parameters)
        {
            string parametersInfo = string.Empty;
            foreach (ParameterInfo parameter in parameters)
            {
                string optionalText = parameter.IsOptional ? "optional:" : string.Empty;
                parametersInfo += $" [{optionalText}{parameter.Name.ToUpper()}]";
            }

            return parametersInfo;
        }

        /// <summary>Creates a string from a CommandInfo, showing the command prefix and the parameters.</summary>
        /// <returns>A string representing the command.</returns>
        public static string MakeFullCommandString(this CommandInfo command, string commandPrefix)
            => $"`{commandPrefix} {(string.IsNullOrWhiteSpace(command.Module.Group) ? string.Empty : $"{command.Module.Group} ")}{command.Name}{command.Parameters.MakeReadableString()}`";
    }
}