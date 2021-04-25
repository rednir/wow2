using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using wow2.Verbose.Messages;
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
                // Default to random template if no template matches author parameter.
                .FirstOrDefault() ?? listOfTemplatePaths.ElementAt(new Random().Next(listOfTemplatePaths.Count() - 1));
            }
            else
            {
                // Assume the user didn't want a specific template because of the small length.
                templateToUsePath = listOfTemplatePaths.ElementAt(new Random().Next(listOfTemplatePaths.Count() - 1));
            }

            using SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(templateToUsePath);
            var fileStreamForImage = new MemoryStream();

            Size imageSize = image.Size();
            int quoteXPos = imageSize.Width / 2;
            int quoteYPos = (imageSize.Height / 2) - (imageSize.Height / 6) - (quote.Length / 4);

            image.Mutate(x => x.DrawText($"\"{quote.Wrap(40)}\"\n\n - {author}", TextModuleFonts.QuoteTextFont, SixLabors.ImageSharp.Color.LightGrey, new PointF(quoteXPos, quoteYPos)));

            await image.SaveAsync(fileStreamForImage, new JpegEncoder());
            fileStreamForImage.Position = 0;
            await Context.Channel.SendFileAsync(fileStreamForImage, "wow2quoteresult.jpg");
        }

        [Command("replace")]
        [Alias("change")]
        [Summary("Replaces all instances of OLDVALUE with NEWVALUE within TEXT")]
        public async Task ReplaceAsync(string oldValue, string newValue, [Name("text")][Remainder] string text)
        {
            if (!text.Contains(oldValue))
                throw new CommandReturnException(Context, $"There are no instances of `{oldValue}` in the given text, so there's nothing to replace.");

            await new GenericMessage(text.Replace(oldValue, $"**{newValue}**"))
                .SendAsync(Context.Channel);
        }

        [Command("emojify")]
        [Alias("emoji")]
        [Summary("Replaces all instances of OLDVALUE with NEWVALUE within TEXT")]
        public async Task EmojifyAsync([Name("text")][Remainder] string text)
        {
            text = text.RemoveUnnecessaryWhiteSpace();

            var random = new Random();
            var stringBuilder = new StringBuilder();
            foreach (string word in text.Split(' '))
            {
                string wordWithoutSymbols = word.ReplaceAll("!.?;.'#\"_-\\".ToArray(), null);
                stringBuilder.Append(word + ' ');

                var matchingEmojis = Emoijs.Array.Where(emoij
                    => emoij.Contains(wordWithoutSymbols, StringComparison.CurrentCultureIgnoreCase));

                if (!matchingEmojis.Any())
                {
                    if (random.Next(3) == 1)
                    {
                        // Sometimes use a completely random emoji if no matching emoji is found.
                        stringBuilder.Append(
                            Emoijs.Array[random.Next(Emoijs.Array.Length)]);
                    }
                }
                else
                {
                    // Choose at random from the matching emojis.
                    stringBuilder.Append(
                        matchingEmojis.ElementAt(random.Next(matchingEmojis.Count())));
                }

                stringBuilder.Append(' ');
            }

            await new GenericMessage(stringBuilder.ToString())
                .SendAsync(Context.Channel);
        }
    }
}