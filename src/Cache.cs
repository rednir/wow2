using System;
using System.Collections.Generic;

namespace wow2
{
    /// <summary>Represents a collection of recently accessed objects.</summary>
    public class Cache<T>
    {
        public Cache(int oldestCachedObjectAllowedInMinutes = 30, int maxAmountOfCachedObjects = 10)
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
                return true;
            }
        }

        public void Add(string identifier, T value)
        {
            List.Add(new(identifier, value));

            if (List.Count > MaxAmountOfCachedObjects)
                List.RemoveAt(0);
        }
    }
}