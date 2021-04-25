using SixLabors.Fonts;
using wow2.Data;

namespace wow2.Modules.Text
{
    // Rethink this
    public static class TextModuleFonts
    {
        private static readonly FontCollection FontCollection = new();
        private static readonly FontFamily RegularFontFamily = FontCollection.Install($"{DataManager.ResDirPath}/ClearSans-Medium.ttf");
        private static readonly FontFamily LightFontFamily = FontCollection.Install($"{DataManager.ResDirPath}/Cantarell-Light.ttf");
        private static readonly FontFamily FancyFontFamily = FontCollection.Install($"{DataManager.ResDirPath}/Z003-MediumItalic.ttf");

        public static Font QuoteTextFont
        {
            get { return LightFontFamily.CreateFont(32, FontStyle.Regular); }
        }
    }
}