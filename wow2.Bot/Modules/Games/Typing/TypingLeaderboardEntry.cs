namespace wow2.Bot.Modules.Games.Typing
{
    public class TypingLeaderboardEntry : LeaderboardEntry
    {
        public TypingLeaderboardEntry()
            : base(null)
        {
        }

        public TypingLeaderboardEntry(TypingGameMessage gameMessage)
            : base(gameMessage)
        {
        }
    }
}