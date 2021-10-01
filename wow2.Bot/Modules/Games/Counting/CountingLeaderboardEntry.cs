namespace wow2.Bot.Modules.Games.Counting
{
    public class CountingLeaderboardEntry : LeaderboardEntry
    {
        public CountingLeaderboardEntry()
        {
        }

        public CountingLeaderboardEntry(CountingGameMessage gameMessage)
            : base(gameMessage.InitialContext.User)
        {
            Points = gameMessage.ListOfMessages.Count - 1;
            Increment = gameMessage.Increment;
            FinalNumber = gameMessage.NextNumber - gameMessage.Increment;
        }

        public float Increment { get; set; }

        public float FinalNumber { get; set; }
    }
}