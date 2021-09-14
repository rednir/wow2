using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Verbose;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.ServerIcon
{
    [Name("Server Icon")]
    [Group("icon")]
    [Summary("Manage the icon of this server.")]
    public class ServerIconModule : Module
    {
        public static WebClient WebClient { get; set; } = new();

        private ServerIconModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].ServerIcon;

        public static void InitializeAllTimers()
        {
            lock (DataManager.AllGuildData)
            {
                foreach (var pair in DataManager.AllGuildData)
                {
                    try
                    {
                        var guild = BotService.Client.GetGuild(pair.Key);
                        InitializeTimer(guild, pair.Value.ServerIcon);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex, $"Exception thrown when trying to initialize server icon timer for {pair.Value.NameOfGuild} ({pair.Key})");
                    }
                }
            }
        }

        private static void InitializeTimer(SocketGuild guild, ServerIconModuleConfig config)
        {
            if (config.IconRotateTimerInterval == null)
                return;

            config.IconRotateTimer?.Stop();
            config.IconRotateTimer = new Timer()
            {
                Interval = config.IconRotateTimerInterval.Value,
            };

            config.IconRotateTimer.Elapsed += async (source, e) =>
            {
                if (config.IconsToRotate.Count < 2)
                    return;

                if (config.IconsToRotateIndex >= config.IconsToRotate.Count)
                    config.IconsToRotateIndex = 0;

                try
                {
                    // TODO: probably want to get rid of invalid urls here after a couple failed tries...
                    byte[] imageBytes = WebClient.DownloadData(config.IconsToRotate[config.IconsToRotateIndex].Url);
                    await guild.ModifyAsync(g => g.Icon = new Image(new MemoryStream(imageBytes)));
                    Logger.Log($"Rotated guild icon to index {config.IconsToRotateIndex} for {guild.Name} ({guild.Id})", LogSeverity.Debug);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Exception thrown when trying to modify server icon for {guild.Name} ({guild.Id})");
                }

                config.IconsToRotateIndex++;
            };

            config.IconRotateTimer.Start();
        }

        [Command("toggle-icon-rotate")]
        [Alias("toggle-icon-rotation", "toggle-icon", "toggle-rotate", "toggle")]
        [Summary("Toggles whether this server's icon will rotate periodically. You can configure what images are in the rotation.")]
        public async Task ToggleIconRotateAsync()
        {
            await SendToggleQuestionAsync(
                currentState: Config.IconRotateTimerInterval != null,
                setter: async x =>
                {
                    if (x)
                    {
                        await new TimeSpanSelectorMessage(
                            confirmFunc: async t =>
                            {
                                if (t < TimeSpan.FromHours(1))
                                {
                                    await new WarningMessage("Time span is too short. Try something a bit longer.")
                                        .SendAsync(Context.Channel);
                                    return;
                                }

                                Config.IconRotateTimerInterval = t.TotalMilliseconds;
                                InitializeTimer(Context.Guild, Config);
                                await new SuccessMessage($"The server icon will rotate periodically{(Config.IconsToRotate.Count < 2 ? " once you add more than 2 icons to the list" : null)}.")
                                    .SendAsync(Context.Channel);
                            },
                            description: "Cool! Now you need to set how many days you want in between each rotation.")
                        {
                            TimeSpan = TimeSpan.FromMilliseconds(Config.IconRotateTimerInterval ?? 0),
                        }
                            .SendAsync(Context.Channel);
                    }
                    else if (!x)
                    {
                        Config.IconRotateTimerInterval = null;
                        Config.IconRotateTimer?.Stop();
                    }
                },
                toggledOnMessage: null,
                toggledOffMessage: "The server icon will no longer rotate periodically.");
        }

        [Command("add")]
        [Summary("Adds an image for server icon rotation. IMAGEURL must contain an image only.")]
        public async Task AddAsync(string imageUrl)
        {
            if (!imageUrl.StartsWith("http://") && !imageUrl.StartsWith("https://"))
                throw new CommandReturnException(Context, "Make sure you link to an image!");

            Config.IconsToRotate.Add(new Icon()
            {
                Url = imageUrl,
                DateTimeAdded = DateTime.Now,
                AddedByMention = Context.User.Mention,
            });

            await new SuccessMessage($"Added server icon to the list.{(Config.IconRotateTimerInterval == null ? " Remember to toggle server icon rotation on!" : null)}")
                .SendAsync(Context.Channel);
        }

        [Command("remove")]
        [Alias("delete")]
        [Summary("Removes an image from the server icon rotation from ID.")]
        public async Task RemoveAsync(int id)
        {
            if (id > Config.IconsToRotate.Count || id < 1)
                throw new CommandReturnException(Context, "No such icon with that ID.");

            Config.IconsToRotate.RemoveAt(id - 1);
            await new SuccessMessage("Removed icon from the rotation.")
                .SendAsync(Context.Channel);
        }

        [Command("list")]
        [Alias("list-icons", "icons")]
        [Summary("Lists all the server icons in rotation.")]
        public async Task ListAsync(int page = 1)
        {
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();

            int id = 1;
            foreach (var icon in Config.IconsToRotate)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"ID: {id}",
                    Value = $"[View image]({icon.Url}) • Added by {icon.AddedByMention} at {icon.DateTimeAdded.ToShortDateString()}",
                });
                id++;
            }

            await new PagedMessage(
                fieldBuilders: listOfFieldBuilders,
                title: "📷 Rotating server icons",
                page: page)
                    .SendAsync(Context.Channel);
        }
    }
}