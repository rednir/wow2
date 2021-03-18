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
        public async Task QuoteAsync(string quote, string author = null)
        {
            var listOfTemplatePaths = Directory.EnumerateFiles("res/quotetemplates");
            string templateToUsePath = listOfTemplatePaths.ElementAt(new Random().Next(listOfTemplatePaths.Count() - 1));
            if (author == null)
                author = Path.GetFileNameWithoutExtension(templateToUsePath);

            using (Image image = Image.Load(templateToUsePath))
            {
                Size imageSize = image.Size();
                int quoteXPos = (imageSize.Width / 2) + (imageSize.Width / 32);
                int quoteYPos = (imageSize.Height / 2) - (imageSize.Height / 6) - (quote.Length / 4);

                image.Mutate(x => x.DrawText($"\"{quote.Wrap(40)}\"\n\n - {author}", ImagesModuleFonts.QuoteTextFont, Color.LightGrey, new PointF(quoteXPos, quoteYPos)));

                await image.SaveAsJpegAsync(QuoteResultPath);
                await Context.Channel.SendFileAsync(QuoteResultPath);
            }
        }
    }
}