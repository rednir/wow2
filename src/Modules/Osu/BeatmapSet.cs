
namespace wow2.Modules.Osu
{
    public class BeatmapSet
    {
        public string artist { get; set; }
        public string creator { get; set; }
        public ulong id { get; set; }
        public string title { get; set; }
        public Covers covers { get; set; }

        public class Covers
        {
            public string cover { get; set; }
        }
    }
}