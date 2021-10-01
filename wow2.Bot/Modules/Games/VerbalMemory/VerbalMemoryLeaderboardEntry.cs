namespace wow2.Bot.Modules.Games.VerbalMemory
{
    public class VerbalMemoryLeaderboardEntry : LeaderboardEntry
    {
        public VerbalMemoryLeaderboardEntry()
            : base(null)
        {
        }

        public VerbalMemoryLeaderboardEntry(VerbalMemoryGameMessage gameMessage)
            : base(gameMessage)
        {
            UniqueWords = gameMessage.SeenWords.Count;
        }

        public int UniqueWords { get; set; }
    }
}