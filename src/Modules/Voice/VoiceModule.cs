using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Audio;
using wow2.Data;
using wow2.Verbose;

namespace wow2.Modules.Voice
{
    [Name("Voice")]
    [Group("vc")]
    [Alias("voice")]
    [Summary("For playing Youtube audio in a voice channel.")]
    public class VoiceModule : ModuleBase<SocketCommandContext>
    {
        [Command("list")]
        [Alias("queue", "upnext")]
        [Summary("Show the song request queue.")]
        public async Task ListAsync()
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);

            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            int i = 0;
            foreach (UserSongRequest songRequest in config.SongRequests)
            {
                i++;

                var fieldBuilderForSongRequest = new EmbedFieldBuilder()
                {
                    Name = $"{i}) {songRequest.VideoMetadata.title}",
                    Value = $"Requested by `{songRequest.RequestedBy.Username}` at `{songRequest.TimeRequested.ToString("HH:mm")}`"
                };
                listOfFieldBuilders.Add(fieldBuilderForSongRequest);
            }

            if (listOfFieldBuilders.Count == 0)
                throw new CommandReturnException("There's nothing in the queue... how sad.", Context);

            await ReplyAsync(
                embed: Messenger.Fields(listOfFieldBuilders, "Up Next")
            );
        }

        [Command("clear")]
        [Alias("empty", "remove", "reset")]
        [Summary("Clears the song request queue.")]
        public async Task ClearAsync()
        {
            DataManager.GetVoiceConfigForGuild(Context.Guild).SongRequests.Clear();
            await Messenger.SendSuccessAsync(Context.Channel, $"The song request queue has been cleared.");
        }

        [Command("add", RunMode = RunMode.Async)]
        [Alias("play")]
        [Summary("Adds REQUEST to the song request queue. REQUEST can be a video URL or a search term.")]
        public async Task AddAsync([Name("REQUEST")] params string[] splitSongRequest)
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);
            string songRequest = string.Join(" ", splitSongRequest);

            if (((SocketGuildUser)Context.User).VoiceChannel == null)
                throw new CommandReturnException("Join a voice channel first before adding song requests.", Context);

            YoutubeVideoMetadata metadata;
            try
            {
                metadata = await YoutubeDl.GetMetadata(songRequest);
            }
            catch (ArgumentException ex)
            {
                throw new CommandReturnException($"The following error was thrown when trying to downloading video metadata:\n```{ex.Message}```", Context);
            }

            config.SongRequests.Enqueue(new UserSongRequest()
            {
                VideoMetadata = metadata,
                TimeRequested = DateTime.Now,
                RequestedBy = Context.User
            });

            await Messenger.SendSuccessAsync(Context.Channel, $"Added song request to the number `{config.SongRequests.Count}` spot in the queue:\n\n**{metadata.title}**\n{metadata.webpage_url}");

            // Play song if it is the first in queue.
            if (!CheckIfAudioClientDisconnected(config.AudioClient) && !config.IsCurrentlyPlayingAudio)
                _ = ContinueAsync();
        }

        [Command("skip")]
        [Alias("next")]
        [Summary("Stops the currently playing request and starts the next request if it exists.")]
        public async Task Skip()
        {
            await Messenger.SendInfoAsync(Context.Channel, "Skipping to the next request.");
            _ = ContinueAsync();
        }

        [Command("join", RunMode = RunMode.Async)]
        [Summary("Joins the voice channel of the person that executed the command.")]
        public async Task JoinAsync()
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);
            IVoiceChannel voiceChannelToJoin = ((IGuildUser)Context.User).VoiceChannel ?? null;

            if (!CheckIfAudioClientDisconnected(config.AudioClient))
            {
                IGuildUser guildUser = await Program.GetClientGuildUserAsync(Context);
                if (guildUser.VoiceChannel == voiceChannelToJoin)
                    throw new CommandReturnException("I'm already in this voice channel.", Context);
            }

            try
            {
                config.AudioClient = await voiceChannelToJoin.ConnectAsync();
                _ = ContinueAsync();
            }
            catch (NullReferenceException)
            {
                throw new CommandReturnException("Join a voice channel first.", Context);
            }
            catch (Exception ex) when (ex is WebSocketClosedException || ex is TaskCanceledException)
            {
            }
        }

        [Command("leave")]
        [Summary("Leaves the voice channel.")]
        public async Task LeaveAsync()
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);

            // Just in case.
            config.IsCurrentlyPlayingAudio = false;

            if (config.AudioClient == null || config.AudioClient?.ConnectionState == ConnectionState.Disconnected)
                throw new CommandReturnException("I'm not currently in a voice channel.", Context);

            await config.AudioClient.StopAsync();
        }

        [Command("np")]
        [Alias("nowplaying")]
        [Summary("Shows details about the currently playing song request.")]
        public async Task NowPlayingAsync()
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);

            if (config.SongRequests.Count == 0 || CheckIfAudioClientDisconnected(config.AudioClient))
                throw new CommandReturnException("Nothing is playing right now.", Context);

            await DisplayNowPlayingRequestAsync(config.SongRequests.Peek());
        }

        private async Task DisplayNowPlayingRequestAsync(UserSongRequest request)
        {
            await ReplyAsync(
                embed: Messenger.NowPlaying(
                    title: request.VideoMetadata.title,
                    url: request.VideoMetadata.webpage_url,
                    thumbnailUrl: request.VideoMetadata.thumbnails[1]?.url,

                    timeRequested: request.TimeRequested,
                    requestedBy: request.RequestedBy,

                    viewCount: request.VideoMetadata.view_count,
                    likeCount: request.VideoMetadata.like_count,
                    dislikeCount: request.VideoMetadata.dislike_count
                )
            );
        }

        private async Task PlayRequestAsync(UserSongRequest request, CancellationToken cancellationToken)
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);
            await DisplayNowPlayingRequestAsync(request);

            using (var ffmpeg = CreateStreamFromYoutubeUrl(request.VideoMetadata.webpage_url))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = config.AudioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                try
                {
                    config.IsCurrentlyPlayingAudio = true;
                    await output.CopyToAsync(discord, cancellationToken);
                }
                finally
                {
                    config.IsCurrentlyPlayingAudio = false;
                    await discord.FlushAsync();
                }
            }

            _ = ContinueAsync();
        }

        private async Task ContinueAsync()
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);
            UserSongRequest nextRequest;

            config.CtsForAudioStreaming.Cancel();
            config.CtsForAudioStreaming = new CancellationTokenSource();

            if (config.SongRequests.TryDequeue(out nextRequest))
            {
                await PlayRequestAsync(nextRequest, config.CtsForAudioStreaming.Token);
            }
            else
            {
                await Messenger.SendInfoAsync(Context.Channel, "**The queue is empty.**\nI'll stay in the voice channel... in silence...");
            }
        }

        private Process CreateStreamFromYoutubeUrl(string url)
        {
            string shellCommand = $"{YoutubeDl.YoutubeDlPath} {url} -o - | {YoutubeDl.FFmpegPath} -hide_banner -loglevel panic -i - -ac 2 -f s16le -ar 48000 pipe:1";
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            return Process.Start(new ProcessStartInfo
            {
                FileName = isWindows ? "cmd" : "bash",
                Arguments = $"{(isWindows ? "/c" : "-c")} \"{shellCommand}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private bool CheckIfAudioClientDisconnected(IAudioClient audioClient)
            => audioClient == null || audioClient?.ConnectionState == ConnectionState.Disconnected;
    }
}