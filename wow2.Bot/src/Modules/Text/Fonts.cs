using System.Reflection;
using SixLabors.Fonts;

namespace wow2.Bot.Modules.Text
{
    public static class Fonts
    {
        static Fonts()
        {
            var assembly = Assembly.GetExecutingAssembly();

            LightFontFamily = FontCollection.Install(
                assembly.GetManifestResourceStream("wow2.res.Cantarell-Light.ttf"));
            RegularFontFamily = FontCollection.Install(
                assembly.GetManifestResourceStream("wow2.res.ClearSans-Medium.ttf"));
        }

        public static Font QuoteTextFont => LightFontFamily.CreateFont(32, FontStyle.Regular);

        private static FontCollection FontCollection { get; } = new();

        private static FontFamily RegularFontFamily { get; }

        private static FontFamily LightFontFamily { get; }

    }
}