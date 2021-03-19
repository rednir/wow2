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
            const string templatesFolderPath = "res/quotetemplates";
            var listOfTemplatePaths = Directory.EnumerateFiles(templatesFolderPath);

            string templateToUsePath;
            if (author == null)
            {
                // Author has not been specified, so choose random template and use default name.
                templateToUsePath = listOfTemplatePaths.ElementAt(new Random().Next(listOfTemplatePaths.Count() - 1));
                author = Path.GetFileNameWithoutExtension(templateToUsePath);
            }
            else if (author.Length > 4)
            {
                // Check if author parameter matches the name of a quote template, and use it if so.
                templateToUsePath = Directory.EnumerateFiles
                (
                    path: templatesFolderPath,
                    searchPattern: $"*{author}*",
                    enumerationOptions: new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive }
                )
                .FirstOrDefault();

                // Default to random template if no template matches author parameter.
                if (templateToUsePath == null)
                    templateToUsePath = listOfTemplatePaths.ElementAt(new Random().Next(listOfTemplatePaths.Count() - 1));
            }
            else
            {
                // Assume the user didn't want a specific template because of the small length.
                templateToUsePath = listOfTemplatePaths.ElementAt(new Random().Next(listOfTemplatePaths.Count() - 1));
            }

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
    }
}