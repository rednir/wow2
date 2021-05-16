using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using wow2.Extentions;
using wow2.Verbose.Messages;

namespace wow2.Modules.Text
{
    [Name("Text")]
    [Group("text")]
    [Summary("Change and manipulate text.")]
    public class TextModule : Module
    {
        /// <summary>Dictionary where the key is the name and the value is the image stream.</summary>
        private readonly static Dictionary<string, Stream> QuoteTemplates;

        static TextModule()
        {
            QuoteTemplates = GetQuoteTemplateImages();
        }

        [Command("quote")]
        [Alias("quotes")]
        [Summary("Creates a fake quote of a famous person. If you want to use a specific person, set AUTHOR to their name.")]
        public async Task QuoteAsync(string quote, string author = null)
        {
            Stream templateImageToUse = null;

            if (author == null)
            {
                // Author has not been specified, so choose random template and use default name.
                var chosenPair = QuoteTemplates.ElementAt(
                    new Random().Next(QuoteTemplates.Count - 1));
                author = chosenPair.Key;
                templateImageToUse = chosenPair.Value;
            }
            else if (author.Length > 3)
            {
                // Check if author parameter matches the name of a quote template, and use it if so.
                foreach (var pair in QuoteTemplates)
                {
                    if (author.Contains(pair.Key))
                    {
                        templateImageToUse = pair.Value;
                        break;
                    }
                }

                // Default to random template if no template matches author parameter.
                if (templateImageToUse == null)
                {
                    templateImageToUse = QuoteTemplates.Values
                        .ElementAt(new Random().Next(QuoteTemplates.Count - 1));
                }
            }
            else
            {
                // Assume the user didn't want a specific template because of the small length.
                templateImageToUse = QuoteTemplates.Values
                    .ElementAt(new Random().Next(QuoteTemplates.Count - 1));
            }

            templateImageToUse.Seek(0, SeekOrigin.Begin);

            using var image = Image.Load(templateImageToUse);
            var fileStreamForImage = new MemoryStream();

            Size imageSize = image.Size();
            int quoteXPos = imageSize.Width / 2;
            int quoteYPos = (imageSize.Height / 2) - (imageSize.Height / 6) - (quote.Length / 4);

            image.Mutate(x => x.DrawText($"\"{quote.Wrap(40)}\"\n\n - {author}", Fonts.QuoteTextFont, Color.LightGrey, new PointF(quoteXPos, quoteYPos)));

            await image.SaveAsync(fileStreamForImage, new JpegEncoder());
            fileStreamForImage.Position = 0;
            await Context.Channel.SendFileAsync(fileStreamForImage, "wow2quoteresult.jpg");
        }

        [Command("replace")]
        [Alias("change")]
        [Summary("Replaces all instances of OLDVALUE with NEWVALUE within TEXT.")]
        public async Task ReplaceAsync(string oldValue, string newValue, [Name("text")][Remainder] string text)
        {
            if (!text.Contains(oldValue))
                throw new CommandReturnException(Context, $"There are no instances of `{oldValue}` in the given text, so there's nothing to replace.");

            await new GenericMessage(text.Replace(oldValue, $"**{newValue}**"))
                .SendAsync(Context.Channel);
        }

        [Command("emojify")]
        [Alias("emoji")]
        [Summary("Adds emojis to some text because its funny haha.")]
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

        private static Dictionary<string, Stream> GetQuoteTemplateImages()
        {
            const string parentFolder = "quotetemplates.";
            var result = new Dictionary<string, Stream>();
            foreach (var resourceName in Program.Assembly.GetManifestResourceNames())
            {
                if (resourceName.Contains(parentFolder))
                {
                    // Get actual file name, not path.
                    int startIndex = resourceName.LastIndexOf(parentFolder) + parentFolder.Length;
                    string readableName = Path
                        .GetFileNameWithoutExtension(resourceName)[startIndex..];
                    result.Add(readableName, Program.Assembly.GetManifestResourceStream(resourceName));
                }
            }
            return result;
        }
    }
}