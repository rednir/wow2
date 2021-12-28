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
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Modules.YouTube;
using wow2.Bot.Verbose;
using wow2.Bot.Verbose.Messages;
using YoutubeExplode.Exceptions;

namespace wow2.Bot.Modules.Voice
{
    [Name("Voice")]
    [Group("vc")]
    [Alias("voice")]
    [Summary("Play YouTube, Twitch or Spotify audio in a voice channel.")]
    public class VoiceModule : Module
    {
        public VoiceModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].Voice;

        public IYoutubeModuleService YouTubeService { get; set; }

        public static bool CheckIfAudioClientDisconnected(IAudioClient audioClient)
            => audioClient == null || audioClient?.ConnectionState == ConnectionState.Disconnected;

        // Doesn't display days if less than 1 day.
        public static string DurationAsString(double? duration) =>
            TimeSpan.FromSeconds(duration ?? 0).ToString((duration ?? 0) >= 86400 ? @"dd'd 'hh'h 'mm'm'" : @"hh'h 'mm'm 'ss's'");

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
        [Alias("empty", "reset")]
        [Summary("Clears the song request queue and stops the currently playing request.")]
        public async Task ClearAsync()
        {
            Config.CurrentSongRequestQueue.Clear();
            StopPlaying();
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

            List<VideoMetadata> metadataList = await GetMetadataForRequestAsync(songRequest);

            if (metadataList.Count > 1)
            {
                await new QuestionMessage(
                    description: "Do you want to clear the current queue first?",
                    title: "You are adding a playlist",
                    onConfirm: async () =>
                    {
                        Config.CurrentSongRequestQueue.Clear();
                        StopPlaying();
                        await next();
                    },
                    onDeny: next)
                        .SendAsync(Context.Channel);
            }
            else
            {
                await next();
            }

            async Task next()
            {
                foreach (var metadata in metadataList)
                    Config.CurrentSongRequestQueue.Enqueue(new UserSongRequest(metadata, Context.User));

                string successText = metadataList.Count > 1 ?
                    $"Added a playlist with {metadataList.Count} items.\nYou can clear this queue with `{Context.Guild.GetCommandPrefix()} vc clear`, or save it for later with `{Context.Guild.GetCommandPrefix()} vc save-queue [NAME]`" :
                    $"Added song request to the number `{Config.CurrentSongRequestQueue.Count}` spot in the queue:\n\n**{metadataList[0].Title}**\n{metadataList[0].WebpageUrl}";

                bool isDisconnected = CheckIfAudioClientDisconnected(Config.AudioClient);
                if (isDisconnected && !Config.IsAutoJoinOn)
                {
                    await new AddedRequestMessage($"{successText}\n\n**You have `toggle-auto-join` turned off, **so if you want me to join the voice channel you'll have to type `{Context.Guild.GetCommandPrefix()} vc join`", Config.CurrentSongRequestQueue)
                        .SendAsync(Context.Channel);
                }
                else
                {
                    await new AddedRequestMessage(successText, Config.CurrentSongRequestQueue)
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
                    if (!isDisconnected && Config.CurrentlyPlayingSongRequest == null)
                        _ = ContinueAsync();
                }
            }
        }

        [Command("play-now")]
        [Alias("playnow")]
        [Summary("Plays a request immediately, stopping the currently playing request.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task PlayNowAsync([Name("REQUEST")][Remainder] string songRequest)
        {
            if (CheckIfAudioClientDisconnected(Config.AudioClient))
                throw new CommandReturnException(Context, "I'm not in a voice channel.");

            List<VideoMetadata> metadataList = await GetMetadataForRequestAsync(songRequest);

            if (metadataList.Count > 1)
                throw new CommandReturnException(Context, "Try clear the current queue and add the playlist normally.", "Can't use this command with playlists");

            Config.PlayNowRequest = new UserSongRequest(metadataList.Single(), Context.User);

            _ = ContinueAsync();
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

        [Command("remove-last")]
        [Alias("pop")]
        [Summary("Removes the last song request that was added.")]
        public async Task RemoveLastAsync()
        {
            if (Config.CurrentSongRequestQueue.Count == 0)
                throw new CommandReturnException(Context, "The queue is empty, add some songs first!", "Nothing to remove.");

            await RemoveAsync(Config.CurrentSongRequestQueue.Count);
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
        [Summary("Sends a skip request for the currently playing song.")]
        public async Task SkipAsync()
        {
            if (Config.CurrentlyPlayingSongRequest == null)
                throw new CommandReturnException(Context, "There's nothing playing right now.");

            if (Config.ListOfUserIdsThatVoteSkipped.Contains(Context.User.Id))
                throw new CommandReturnException(Context, "You've already sent a skip request.");

            if (Config.ListOfUserIdsThatVoteSkipped.Count + 1 < Config.VoteSkipsNeeded)
            {
                Config.ListOfUserIdsThatVoteSkipped.Add(Context.User.Id);
                await new InfoMessage(
                    description: $"Waiting for `{Config.VoteSkipsNeeded - Config.ListOfUserIdsThatVoteSkipped.Count}` more vote(s) before skipping.\n",
                    title: $"Sent skip request for {Context.User.Mention}")
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

        [Command("force-skip")]
        [Alias("fs", "forceskip", "force")]
        [Summary("Stops the currently playing request immediately and starts the next request if it exists.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public Task ForceSkipAsync()
        {
            if (Config.CurrentlyPlayingSongRequest == null)
                throw new CommandReturnException(Context, "There's nothing playing right now.");

            Config.IsLoopEnabled = false;
            _ = ContinueAsync();

            return Task.CompletedTask;
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
            StopPlaying();

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
        [Alias("popqueue")]
        [Summary("Replaces the current song request queue with a saved queue. The saved queue will also be deleted.")]
        public async Task PopQueueAsync([Remainder] string name)
        {
            if (!Config.SavedSongRequestQueues.TryGetValue(name, out var loadedQueue))
                throw new CommandReturnException(Context, "No queue with that name exists.");

            Config.CurrentSongRequestQueue = loadedQueue;
            Config.SavedSongRequestQueues.Remove(name);

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
            await new SuccessMessage("You can safely delete this queue from the saved queue list if you want.", "Loaded queue")
                .SendAsync(Context.Channel);
        }

        [Command("toggle-loop")]
        [Alias("loop")]
        [Summary("Toggles whether the current song request will keep looping.")]
        public async Task ToggleLoopAsync()
        {
            await SendToggleQuestionAsync(
                currentState: Config.IsLoopEnabled,
                setter: x => Config.IsLoopEnabled = x,
                toggledOnMessage: "The current song will loop.",
                toggledOffMessage: "Looping has been disabled.");
        }

        [Command("toggle-auto-np")]
        [Summary("Toggles whether the np command will be executed everytime a new song is playing.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task ToggleAutoNpAsync()
        {
            await SendToggleQuestionAsync(
                currentState: Config.IsAutoNpOn,
                setter: x => Config.IsAutoNpOn = x,
                toggledOnMessage: "The `vc np` command will be executed everytime a new song is playing.",
                toggledOffMessage: "The `vc np` command will no longer be executed everytime a new song is playing.");
        }

        [Command("toggle-auto-join")]
        [Summary("Toggles whether the bot will try join when a new song is added to the queue.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task ToggleAutoJoinAsync()
        {
            await SendToggleQuestionAsync(
                currentState: Config.IsAutoJoinOn,
                setter: x => Config.IsAutoJoinOn = x,
                toggledOnMessage: "The bot will join a voice channel automatically.",
                toggledOffMessage: "The bot will no longer join a voice channel automatically, you'll have to use the `vc join` command.");
        }

        [Command("set-vote-skips-needed")]
        [Summary("Sets the number of votes needed to skip a song request to NUMBER.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetVoteSkipsNeededAsync([Name("NUMBER")] int newNumberOfSkips)
        {
            if (newNumberOfSkips > Context.Guild.MemberCount)
                throw new CommandReturnException(Context, "The number of votes required is greater than the amount of people in the server.", "Number too large");

            newNumberOfSkips = Math.Max(newNumberOfSkips, 1);
            Config.VoteSkipsNeeded = newNumberOfSkips;
            await new SuccessMessage($"`{newNumberOfSkips}` votes are now required to skip a song request.")
                .SendAsync(Context.Channel);
        }

        private void StopPlaying()
        {
            Config.ListOfUserIdsThatVoteSkipped.Clear();
            Config.CtsForAudioStreaming.Cancel();
            Config.CtsForAudioStreaming = new CancellationTokenSource();
            Config.CurrentlyPlayingSongRequest = null;
        }

        private async Task<List<VideoMetadata>> GetMetadataForRequestAsync(string request)
        {
            try
            {
                return await DownloadService.GetMetadataAsync(request);
            }
            catch (ArgumentException ex)
            {
                throw new CommandReturnException(Context, $"{ex.Message}", "Invalid input");
            }
            catch (VideoUnplayableException)
            {
                throw new CommandReturnException(Context, "The video is most likely age-restricted.", "Video is unplayable");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Could not fetch video metadata");
                await new ErrorMessage($"```{ex}```", "Could not fetch video metadata")
                    .SendAsync(Context.Channel);
                throw new CommandReturnException();
            }
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

                var nextRequest = Config.CurrentSongRequestQueue.Peek();
                if (DateTime.Now - nextRequest.TimeRequested > TimeSpan.FromHours(24))
                {
                    await new InfoMessage($"You might want to clear the current queue with `{Context.Guild.GetCommandPrefix()} vc clear`", $"The queue has some requests from over {nextRequest.TimeRequested.ToDiscordTimestamp("R")}")
                        .SendAsync(Context.Channel);
                }

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

            NowPlayingMessage nowPlayingMessage = null;
            try
            {
                nowPlayingMessage = new NowPlayingMessage(Config.CurrentlyPlayingSongRequest, Config, skipButton);
                await nowPlayingMessage.SendAsync(Context.Channel);
            }
            catch (Exception ex)
            {
                string errorText = $"Displaying metadata failed for the following video:\n{Config.CurrentlyPlayingSongRequest?.VideoMetadata?.WebpageUrl}";
                Logger.LogException(ex, errorText);
                await new ErrorMessage(errorText)
                    .SendAsync(Context.Channel);
            }

            // Almost same implementation as SkipAsync()
            async Task skipButton(SocketMessageComponent component, UserSongRequest request)
            {
                if (Config.CurrentlyPlayingSongRequest != request)
                {
                    await component.FollowupAsync(embed: new WarningMessage("This request has already finished playing.").Embed, ephemeral: true);
                    return;
                }

                if (Config.ListOfUserIdsThatVoteSkipped.Contains(component.User.Id))
                {
                    await component.FollowupAsync(embed: new WarningMessage("You've already sent a skip request.").Embed, ephemeral: true);
                    return;
                }

                if (Config.ListOfUserIdsThatVoteSkipped.Count + 1 < Config.VoteSkipsNeeded)
                {
                    Config.ListOfUserIdsThatVoteSkipped.Add(component.User.Id);
                    await new InfoMessage(
                        description: $"Waiting for `{Config.VoteSkipsNeeded - Config.ListOfUserIdsThatVoteSkipped.Count}` more vote(s) before skipping.\n",
                        title: $"Sent skip request for {component.User.Mention}")
                            .SendAsync(Context.Channel);
                    return;
                }
                else
                {
                    // TODO: preferably I want this logic contained inside the message.
                    if (nowPlayingMessage != null)
                    {
                        nowPlayingMessage.UsernameWhoSkipped = component.User.Username;
                        await nowPlayingMessage.StopAsync();
                    }

                    Config.IsLoopEnabled = false;
                    _ = ContinueAsync();
                }
            }
        }

        private async Task PlayRequestAsync(UserSongRequest request, CancellationToken cancellationToken)
        {
            if (request.VideoMetadata.LookupTitleOnYoutube)
            {
                string searchTerm = request.VideoMetadata.Title;
                Logger.Log($"About to lookup spotify request {searchTerm} on youtube.", LogSeverity.Debug);

                try
                {
                    var search = await YouTubeService.SearchForAsync(searchTerm, "video", true);
                    var video = await YouTubeService.GetVideoAsync(search.Id.VideoId);
                    request.VideoMetadata = new VideoMetadata(video)
                    {
                        // Remember what the original source was.
                        Title = request.VideoMetadata.Title,
                        Extractor = request.VideoMetadata.Extractor,
                        DirectAudioUrl = await YouTubeService.GetYoutubeAudioUrlAsync(video.Id),
                    };
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Lookup failed for {request.VideoMetadata.Title}");
                    await new ErrorMessage($"```{request.VideoMetadata.Title}```", "Couldn't lookup song.")
                        .SendAsync(Context.Channel);
                    _ = ContinueAsync();
                    return;
                }
            }

            if (DateTime.Now + TimeSpan.FromSeconds(request.VideoMetadata.Duration) > request.VideoMetadata.DirectAudioExpiryDate)
                request.VideoMetadata.DirectAudioUrl = await YouTubeService.GetYoutubeAudioUrlAsync(request.VideoMetadata.Id);

            Config.CurrentlyPlayingSongRequest = request;
            if (Config.IsAutoNpOn)
                await DisplayCurrentlyPlayingRequestAsync();

            do
            {
                await play(true);
            }
            while (Config.IsLoopEnabled);

            async Task play(bool retry)
            {
                using Process ffmpeg = DownloadService.CreateStream(request.VideoMetadata);
                using Stream output = ffmpeg.StandardOutput.BaseStream;
                using AudioOutStream discord = Config.AudioClient.CreatePCMStream(AudioApplication.Music);

                try
                {
                    // Sometimes you would get an expired YouTube audio stream even though its expiry date is in the future.
                    // This is a hacky workaround, retrying playback if the audio stream is empty (assumed by actual playback time)
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    await output.CopyToAsync(discord, cancellationToken);

                    stopwatch.Stop();
                    if (retry && stopwatch.ElapsedMilliseconds * 4 < Config.CurrentlyPlayingSongRequest.VideoMetadata.Duration * 1000)
                    {
                        Logger.Log($"Audio playback was too short for request '{Config.CurrentlyPlayingSongRequest.VideoMetadata.Title}' ({stopwatch.ElapsedMilliseconds}/{Config.CurrentlyPlayingSongRequest.VideoMetadata.Duration * 1000}ms) and the direct audio URL will be refetched.", LogSeverity.Info);
                        Config.CurrentlyPlayingSongRequest.VideoMetadata.DirectAudioUrl = await YouTubeService.GetYoutubeAudioUrlAsync(Config.CurrentlyPlayingSongRequest.VideoMetadata.Id);
                        await play(false);
                    }
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
            StopPlaying();

            if (CheckIfAudioClientDisconnected(Config.AudioClient))
                return;

            var users = Context.Guild.GetUser(Context.Client.CurrentUser.Id)?.VoiceChannel?.Users;
            if (users?.Count < 2)
            {
                StopPlaying();
                await Config.AudioClient.StopAsync();
                await new InfoMessage("I left the voice channel since there's nobody here to listen.")
                    .SendAsync(Context.Channel);
                return;
            }

            if (Config.PlayNowRequest != null)
            {
                var playNow = Config.PlayNowRequest;
                Config.PlayNowRequest = null;
                await PlayRequestAsync(playNow, Config.CtsForAudioStreaming.Token);
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