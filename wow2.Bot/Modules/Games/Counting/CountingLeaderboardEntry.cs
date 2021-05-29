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
            Increment = gameConfig.Increment;
            FinalNumber = gameConfig.NextNumber - gameConfig.Increment;
            NumberOfCorrectMessages = gameConfig.ListOfMessages.Count - 1;
        }

        public float Increment { get; set; }
        public float FinalNumber { get; set; }
        public int NumberOfCorrectMessages { get; set; }
    }
}