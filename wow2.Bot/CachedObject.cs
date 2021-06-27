using System;

namespace wow2.Bot
{
    public class CachedObject<T>
    {
        public CachedObject(string identifier, T value)
        {
            Identifier = identifier;
            Value = value;
            CreatedAt = DateTime.Now;
        }

        public string Identifier { get; }

        public T Value { get; }

        public DateTime CreatedAt { get; }
    }
}