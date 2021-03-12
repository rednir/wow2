using wow2.Modules.Keywords;
using wow2.Modules.Games;
using wow2.Modules.Voice;

namespace wow2
{
    public class GuildData
    {
        public KeywordsModuleConfig Keywords { get; set; } = new KeywordsModuleConfig();
        public GamesModuleConfig Games { get; set; } = new GamesModuleConfig();
        public VoiceModuleConfig Voice { get; set; } = new VoiceModuleConfig();
    }
}