using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Discord.Commands;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using ExtentionMethods;

namespace wow2.Modules.Images
{
    [Name("Images")]
    [Group("images")]
    [Alias("image")]
    [Summary("For creating and editing images.")]
    public class ImagesModule : ModuleBase<SocketCommandContext>
    {
        public static readonly string QuoteResultPath = $"{System.IO.Path.GetTempPath()}/wow2quoteresult.jpg";

        [Command("quote")]
        [Alias("quotes")]
        public async Task QuoteAsync(string author, [Name("quote")] params string[] quoteSplit)
        {
            string quote = string.Join(" ", quoteSplit);

            var quoteTextOptions = new TextGraphicsOptions()
            {
                TextOptions = new TextOptions()
                {
                    TabWidth = 45
                }
            };

            var listOfTemplatePaths = Directory.EnumerateFiles("res/quotetemplates");
            string templateToUsePath = listOfTemplatePaths.ElementAt(new Random().Next(listOfTemplatePaths.Count() - 1));

            using (Image image = Image.Load(templateToUsePath))
            {
                image.Mutate(x => x.DrawText(quoteTextOptions, $"\"{quote.Wrap(40)}\"\n\n\t - {author}", ImagesModuleFonts.QuoteTextFont, Color.LightGrey, new PointF(350, 115)));

                await image.SaveAsJpegAsync(QuoteResultPath);
                await Context.Channel.SendFileAsync(QuoteResultPath);
            }
        }
    }
}