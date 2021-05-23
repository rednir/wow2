using System;

namespace wow2.Bot.Modules.Dev
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute
    {
        public TestAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}