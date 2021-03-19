using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using ExtentionMethods;

namespace wow2.Modules.Images
{
    [Name("Images")]
    [Group("images")]
    [Alias("image")]
    [Summary("For creating and editing images.")]
    public class ImagesModule : ModuleBase<SocketCommandContext>
    {
        [Command("quote")]
        [Alias("quotes")]
        public async Task QuoteAsync(string quote, string author = null)
        {
            var listOfTemplatePaths = Directory.EnumerateFiles("res/quotetemplates");
            string templateToUsePath = listOfTemplatePaths.ElementAt(new Random().Next(listOfTemplatePaths.Count() - 1));

            if (author == null)
                author = Path.GetFileNameWithoutExtension(templateToUsePath);

            using (SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(templateToUsePath))
            {
                MemoryStream fileStreamForImage = new MemoryStream();

                Size imageSize = image.Size();
                int quoteXPos = (imageSize.Width / 2) + (imageSize.Width / 32);
                int quoteYPos = (imageSize.Height / 2) - (imageSize.Height / 6) - (quote.Length / 4);

                image.Mutate(x => x.DrawText($"\"{quote.Wrap(40)}\"\n\n - {author}", ImagesModuleFonts.QuoteTextFont, SixLabors.ImageSharp.Color.LightGrey, new PointF(quoteXPos, quoteYPos)));

                await image.SaveAsync(fileStreamForImage, new JpegEncoder());
                fileStreamForImage.Position = 0;
                await Context.Channel.SendFileAsync(fileStreamForImage, "wow2quoteresult.jpg");
            }
        }

        [Command("quote-list")]
        [Alias("quotes-list", "quotelist")]
        public async Task QuoteListAsync()
        {
            var listOfTemplatePaths = Directory.EnumerateFiles("res/quotetemplates");

            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            foreach (string path in listOfTemplatePaths)
            {
            }
        }
    }
}