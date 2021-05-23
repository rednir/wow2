using System;

namespace wow2
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

        public string LongName { get; }
        public char ShortName { get; }
    }
}