namespace wow2.Bot.Modules.Osu
{
    /// <summary>What the user HTTP request will be deserialized into.</summary>
    public class UserData
    {
        public ulong id { get; set; }

        public string username { get; set; }

        public string join_date { get; set; }

        public string avatar_url { get; set; }

        public string cover_url { get; set; }

        public string playmode { get; set; }

        public Statistics statistics { get; set; }

        public class Statistics
        {
            public double pp { get; set; }

            public double hit_accuracy { get; set; }

            public int? play_time { get; set; }

            public int? global_rank { get; set; }
        }
    }
}