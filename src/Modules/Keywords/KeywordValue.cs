
namespace wow2.Modules.Keywords
{
    public class KeywordValue
    {
        public string Content { get; set; }
        public string Title { get; set; }

        public ulong AddedByUserId { get; set; } = 0;
        public long DateTimeAddedBinary { get; set; } = 0;
    }
}