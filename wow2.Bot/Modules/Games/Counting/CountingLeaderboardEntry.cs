namespace wow2.Bot.Modules.Games.Counting
{
    public class CountingLeaderboardEntry : LeaderboardEntry
    {
        public CountingLeaderboardEntry()
            : base(null)
        {
        }

        public CountingLeaderboardEntry(CountingGameMessage gameMessage)
            : base(gameMessage)
        {
            Increment = gameMessage.Increment;
            FinalNumber = gameMessage.NextNumber - gameMessage.Increment;
        }

        public float Increment { get; set; }

        public float FinalNumber { get; set; }
    }
}