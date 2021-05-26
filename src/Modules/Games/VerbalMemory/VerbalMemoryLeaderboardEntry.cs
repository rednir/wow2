namespace wow2.Modules.Games.VerbalMemory
{
    public class VerbalMemoryLeaderboardEntry
    {
        public VerbalMemoryLeaderboardEntry()
        {
        }

        public VerbalMemoryLeaderboardEntry(VerbalMemoryGameConfig gameConfig)
        {
            Points = gameConfig.Turns;
            UniqueWords = gameConfig.SeenWords.Count;
        }

        public int Points { get; }

        public int UniqueWords { get; }
    }
}