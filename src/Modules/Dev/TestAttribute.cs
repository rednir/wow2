using System;

namespace wow2.Modules.Dev
{
    /// <summary>Marks a method as a test that can be called though Discord using the `run-test` command.</summary>
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