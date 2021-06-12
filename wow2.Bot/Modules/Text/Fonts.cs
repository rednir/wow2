using SixLabors.Fonts;
using wow2.Resources;

namespace wow2.Bot.Modules.Text
{
    // Rethink this
    public static class Fonts
    {
        private static readonly FontCollection FontCollection = new();

        private static readonly FontFamily EmojiFontFamily = FontCollection.Install(
            Resource.ResourceAssembly.GetManifestResourceStream("wow2.Resources.Fonts.OpenSansEmoji.ttf"));

        private static readonly FontFamily RegularFontFamily = FontCollection.Install(
            Resource.ResourceAssembly.GetManifestResourceStream("wow2.Resources.Fonts.ClearSans-Medium.ttf"));

        private static readonly FontFamily LightFontFamily = FontCollection.Install(
            Resource.ResourceAssembly.GetManifestResourceStream("wow2.Resources.Fonts.Cantarell-Light.ttf"));

        public static Font QuoteTextFont => EmojiFontFamily.CreateFont(32, FontStyle.Regular);
    }
}