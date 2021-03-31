using wow2.Modules.Main;
using wow2.Modules.Keywords;
using wow2.Modules.Games;
using wow2.Modules.Voice;
using wow2.Modules.Moderator;

namespace wow2.Data
{
    public class GuildData
    {
        public MainModuleConfig Main { get; set; } = new MainModuleConfig();
        public KeywordsModuleConfig Keywords { get; set; } = new KeywordsModuleConfig();
        public GamesModuleConfig Games { get; set; } = new GamesModuleConfig();
        public VoiceModuleConfig Voice { get; set; } = new VoiceModuleConfig();
        public ModeratorModuleConfig Moderator { get; set; } = new ModeratorModuleConfig();
    }
}