using System;

namespace wow2.Modules.Dev
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute
    {
        public readonly string Name;

        public TestAttribute(string name)
        {
            Name = name;
        }
    }
}