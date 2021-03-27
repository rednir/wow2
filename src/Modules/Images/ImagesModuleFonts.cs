using SixLabors.Fonts;
using wow2.Data;

namespace wow2.Modules.Images
{
    // Rethink this
    public static class ImagesModuleFonts
    {
        private static FontCollection FontCollection = new FontCollection();
        private static FontFamily RegularFontFamily = FontCollection.Install($"{DataManager.ResDirPath}/ClearSans-Medium.ttf");
        private static FontFamily LightFontFamily = FontCollection.Install($"{DataManager.ResDirPath}/Cantarell-Light.ttf");
        private static FontFamily FancyFontFamily = FontCollection.Install($"{DataManager.ResDirPath}/Z003-MediumItalic.ttf");

        public static Font QuoteTextFont
        {
            get { return LightFontFamily.CreateFont(32, FontStyle.Regular); }
        }
    }
}