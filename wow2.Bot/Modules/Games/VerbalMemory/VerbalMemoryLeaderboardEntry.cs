namespace wow2.Bot.Modules.Games.VerbalMemory
{
    public class VerbalMemoryLeaderboardEntry : LeaderboardEntry
    {
        public VerbalMemoryLeaderboardEntry()
        {
        }

        public VerbalMemoryLeaderboardEntry(VerbalMemoryMessage gameMessage)
            : base(gameMessage.InitialContext.User)
        {
            Points = gameMessage.Turns;
            UniqueWords = gameMessage.SeenWords.Count;
        }

        public int UniqueWords { get; set; }
    }
}