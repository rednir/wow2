using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Discord.Commands;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using wow2.Verbose;
using wow2.Data;
using wow2.Extentions;

namespace wow2.Modules.Text
{
    [Name("Text")]
    [Group("text")]
    [Summary("For changing and manipulating text.")]
    public class TextModule : ModuleBase<SocketCommandContext>
    {
        [Command("quote")]
        [Alias("quotes")]
        [Summary("Creates a fake quote of a famous person. If you want to use a specific person, set AUTHOR to their name.")]
        public async Task QuoteAsync(string quote, string author = null)
        {
            // Search for folder `quotetemplates` in runtime directory and in working directory.
            string templatesFolderPath = $"{DataManager.ResDirPath}/quotetemplates";

            var listOfTemplatePaths = Directory.EnumerateFiles(templatesFolderPath);

            string templateToUsePath;
            if (author == null)
            {
                // Author has not been specified, so choose random template and use default name.
                templateToUsePath = listOfTemplatePaths.ElementAt(new Random().Next(listOfTemplatePaths.Count() - 1));
                author = Path.GetFileNameWithoutExtension(templateToUsePath);
            }
            else if (author.Length > 3)
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
                int quoteXPos = imageSize.Width / 2;
                int quoteYPos = (imageSize.Height / 2) - (imageSize.Height / 6) - (quote.Length / 4);

                image.Mutate(x => x.DrawText($"\"{quote.Wrap(40)}\"\n\n - {author}", TextModuleFonts.QuoteTextFont, SixLabors.ImageSharp.Color.LightGrey, new PointF(quoteXPos, quoteYPos)));

                await image.SaveAsync(fileStreamForImage, new JpegEncoder());
                fileStreamForImage.Position = 0;
                await Context.Channel.SendFileAsync(fileStreamForImage, "wow2quoteresult.jpg");
            }
        }

        [Command("replace")]
        [Summary("Replaces all instances of OLDVALUE with NEWVALUE within TEXT")]
        public async Task ReplaceAsync(string oldValue, string newValue, [Name("text")] params string[] textSplit)
        {
            if (textSplit.Count() == 0)
                throw new CommandReturnException(Context, "No text was given.");
            if (!textSplit.Contains(oldValue))
                throw new CommandReturnException(Context, $"There are no instances of `{oldValue}` in the given text, so there's nothing to replace.");

            var text = Context.Message.GetParams(textSplit);

            await GenericMessenger.SendResponseAsync(Context.Channel, text.Replace(oldValue, $"**{newValue}**"));
        }
    }
}