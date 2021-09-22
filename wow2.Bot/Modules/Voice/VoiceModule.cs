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

namespace wow2.Bot.Modules.Voice
{
    [Name("Voice")]
    [Group("vc")]
    [Alias("voice")]
    [Summary("Play YouTube or Twitch audio in a voice channel.")]
    public class VoiceModule : Module
    {
        public VoiceModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].Voice;

        public IYoutubeModuleService YouTubeService { get; set; }

        public static bool CheckIfAudioClientDisconnected(IAudioClient audioClient)
            => audioClient == null || audioClient?.ConnectionState == ConnectionState.Disconnected;

        // Doesn't display hours if less than 1 hour.
        public static string DurationAsString(double? duration) =>
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

            List<VideoMetadata> metadataList;
            try
            {
                metadataList = await DownloadService.GetMetadataAsync(songRequest);
            }
            catch (ArgumentException ex)
            {
                throw new CommandReturnException(Context, $"{ex.Message}", "Invalid input");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Could not fetch video metadata");
                await new ErrorMessage("One or more errors were returned.", "Could not fetch video metadata")
                    .SendAsync(Context.Channel);
                return;
            }

            if (metadataList.Count > 1)
            {
                await new QuestionMessage(
                    description: "Do you want to clear the current queue first?",
                    title: "You are adding a playlist",
                    onConfirm: async () =>
                    {
                        Config.CurrentSongRequestQueue.Clear();
                        StopPlaying(Config);
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
                {
                    Config.CurrentSongRequestQueue.Enqueue(new UserSongRequest()
                    {
                        VideoMetadata = metadata,
                        TimeRequested = DateTime.Now,
                        RequestedBy = Context.User,
                    });
                }

                string successText = metadataList.Count > 1 ?
                    $"Added a playlist with {metadataList.Count} items.\nYou can clear this queue with `{Context.Guild.GetCommandPrefix()} vc clear`, or save it for later with `{Context.Guild.GetCommandPrefix()} vc save-queue [NAME]`" :
                    $"Added song request to the number `{Config.CurrentSongRequestQueue.Count}` spot in the queue:\n\n**{metadataList[0].title}**\n{metadataList[0].webpage_url}";

                bool isDisconnected = CheckIfAudioClientDisconnected(Config.AudioClient);
                if (isDisconnected && !Config.IsAutoJoinOn)
                {
                    await new SuccessMessage($"{successText}\n\n**You have `toggle-auto-join` turned off, **so if you want me to join the voice channel you'll have to type `{Context.Guild.GetCommandPrefix()} vc join`")
                        .SendAsync(Context.Channel);
                }
                else
                {
                    await new SuccessMessage(successText)
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
            StopPlaying(Config);

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
        [Alias("pop", "popqueue")]
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
        public async Task SetVoteSkipsNeededAsync([Name("NUMBER")] int newNumberOfSkips)
        {
            if (newNumberOfSkips > Context.Guild.MemberCount)
                throw new CommandReturnException(Context, "The number of votes required is greater than the amount of people in the server.", "Number too large");

            newNumberOfSkips = Math.Max(newNumberOfSkips, 1);
            Config.VoteSkipsNeeded = newNumberOfSkips;
            await new SuccessMessage($"`{newNumberOfSkips}` votes are now required to skip a song request.")
                .SendAsync(Context.Channel);
        }

        private static void StopPlaying(VoiceModuleConfig config)
        {
            config.ListOfUserIdsThatVoteSkipped.Clear();
            config.CtsForAudioStreaming.Cancel();
            config.CtsForAudioStreaming = new CancellationTokenSource();
            config.CurrentlyPlayingSongRequest = null;
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
                await new NowPlayingMessage(Config.CurrentlyPlayingSongRequest, Config, skipButton)
                    .SendAsync(Context.Channel);
            }
            catch (Exception ex)
            {
                string errorText = $"Displaying metadata failed for the following video:\n{Config.CurrentlyPlayingSongRequest?.VideoMetadata?.webpage_url}";
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

                if (Config.ListOfUserIdsThatVoteSkipped.Count + 1 < Config.VoteSkipsNeeded)
                {
                    if (Config.ListOfUserIdsThatVoteSkipped.Contains(component.User.Id))
                    {
                        await component.FollowupAsync(embed: new WarningMessage("You've already sent a skip request.").Embed, ephemeral: true);
                        return;
                    }

                    Config.ListOfUserIdsThatVoteSkipped.Add(component.User.Id);
                    await new InfoMessage(
                        description: $"Waiting for `{Config.VoteSkipsNeeded - Config.ListOfUserIdsThatVoteSkipped.Count}` more vote(s) before skipping.\n",
                        title: $"Sent skip request for {component.User.Mention}")
                            .SendAsync(Context.Channel);
                    return;
                }
                else
                {
                    await new InfoMessage($"Skipping request on behalf of {component.User.Mention}")
                        .SendAsync(Context.Channel);
                    Config.IsLoopEnabled = false;
                    _ = ContinueAsync();
                }
            }
        }

        private async Task PlayRequestAsync(UserSongRequest request, CancellationToken cancellationToken)
        {
            if (request.VideoMetadata.LookupTitleOnYoutube)
            {
                try
                {
                    var search = await YouTubeService.SearchForAsync(request.VideoMetadata.title, "videos");
                    request.VideoMetadata = new VideoMetadata(await YouTubeService.GetVideoAsync(search.Id.VideoId))
                    {
                        // Remember what the original source was.
                        extractor = request.VideoMetadata.extractor,
                    };
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Lookup failed for {request.VideoMetadata.title}");
                    await new ErrorMessage($"```{request.VideoMetadata.title}```", "Couldn't lookup song.")
                        .SendAsync(Context.Channel);
                    _ = ContinueAsync();
                    return;
                }
            }

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

            var users = Context.Guild.GetUser(Context.Client.CurrentUser.Id)?.VoiceChannel?.Users;
            if (users?.Count < 2)
            {
                StopPlaying(Config);
                await Config.AudioClient.StopAsync();
                await new InfoMessage("I left the voice channel since there's nobody here to listen.")
                    .SendAsync(Context.Channel);
                return;
            }

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