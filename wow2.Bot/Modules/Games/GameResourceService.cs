using System;
using System.Collections.Generic;
using System.Linq;
using wow2.Resources;

namespace wow2.Bot.Modules.Games
{
    public class GameResourceService
    {
        private readonly Random Random = new();

        private Queue<string> WordListBag { get; set; } = new();

        public string GetRandomWord()
        {
            if (WordListBag.Count == 0)
                WordListBag = new Queue<string>(Resource.WordList.OrderBy(_ => Random.Next()));

            return WordListBag.Dequeue();
        }
    }
}