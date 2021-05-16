using SixLabors.Fonts;

namespace wow2.Modules.Text
{
    // Rethink this
    public static class Fonts
    {
        private static readonly FontCollection FontCollection = new();

        private static readonly FontFamily RegularFontFamily = FontCollection.Install(
            Program.Assembly.GetManifestResourceStream("wow2.res.ClearSans-Medium.ttf"));

        private static readonly FontFamily LightFontFamily = FontCollection.Install(
            Program.Assembly.GetManifestResourceStream("wow2.res.Cantarell-Light.ttf"));

        public static Font QuoteTextFont => LightFontFamily.CreateFont(32, FontStyle.Regular);
    }
}