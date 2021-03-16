using SixLabors.Fonts;

namespace wow2.Modules.Images
{
    public static class ImagesModuleFonts
    {
        private static FontCollection FontCollection = new FontCollection();
        private static FontFamily RegularFontFamily = FontCollection.Install("res/ClearSans-Medium.ttf");
        private static FontFamily FancyFontFamily = FontCollection.Install("res/Z003-MediumItalic.ttf");

        public static Font QuoteTextFont {
            get { return FancyFontFamily.CreateFont(18, FontStyle.Regular); }
        }
    }
}