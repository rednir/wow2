namespace wow2.Bot.Modules.Games.VerbalMemory
{
    public class VerbalMemoryLeaderboardEntry : LeaderboardEntry
    {
        public VerbalMemoryLeaderboardEntry()
        {
        }

        public VerbalMemoryLeaderboardEntry(VerbalMemoryGameConfig gameConfig)
            : base(gameConfig.InitalContext.User)
        {
            Points = gameConfig.Turns;
            UniqueWords = gameConfig.SeenWords.Count;
        }

        public int Points { get; }

        public int UniqueWords { get; }
    }
}