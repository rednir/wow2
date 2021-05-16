using System;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;

namespace wow2.Modules.Moderator
{
    public static class AutoModMethods
    {
        public static bool CheckMessagesForSpam(IEnumerable<SocketMessage> messages)
        {
            const int numberOfMessagesToCheckForSpam = 7;

            // Order the list with newest messages first.
            messages = messages.OrderByDescending(message => message.Timestamp);

            if (messages.Count() > numberOfMessagesToCheckForSpam)
            {
                var timeSpan = messages.First().Timestamp - messages.ElementAt(numberOfMessagesToCheckForSpam).Timestamp;
                if (timeSpan < TimeSpan.FromSeconds(12))
                    return true;
            }

            return false;
        }

        public static bool CheckMessagesForRepeatedContent(IEnumerable<SocketMessage> messages)
        {
            const int numberOfMessagesToCheck = 4;

            if (messages.Count() < numberOfMessagesToCheck)
                return false;

            // Order the list with newest messages first, and get subsection of list.
            messages = messages.OrderByDescending(message => message.Timestamp)
                .ToList().GetRange(0, numberOfMessagesToCheck);

            return messages.All(m => m.Content == messages.First().Content);
        }
    }
}