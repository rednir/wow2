using wow2.Modules.Config;

namespace wow2
{
    public class GuildData
    {
        public KeywordsModuleConfig Keywords { get; set; } = new KeywordsModuleConfig();
        public GamesModuleConfig Games { get; set; } = new GamesModuleConfig();
        public VoiceModuleConfig Voice { get; set; } = new VoiceModuleConfig();
    }
}