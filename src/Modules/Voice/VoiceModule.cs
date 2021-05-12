using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Net;
using Discord.Commands;
using Discord.Audio;
using wow2.Data;
using wow2.Verbose;
using wow2.Verbose.Messages;

namespace wow2.Modules.Voice
{
    [Name("Voice")]
    [Group("vc")]
    [Alias("voice")]
    [Summary("For playing YouTube or Twitch audio in a voice channel.")]
    public class VoiceModule : Module
    {
        [Command("list")]
        [Alias("queue", "upnext")]
        [Summary("Show the song request queue.")]
        public async Task ListAsync(int page = 1)
        {
            var config = GetConfigForGuild(Context.Guild);

            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            int i = 0;
            foreach (UserSongRequest songRequest in config.CurrentSongRequestQueue)
            {
                i++;

                var fieldBuilderForSongRequest = new EmbedFieldBuilder()
                {
                    Name = $"{i}) {songRequest.VideoMetadata.title}",
                    Value = $"{songRequest.VideoMetadata.webpage_url}\nRequested at {songRequest.TimeRequested:HH:mm} by {songRequest.RequestedBy?.Mention}"
                };
                listOfFieldBuilders.Add(fieldBuilderForSongRequest);
            }

            if (listOfFieldBuilders.Count == 0)
                throw new CommandReturnException(Context, "There's nothing in the queue... how sad.");

            await new GenericMessage(
                title: "🔉 Up Next",
                fieldBuilders: listOfFieldBuilders,
                fieldBuildersPage: page)
                    .SendAsync(Context.Channel);
        }

        [Command("clear")]
        [Alias("empty", "remove", "reset")]
        [Summary("Clears the song request queue.")]
        public async Task ClearAsync()
        {
            var config = GetConfigForGuild(Context.Guild);

            config.CurrentSongRequestQueue.Clear();
            StopPlaying(config);
            await new SuccessMessage("The song request queue was cleared.")
                .SendAsync(Context.Channel);
        }

        [Command("add")]
        [Alias("play")]
        [Summary("Adds REQUEST to the song request queue. REQUEST can be a video URL or a youtube search term.")]
        public async Task AddAsync([Name("REQUEST")][Remainder] string songRequest)
        {
            var config = GetConfigForGuild(Context.Guild);

            // Might want to consider using this.
            /*if (((SocketGuildUser)Context.User).VoiceChannel == null)
                throw new CommandReturnException(Context, "Join a voice channel first before adding song requests.");*/

            VideoMetadata metadata;
            try
            {
                metadata = await YouTubeDl.GetMetadata(songRequest);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Could not fetch video metadata");
                await new ErrorMessage("One or more errors were returned.", "Could not fetch video metadata")
                    .SendAsync(Context.Channel);
                return;
            }

            config.CurrentSongRequestQueue.Enqueue(new UserSongRequest()
            {
                VideoMetadata = metadata,
                TimeRequested = DateTime.Now,
                RequestedBy = Context.User
            });

            await new SuccessMessage($"Added song request to the number `{config.CurrentSongRequestQueue.Count}` spot in the queue:\n\n**{metadata.title}**\n{metadata.webpage_url}")
                .SendAsync(Context.Channel);

            if (config.IsAutoJoinOn)
            {
                try
                {
                    await JoinVoiceChannelAsync(config, ((IGuildUser)Context.User).VoiceChannel);
                }
                catch
                {
                }
            }

            // Play song if nothing else is playing.
            if (!CheckIfAudioClientDisconnected(config.AudioClient) && config.CurrentlyPlayingSongRequest == null)
                _ = ContinueAsync();
        }

        [Command("remove")]
        [Alias("delete")]
        [Summary("Removes a song request from the queue at the given index.")]
        public async Task RemoveAsync(int number)
        {
            var config = GetConfigForGuild(Context.Guild);

            if (number < 1 || number > config.CurrentSongRequestQueue.Count)
                throw new CommandReturnException(Context, "There's no song request at that place in the queue", "Invalid number");

            int elementToRemoveIndex = number - 1;
            config.CurrentSongRequestQueue = new Queue<UserSongRequest>(
                config.CurrentSongRequestQueue.Where((_, i) => i != elementToRemoveIndex));

            await new SuccessMessage("Removed from the queue.")
                .SendAsync(Context.Channel);
        }

        [Command("remove-many")]
        [Alias("remove", "delete", "delete-many", "deletemany", "remove-many", "removemany")]
        [Summary("Removes all song requests from START to END inclusive.")]
        public async Task RemoveManyAsync(int start, int end)
        {
            var config = GetConfigForGuild(Context.Guild);

            int startIndex = start - 1;
            int endIndex = end - 1;
            config.CurrentSongRequestQueue = new Queue<UserSongRequest>(
                config.CurrentSongRequestQueue.Where((_, i) => i < startIndex || i > endIndex));

            await new SuccessMessage($"There's now {config.CurrentSongRequestQueue.Count} songs in the queue.", "Removed from the queue.")
                .SendAsync(Context.Channel);
        }

        [Command("skip")]
        [Alias("next")]
        [Summary("Stops the currently playing request and starts the next request if it exists.")]
        public async Task SkipAsync()
        {
            var config = GetConfigForGuild(Context.Guild);

            if (config.CurrentlyPlayingSongRequest == null)
                throw new CommandReturnException(Context, "There's nothing playing right now.");

            if (config.ListOfUserIdsThatVoteSkipped.Count + 1 < config.VoteSkipsNeeded)
            {
                if (config.ListOfUserIdsThatVoteSkipped.Contains(Context.User.Id))
                    throw new CommandReturnException(Context, "You've already sent a skip request.");

                config.ListOfUserIdsThatVoteSkipped.Add(Context.User.Id);
                await new InfoMessage(
                    description: $"Waiting for `{config.VoteSkipsNeeded - config.ListOfUserIdsThatVoteSkipped.Count}` more vote(s) before skipping.\n",
                    title: "Sent skip request")
                        .SendAsync(Context.Channel);
                return;
            }
            else
            {
                // Required number of vote skips has been met. 
                config.IsLoopEnabled = false;
                _ = ContinueAsync();
            }
        }

        [Command("join")]
        [Summary("Joins the voice channel of the person that executed the command.")]
        public async Task JoinAsync()
        {
            var config = GetConfigForGuild(Context.Guild);
            IVoiceChannel voiceChannelToJoin = ((IGuildUser)Context.User).VoiceChannel;

            try
            {
                await JoinVoiceChannelAsync(config, voiceChannelToJoin);
            }
            catch (ArgumentException)
            {
                throw new CommandReturnException(Context, "I'm already in a voice channel.");
            }
            catch (NullReferenceException)
            {
                throw new CommandReturnException(Context, "Join a voice channel first.");
            }
        }

        [Command("leave")]
        [Summary("Leaves the voice channel.")]
        public async Task LeaveAsync()
        {
            var config = GetConfigForGuild(Context.Guild);

            config.CurrentlyPlayingSongRequest = null;

            if (config.AudioClient == null || config.AudioClient?.ConnectionState == ConnectionState.Disconnected)
                throw new CommandReturnException(Context, "I'm not currently in a voice channel.");

            await config.AudioClient.StopAsync();
            await new SuccessMessage("Goodbye!")
                .SendAsync(Context.Channel);
        }

        [Command("np")]
        [Alias("nowplaying")]
        [Summary("Shows details about the currently playing song request.")]
        public async Task NowPlayingAsync()
        {
            var config = GetConfigForGuild(Context.Guild);

            if (config.CurrentlyPlayingSongRequest == null || CheckIfAudioClientDisconnected(config.AudioClient))
                throw new CommandReturnException(Context, "Nothing is playing right now.");

            await DisplayCurrentlyPlayingRequestAsync();
        }

        [Command("save-queue")]
        [Alias("save", "savequeue", "save-list", "savelist")]
        [Summary("Saves the current song request queue with a name for later use.")]
        public async Task SaveQueueAsync([Remainder] string name)
        {
            var config = GetConfigForGuild(Context.Guild);

            if (name.Length > 50)
                throw new CommandReturnException(Context, "Name can't be longer than 50 characters");
            if (config.SavedSongRequestQueues.ContainsKey(name))
                throw new CommandReturnException(Context, "You already have a saved queue with that name.");

            config.SavedSongRequestQueues.Add(name, config.CurrentSongRequestQueue);

            // TODO: Slow, hacky workaround to copy the queue.
            //       Maybe look into ICloneable or something.
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await DataManager.LoadGuildDataFromFileAsync(Context.Guild.Id);

            await new SuccessMessage("The current queue was cleared. You can reload this queue anytime you want.", "Saved queue")
                .SendAsync(Context.Channel);
        }

        [Command("list-saved")]
        [Alias("listsaved", "saved")]
        [Summary("Shows a list of saved song request queues.")]
        public async Task ListSavedAsync(int page = 1)
        {
            var config = GetConfigForGuild(Context.Guild);

            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            foreach (var pair in config.SavedSongRequestQueues)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"`{pair.Key}`",
                    Value = $"Total songs: {pair.Value.Count}",
                    IsInline = true
                });
            }

            await new GenericMessage(
                title: "💾 Saved Queues",
                fieldBuilders: listOfFieldBuilders,
                fieldBuildersPage: page)
                    .SendAsync(Context.Channel);
        }

        [Command("pop-queue")]
        [Alias("load", "load-queue", "loadqueue", "pop", "popqueue")]
        [Summary("Replaces the current song request queue with a saved queue. The saved queue will also be deleted.")]
        public async Task PopQueueAsync([Remainder] string name)
        {
            var config = GetConfigForGuild(Context.Guild);

            if (!config.SavedSongRequestQueues.TryGetValue(name, out var loadedQueue))
                throw new CommandReturnException(Context, "No queue with that name exists.");

            config.CurrentSongRequestQueue = loadedQueue;
            config.SavedSongRequestQueues.Remove(name);
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);

            await new SuccessMessage("Also deleted queue from the saved queue list.", "Loaded queue")
                .SendAsync(Context.Channel);
        }

        [Command("toggle-loop")]
        [Alias("loop")]
        [Summary("Toggles whether the current song request will keep looping.")]
        public async Task ToggleLoopAsync()
        {
            var config = GetConfigForGuild(Context.Guild);

            config.IsLoopEnabled = !config.IsLoopEnabled;
            await new SuccessMessage($"Looping is now turned `{(config.IsLoopEnabled ? "on" : "off")}`")
                .SendAsync(Context.Channel);
        }

        [Command("toggle-auto-np")]
        [Summary("Toggles whether the np command will be executed everytime a new song is playing.")]
        public async Task ToggleAutoNpAsync()
        {
            var config = GetConfigForGuild(Context.Guild);

            config.IsAutoNpOn = !config.IsAutoNpOn;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"Auto execution of `vc np` is turned `{(config.IsAutoNpOn ? "on" : "off")}`")
                .SendAsync(Context.Channel);
        }

        [Command("toggle-auto-join")]
        [Summary("Toggles whether the bot will try join when a new song is added to the queue.")]
        public async Task ToggleAutoJoinAsync()
        {
            var config = GetConfigForGuild(Context.Guild);

            config.IsAutoJoinOn = !config.IsAutoJoinOn;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"Auto joining when a new song is added is turned `{(config.IsAutoNpOn ? "on" : "off")}`")
                .SendAsync(Context.Channel);
        }

        [Command("set-vote-skips-needed")]
        [Summary("Sets the number of votes needed to skip a song request to NUMBER.")]
        public async Task SetVoteSkipsNeededAsync([Name("NUMBER")] int newNumberOfSkips)
        {
            if (newNumberOfSkips > Context.Guild.MemberCount)
                throw new CommandReturnException(Context, "The number of votes required is greater than the amount of people in the server.", "Number too large");

            newNumberOfSkips = Math.Max(newNumberOfSkips, 1);
            GetConfigForGuild(Context.Guild).VoteSkipsNeeded = newNumberOfSkips;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"`{newNumberOfSkips}` votes are now required to skip a song request.")
                .SendAsync(Context.Channel);
        }

        private async Task JoinVoiceChannelAsync(VoiceModuleConfig config, IVoiceChannel channel)
        {
            if (!CheckIfAudioClientDisconnected(config.AudioClient))
            {
                // Uncomment below code to also check the voice channel the bot is in.
                //IGuildUser clientUser = await Program.GetClientGuildUserAsync(Context.Channel);
                //if (clientUser.VoiceChannel == channel)
                throw new ArgumentException("Already in voice channel.");
            }

            try
            {
                config.AudioClient = await channel.ConnectAsync();
                _ = ContinueAsync();
            }
            catch (Exception ex) when (ex is WebSocketClosedException || ex is TaskCanceledException)
            {
                // No need to notify the user of these exceptions.
            }
        }

        private async Task DisplayCurrentlyPlayingRequestAsync()
        {
            UserSongRequest request = GetConfigForGuild(Context.Guild).CurrentlyPlayingSongRequest;

            if (request == null) return;

            try
            {
                await ReplyAsync(embed: BuildNowPlayingEmbed(request));
            }
            catch
            {
                await new WarningMessage($"Displaying metadata failed for the following video:\n{request?.VideoMetadata?.webpage_url}")
                    .SendAsync(Context.Channel);
            }
        }

        private async Task PlayRequestAsync(UserSongRequest request, CancellationToken cancellationToken)
        {
            var config = GetConfigForGuild(Context.Guild);

            using (var ffmpeg = YouTubeDl.CreateStreamFromVideoUrl(request.VideoMetadata.webpage_url))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = config.AudioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                try
                {
                    // No need to np if loop is enabled, and it is not the first time the song is playing.
                    if ((config.IsLoopEnabled && config.CurrentlyPlayingSongRequest != request) ||
                        !config.IsLoopEnabled)
                    {
                        config.CurrentlyPlayingSongRequest = request;
                        if (config.IsAutoNpOn)
                            await DisplayCurrentlyPlayingRequestAsync();
                    }

                    await output.CopyToAsync(discord, cancellationToken);
                }
                finally
                {
                    await discord.FlushAsync(cancellationToken);
                }
            }

            _ = ContinueAsync();
        }

        /// <summary>Continue to the next song request, if it exists. Otherwise notify the user that the queue is empty.</summary>
        private async Task ContinueAsync()
        {
            var config = GetConfigForGuild(Context.Guild);

            StopPlaying(config);

            if (CheckIfAudioClientDisconnected(config.AudioClient))
                return;

            if (config.IsLoopEnabled && config.CurrentlyPlayingSongRequest != null)
            {
                await PlayRequestAsync(config.CurrentlyPlayingSongRequest, config.CtsForAudioStreaming.Token);
            }
            else if (config.CurrentSongRequestQueue.TryDequeue(out UserSongRequest nextRequest))
            {
                await PlayRequestAsync(nextRequest, config.CtsForAudioStreaming.Token);
            }
            else
            {
                config.CurrentlyPlayingSongRequest = null;
                await new InfoMessage(
                    description: "I'll stay in the voice channel... in silence...",
                    title: "The queue is empty")
                        .SendAsync(Context.Channel);
            }
        }

        private static void StopPlaying(VoiceModuleConfig config)
        {
            config.ListOfUserIdsThatVoteSkipped.Clear();
            config.CtsForAudioStreaming.Cancel();
            config.CtsForAudioStreaming = new CancellationTokenSource();
        }

        private static Embed BuildNowPlayingEmbed(UserSongRequest request)
        {
            const string youtubeIconUrl = "https://cdn4.iconfinder.com/data/icons/social-messaging-ui-color-shapes-2-free/128/social-youtube-circle-512.png";
            const string twitchIconUrl = "https://www.net-aware.org.uk/siteassets/images-and-icons/application-icons/app-icons-twitch.png?w=585&scale=down";

            // Don't display hours if less than 1 hour.
            string formattedDuration = TimeSpan.FromSeconds(request.VideoMetadata.duration ?? 0)
                .ToString((request.VideoMetadata.duration ?? 0) >= 3600 ? @"hh\:mm\:ss" : @"mm\:ss");

            var embedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = "Now Playing",
                    IconUrl = request.VideoMetadata.extractor.StartsWith("twitch") ? twitchIconUrl : youtubeIconUrl,
                    Url = request.VideoMetadata.webpage_url
                },
                Footer = new EmbedFooterBuilder()
                {
                    Text = request.VideoMetadata.extractor.StartsWith("youtube") ?
                        $"👁️  {request.VideoMetadata.view_count ?? 0}      |      👍  {request.VideoMetadata.like_count ?? 0}      |      👎  {request.VideoMetadata.dislike_count ?? 0}      |      🕓  {formattedDuration}" : ""
                },
                Title = (request.VideoMetadata.extractor == "twitch:stream" ? $"*(LIVE)* {request.VideoMetadata.description}" : request.VideoMetadata.title) + $" *({request.VideoMetadata.uploader})*",
                ThumbnailUrl = request.VideoMetadata.thumbnails.Last().url,
                Description = $"Requested at {request.TimeRequested:HH:mm} by {request.RequestedBy.Mention}",
                Color = Color.LightGrey
            };

            return embedBuilder.Build();
        }

        public static bool CheckIfAudioClientDisconnected(IAudioClient audioClient)
            => audioClient == null || audioClient?.ConnectionState == ConnectionState.Disconnected;

        public static VoiceModuleConfig GetConfigForGuild(IGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Voice;
    }
}