using System;
using System.Collections.Generic;
using Discord;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose;

namespace wow2.Bot
{
    /// <summary>Represents a collection of recently accessed objects.</summary>
    public class Cache<T>
    {
        public Cache(int oldestCachedObjectAllowedInMinutes = 30, int maxAmountOfCachedObjects = 100)
        {
            MaxAmountOfCachedObjects = maxAmountOfCachedObjects;
            OldestCachedObjectAllowed = TimeSpan.FromMinutes(oldestCachedObjectAllowedInMinutes);
        }

        private int MaxAmountOfCachedObjects { get; }

        private TimeSpan OldestCachedObjectAllowed { get; }

        private List<CachedObject<T>> List { get; } = new();

        /// <summary>Gets the cached object from its identifier.</summary>
        /// <returns>True if the fetch was successful.</returns>
        public bool TryFetch(string identifier, out T obj)
        {
            CachedObject<T> result = List.Find(c => c.Identifier == identifier);

            if (result == null)
            {
                obj = default;
                return false;
            }
            else
            {
                List.Remove(result);

                if (DateTime.Now.Subtract(result.CreatedAt) > OldestCachedObjectAllowed)
                {
                    obj = default;
                    return false;
                }

                // Move to end of list.
                List.Add(result);

                obj = result.Value;
                Logger.Log($"Fetched cached {typeof(T)} object with identifier '{identifier}'", LogSeverity.Debug);
                return true;
            }
        }

        public void Add(string identifier, T value)
        {
            List.Add(new(identifier, value));
            List.Truncate(MaxAmountOfCachedObjects);
            Logger.Log($"Added new cached {typeof(T)} object with identifier '{identifier}'", LogSeverity.Debug);
        }
    }
}