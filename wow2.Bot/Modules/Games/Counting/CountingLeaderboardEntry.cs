namespace wow2.Bot.Modules.Games.Counting
{
    public class CountingLeaderboardEntry : LeaderboardEntry
    {
        public CountingLeaderboardEntry()
        {
        }

        public CountingLeaderboardEntry(CountingGameConfig gameConfig)
            : base(gameConfig.InitalContext.User)
        {
            Points = gameConfig.ListOfMessages.Count - 1;
            Increment = gameConfig.Increment;
            FinalNumber = gameConfig.NextNumber - gameConfig.Increment;
        }

        public float Increment { get; set; }

        public float FinalNumber { get; set; }
    }
}