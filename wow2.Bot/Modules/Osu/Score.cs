using System;

namespace wow2.Bot.Modules.Osu
{
    /// <summary>What the score HTTP request will be deserialized into.</summary>
    public class Score
    {
        public ulong id { get; set; }

        public int user_id { get; set; }

        public double accuracy { get; set; }

        public string[] mods { get; set; }

        public ulong score { get; set; }

        public int max_combo { get; set; }

        public string rank { get; set; }

        public double pp { get; set; }

        public bool replay { get; set; }

        public DateTime created_at { get; set; }

        public Beatmap beatmap { get; set; }

        public BeatmapSet beatmapSet { get; set; }

        public bool Equals(Score other)
        {
            if (other == null)
                return false;
            return id == other.id;
        }
    }
}