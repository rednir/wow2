namespace wow2.Modules.Games.Counting
{
    public class CountingLeaderboardEntry : LeaderboardEntry
    {
        public CountingLeaderboardEntry()
        {
        }

        public CountingLeaderboardEntry(CountingGameConfig gameConfig)
            : base(gameConfig.InitalContext.User)
        {
            Increment = gameConfig.Increment;
            FinalNumber = gameConfig.NextNumber - gameConfig.Increment;
            NumberOfCorrectMessages = gameConfig.ListOfMessages.Count;
        }

        public float Increment { get; }
        public float FinalNumber { get; }
        public int NumberOfCorrectMessages { get; }
    }
}