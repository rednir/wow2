using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace wow2.Bot.Modules.Keywords
{
    public class KeywordsModuleConfig
    {
        /// <summary>Gets or sets a list of message IDs sent by the bot that are responses to a keyword.</summary>
        [JsonIgnore]
        public List<ResponseMessage> ListOfResponseMessages { get; set; } = new();

        public Dictionary<string, List<KeywordValue>> KeywordsDictionary { get; set; } = new();
        public bool IsDeleteReactionOn { get; set; } = false;
        public bool IsLikeReactionOn { get; set; } = true;
    }
}