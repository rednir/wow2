using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Net;
using wow2.Data;
using wow2.Verbose;
using wow2.Verbose.Messages;

namespace wow2.Modules.Voice
{
    [Name("Voice")]
    [Group("vc")]
    [Alias("voice")]
    [Summary("Play YouTube or Twitch audio in a voice channel.")]
    public class VoiceModule : Module
    {
        public VoiceModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].Voice;

        public static bool CheckIfAudioClientDisconnected(IAudioClient audioClient)
            => audioClient == null || audioClient?.ConnectionState == ConnectionState.Disconnected;

        // Doesn't display hours if less than 1 hour.
        public static string DurationAsString(float? duration) =>
            TimeSpan.FromSeconds(duration ?? 0).ToString((duration ?? 0) >= 3600 ? @"hh\:mm\:ss" : @"mm\:ss");

        [Command("list")]
        [Alias("queue", "upnext")]
        [Summary("Show the song request queue.")]
        public async Task ListAsync(int page = 1)
        {
            var message = new ListOfSongsMessage(Config.CurrentSongRequestQueue, "🔊 Up next", page);
            if (message.Embed.Fields.Length == 0)
                throw new CommandReturnException(Context, "There's nothing in the queue... how sad.");
            await message.SendAsync(Context.Channel);
        }

        [Command("clear")]
        [Alias("empty", "remove", "reset")]
        [Summary("Clears the song request queue.")]
        public async Task ClearAsync()
        {
            Config.CurrentSongRequestQueue.Clear();
            StopPlaying(Config);
            await new SuccessMessage("The song request queue was cleared.")
                .SendAsync(Context.Channel);
        }

        [Command("add")]
        [Alias("play")]
        [Summary("Adds REQUEST to the song request queue. REQUEST can be a video URL or a youtube search term.")]
        public async Task AddAsync([Name("REQUEST")][Remainder] string songRequest)
        {
            // Might want to consider using this.
            /*if (((SocketGuildUser)Context.User).VoiceChannel == null)
                throw new CommandReturnException(Context, "Join a voice channel first before adding song requests.");*/

            VideoMetadata metadata;
            try
            {
                metadata = await DownloadService.GetMetadataAsync(songRequest);
            }
            catch (ArgumentException ex)
            {
                throw new CommandReturnException(Context, $"`{ex.Message}`", "Invalid input");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Could not fetch video metadata");
                await new ErrorMessage("One or more errors were returned.", "Could not fetch video metadata")
                    .SendAsync(Context.Channel);
                return;
            }

            Config.CurrentSongRequestQueue.Enqueue(new UserSongRequest()
            {
                VideoMetadata = metadata,
                TimeRequested = DateTime.Now,
                RequestedBy = Context.User,
            });

            await new SuccessMessage($"Added song request to the number `{Config.CurrentSongRequestQueue.Count}` spot in the queue:\n\n**{metadata.title}**\n{metadata.webpage_url}")
                .SendAsync(Context.Channel);

            if (Config.IsAutoJoinOn)
            {
                try
                {
                    await JoinVoiceChannelAsync(((IGuildUser)Context.User).VoiceChannel);
                }
                catch
                {
                }
            }

            // Play song if nothing else is playing.
            if (!CheckIfAudioClientDisconnected(Config.AudioClient) && Config.CurrentlyPlayingSongRequest == null)
                _ = ContinueAsync();

            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("remove")]
        [Alias("delete")]
        [Summary("Removes a song request from the queue at the given index.")]
        public async Task RemoveAsync(int number)
        {
            if (number < 1 || number > Config.CurrentSongRequestQueue.Count)
                throw new CommandReturnException(Context, "There's no song request at that place in the queue", "Invalid number");

            int elementToRemoveIndex = number - 1;
            Config.CurrentSongRequestQueue = new Queue<UserSongRequest>(
                Config.CurrentSongRequestQueue.Where((_, i) => i != elementToRemoveIndex));

            await new SuccessMessage("Removed from the queue.")
                .SendAsync(Context.Channel);
        }

        [Command("remove-many")]
        [Alias("remove", "delete", "delete-many", "deletemany", "remove-many", "removemany")]
        [Summary("Removes all song requests from START to END inclusive.")]
        public async Task RemoveManyAsync(int start, int end)
        {
            int startIndex = start - 1;
            int endIndex = end - 1;
            Config.CurrentSongRequestQueue = new Queue<UserSongRequest>(
                Config.CurrentSongRequestQueue.Where((_, i) => i < startIndex || i > endIndex));

            await new SuccessMessage($"There's now {Config.CurrentSongRequestQueue.Count} songs in the queue.", "Removed from the queue.")
                .SendAsync(Context.Channel);
        }

        [Command("skip")]
        [Alias("next")]
        [Summary("Stops the currently playing request and starts the next request if it exists.")]
        public async Task SkipAsync()
        {
            if (Config.CurrentlyPlayingSongRequest == null)
                throw new CommandReturnException(Context, "There's nothing playing right now.");

            if (Config.ListOfUserIdsThatVoteSkipped.Count + 1 < Config.VoteSkipsNeeded)
            {
                if (Config.ListOfUserIdsThatVoteSkipped.Contains(Context.User.Id))
                    throw new CommandReturnException(Context, "You've already sent a skip request.");

                Config.ListOfUserIdsThatVoteSkipped.Add(Context.User.Id);
                await new InfoMessage(
                    description: $"Waiting for `{Config.VoteSkipsNeeded - Config.ListOfUserIdsThatVoteSkipped.Count}` more vote(s) before skipping.\n",
                    title: "Sent skip request")
                        .SendAsync(Context.Channel);
                return;
            }
            else
            {
                // Required number of vote skips has been met.
                Config.IsLoopEnabled = false;
                _ = ContinueAsync();
            }
        }

        [Command("join")]
        [Summary("Joins the voice channel of the person that executed the command.")]
        public async Task JoinAsync()
        {
            IVoiceChannel voiceChannelToJoin = ((IGuildUser)Context.User).VoiceChannel;
            try
            {
                await JoinVoiceChannelAsync(voiceChannelToJoin);
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
        [Alias("disconnect", "stop")]
        [Summary("Leaves the voice channel.")]
        public async Task LeaveAsync()
        {
            Config.CurrentlyPlayingSongRequest = null;

            if (Config.AudioClient == null || Config.AudioClient?.ConnectionState == ConnectionState.Disconnected)
                throw new CommandReturnException(Context, "I'm not currently in a voice channel.");

            await Config.AudioClient.StopAsync();
            await new SuccessMessage("Goodbye!")
                .SendAsync(Context.Channel);
        }

        [Command("shuffle")]
        [Alias("random")]
        [Summary("Randomly shuffles the song request queue.")]
        public async Task ShuffleAsync()
        {
            var random = new Random();
            Config.CurrentSongRequestQueue = new Queue<UserSongRequest>(
                Config.CurrentSongRequestQueue.OrderBy(_ => random.Next()));

            await new SuccessMessage("Shuffled the queue.")
                .SendAsync(Context.Channel);
        }

        [Command("np")]
        [Alias("nowplaying")]
        [Summary("Shows details about the currently playing song request.")]
        public async Task NowPlayingAsync()
        {
            if (Config.CurrentlyPlayingSongRequest == null || CheckIfAudioClientDisconnected(Config.AudioClient))
                throw new CommandReturnException(Context, "Nothing is playing right now.");

            await DisplayCurrentlyPlayingRequestAsync();
        }

        [Command("save-queue")]
        [Alias("save", "savequeue", "save-list", "savelist")]
        [Summary("Saves the current song request queue with a name for later use.")]
        public async Task SaveQueueAsync([Remainder] string name)
        {
            if (name.Length > 50)
                throw new CommandReturnException(Context, "Name can't be longer than 50 characters");
            if (Config.SavedSongRequestQueues.ContainsKey(name))
                throw new CommandReturnException(Context, "You already have a saved queue with that name.");

            Config.SavedSongRequestQueues.Add(name, new(Config.CurrentSongRequestQueue));
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);

            await new SuccessMessage("You can load this queue anytime you want.", "Saved queue")
                .SendAsync(Context.Channel);
        }

        [Command("list-saved")]
        [Alias("listsaved", "saved")]
        [Summary("Shows a list of songs in a saved queue.")]
        public async Task ListSavedAsync(string name, int page = 1)
        {
            if (!Config.SavedSongRequestQueues.TryGetValue(name, out var queue))
                throw new CommandReturnException(Context, "No queue with that name exists.");

            await new ListOfSongsMessage(queue, $"💾 Saved Queue: {name}", page)
                .SendAsync(Context.Channel);
        }

        [Command("list-saved")]
        [Alias("listsaved", "saved")]
        [Summary("Shows a list of saved queues.")]
        public async Task ListSavedAsync()
        {
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            foreach (var pair in Config.SavedSongRequestQueues)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"`{pair.Key}`",
                    Value = $"Total songs: {pair.Value.Count}",
                    IsInline = true,
                });
            }

            await new PagedMessage(
                title: "💾 Saved Queues",
                fieldBuilders: listOfFieldBuilders,
                page: 1)
                    .SendAsync(Context.Channel);
        }

        [Command("pop-queue")]
        [Alias("pop", "popqueue")]
        [Summary("Replaces the current song request queue with a saved queue. The saved queue will also be deleted.")]
        public async Task PopQueueAsync([Remainder] string name)
        {
            if (!Config.SavedSongRequestQueues.TryGetValue(name, out var loadedQueue))
                throw new CommandReturnException(Context, "No queue with that name exists.");

            Config.CurrentSongRequestQueue = loadedQueue;
            Config.SavedSongRequestQueues.Remove(name);
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);

            await new SuccessMessage("Also deleted queue from the saved queue list.", "Loaded queue")
                .SendAsync(Context.Channel);
        }

        [Command("load-queue")]
        [Alias("load", "loadqueue")]
        [Summary("Replaces the current song request queue with a saved queue. The saved queue will also be deleted.")]
        public async Task LoadQueueAsync([Remainder] string name)
        {
            if (!Config.SavedSongRequestQueues.TryGetValue(name, out var loadedQueue))
                throw new CommandReturnException(Context, "No queue with that name exists.");

            Config.CurrentSongRequestQueue = new(loadedQueue);
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);

            await new SuccessMessage("You can safely delete this queue from the saved queue list if you want.", "Loaded queue")
                .SendAsync(Context.Channel);
        }

        [Command("toggle-loop")]
        [Alias("loop")]
        [Summary("Toggles whether the current song request will keep looping.")]
        public async Task ToggleLoopAsync()
        {
            Config.IsLoopEnabled = !Config.IsLoopEnabled;
            await new SuccessMessage($"Looping is now turned `{(Config.IsLoopEnabled ? "on" : "off")}`")
                .SendAsync(Context.Channel);
        }

        [Command("toggle-auto-np")]
        [Summary("Toggles whether the np command will be executed everytime a new song is playing.")]
        public async Task ToggleAutoNpAsync()
        {
            Config.IsAutoNpOn = !Config.IsAutoNpOn;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"Auto execution of `vc np` is turned `{(Config.IsAutoNpOn ? "on" : "off")}`")
                .SendAsync(Context.Channel);
        }

        [Command("toggle-auto-join")]
        [Summary("Toggles whether the bot will try join when a new song is added to the queue.")]
        public async Task ToggleAutoJoinAsync()
        {
            Config.IsAutoJoinOn = !Config.IsAutoJoinOn;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"Auto joining when a new song is added is turned `{(Config.IsAutoJoinOn ? "on" : "off")}`")
                .SendAsync(Context.Channel);
        }

        [Command("set-vote-skips-needed")]
        [Summary("Sets the number of votes needed to skip a song request to NUMBER.")]
        public async Task SetVoteSkipsNeededAsync([Name("NUMBER")] int newNumberOfSkips)
        {
            if (newNumberOfSkips > Context.Guild.MemberCount)
                throw new CommandReturnException(Context, "The number of votes required is greater than the amount of people in the server.", "Number too large");

            newNumberOfSkips = Math.Max(newNumberOfSkips, 1);
            Config.VoteSkipsNeeded = newNumberOfSkips;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"`{newNumberOfSkips}` votes are now required to skip a song request.")
                .SendAsync(Context.Channel);
        }

        private static void StopPlaying(VoiceModuleConfig config)
        {
            config.ListOfUserIdsThatVoteSkipped.Clear();
            config.CtsForAudioStreaming.Cancel();
            config.CtsForAudioStreaming = new CancellationTokenSource();
        }

        private async Task JoinVoiceChannelAsync(IVoiceChannel channel)
        {
            if (!CheckIfAudioClientDisconnected(Config.AudioClient))
            {
                // Uncomment below code to also check the voice channel the bot is in.
                /*IGuildUser clientUser = await Program.GetClientGuildUserAsync(Context.Channel);
                if (clientUser.VoiceChannel == channel)*/
                throw new ArgumentException("Already in voice channel.");
            }

            try
            {
                Config.AudioClient = await channel.ConnectAsync();
                _ = ContinueAsync();
            }
            catch (Exception ex) when (ex is WebSocketClosedException || ex is TaskCanceledException)
            {
                // No need to notify the user of these exceptions.
            }
        }

        private async Task DisplayCurrentlyPlayingRequestAsync()
        {
            if (Config.CurrentlyPlayingSongRequest == null)
                return;

            try
            {
                await new NowPlayingMessage(Config.CurrentlyPlayingSongRequest)
                    .SendAsync(Context.Channel);
            }
            catch (Exception ex)
            {
                string errorText = $"Displaying metadata failed for the following video:\n{Config.CurrentlyPlayingSongRequest?.VideoMetadata?.webpage_url}";
                Logger.LogException(ex, errorText);
                await new ErrorMessage(errorText)
                    .SendAsync(Context.Channel);
            }
        }

        private async Task PlayRequestAsync(UserSongRequest request, CancellationToken cancellationToken)
        {
            using (Process ffmpeg = DownloadService.CreateStreamFromVideoUrl(request.VideoMetadata.webpage_url))
            using (Stream output = ffmpeg.StandardOutput.BaseStream)
            using (AudioOutStream discord = Config.AudioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                try
                {
                    // No need to np if loop is enabled, and it is not the first time the song is playing.
                    if ((Config.IsLoopEnabled && Config.CurrentlyPlayingSongRequest != request) ||
                        !Config.IsLoopEnabled)
                    {
                        Config.CurrentlyPlayingSongRequest = request;
                        if (Config.IsAutoNpOn)
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
            StopPlaying(Config);

            if (CheckIfAudioClientDisconnected(Config.AudioClient))
                return;

            if (Config.IsLoopEnabled && Config.CurrentlyPlayingSongRequest != null)
            {
                await PlayRequestAsync(Config.CurrentlyPlayingSongRequest, Config.CtsForAudioStreaming.Token);
            }
            else if (Config.CurrentSongRequestQueue.TryDequeue(out UserSongRequest nextRequest))
            {
                await PlayRequestAsync(nextRequest, Config.CtsForAudioStreaming.Token);
            }
            else
            {
                Config.CurrentlyPlayingSongRequest = null;
                await new InfoMessage(
                    description: "I'll stay in the voice channel... in silence...",
                    title: "The queue is empty")
                        .SendAsync(Context.Channel);
            }
        }
    }
}