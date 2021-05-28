using System;

namespace wow2.Bot
{
    [AttributeUsage(AttributeTargets.Method)]
    public class OptionAttribute : Attribute
    {
        public OptionAttribute(string longName)
        {
            LongName = longName;
        }

        public OptionAttribute(string longName, char shortName)
        {
            LongName = longName;
            ShortName = shortName;
        }

        public string Description { get; set; } = "No description provided.";

        public string LongName { get; }
        public char ShortName { get; }
    }
}