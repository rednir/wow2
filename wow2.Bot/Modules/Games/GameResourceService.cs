using System;
using System.Linq;
using System.Collections.Generic;
using wow2.Resources;

namespace wow2.Bot.Modules.Games
{
    public class GameResourceService
    {
        private readonly Random Random = new();

        private List<string> WordListBag { get; set; } = new();

        public string GetRandomWord()
        {
            if (WordListBag.Count == 0)
                WordListBag = Resource.WordList.OrderBy(_ => Random.Next()).ToList();

            return WordListBag[Random.Next(WordListBag.Count)];
        }
    }
}