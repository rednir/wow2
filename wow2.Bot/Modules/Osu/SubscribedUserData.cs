namespace wow2.Bot.Modules.Osu
{
    public class SubscribedUserData
    {
        public SubscribedUserData()
        {
        }

        public SubscribedUserData(UserData userData, Score bestScore, string mode = null)
        {
            Id = userData.id;
            Username = userData.username;
            Mode = mode ?? userData.playmode;
            GlobalRank = userData.statistics.global_rank;
            BestScore = bestScore;
        }

        public ulong Id { get; set; }

        public string Username { get; set; }

        public string Mode { get; set; }

        public int? GlobalRank { get; set; }

        public Score BestScore { get; set; }
    }
}