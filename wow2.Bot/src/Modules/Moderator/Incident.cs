namespace wow2.Modules.Moderator
{
    public abstract class Incident
    {
        public ulong RequestedBy { get; set; }
        public long DateTimeBinary { get; set; }
    }
}