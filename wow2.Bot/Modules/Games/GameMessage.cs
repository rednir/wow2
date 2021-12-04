using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games
{
    public abstract class GameMessage : InteractiveMessage
    {
        protected GameMessage(SocketCommandContext context, LeaderboardEntry[] leaderboard, GameResourceService resourceService)
        {
            InitialContext = context;
            Leaderboard = leaderboard;
            ResourceService = resourceService;
        }

        public override Task StopAsync()
        {
            SubmitGame();
            return base.StopAsync();
        }

        protected int PlaceInLeaderboard
        {
            get
            {
                int place;
                for (place = 1; place <= Leaderboard.Length; place++)
                {
                    if (Leaderboard[place - 1].Points < Points)
                        break;
                }

                return place;
            }
        }

        protected List<EmbedFieldBuilder> MiniLeaderboardFields
        {
            get
            {
                var result = new List<EmbedFieldBuilder>();
                int placeInLeaderboard = PlaceInLeaderboard;

                // TODO: make this logic more simple.
                if (placeInLeaderboard == 1)
                {
                    add();
                    add(1);
                    add(2);
                }
                else if (placeInLeaderboard == 2)
                {
                    add(1);
                    add();
                    add(2);
                }
                else
                {
                    add(placeInLeaderboard - 2);
                    add(placeInLeaderboard - 1);
                    add();
                }

                return result;

                void add(int place = 0)
                {
                    var entry = Leaderboard.ElementAtOrDefault(place - 1);
                    if (entry == null && place != 0)
                        return;

                    result.Add(new EmbedFieldBuilder()
                    {
                        Name = place == 0 ? $" • {placeInLeaderboard}) {Points} points" : $"{(placeInLeaderboard > place ? place : place + 1)}) {entry.Points} points",
                        Value = place == 0 ? $"{InitialContext.User.Mention} is currently playing..." : $"{entry.PlayedByMention} {entry.PlayedAt.ToDiscordTimestamp("R")}",
                    });
                }
            }
        }

        public SocketCommandContext InitialContext { get; }

        public Action SubmitGame { get; set; }

        public virtual int Points { get; protected set; }

        protected LeaderboardEntry[] Leaderboard { get; }

        protected GameResourceService ResourceService { get; }
    }
}