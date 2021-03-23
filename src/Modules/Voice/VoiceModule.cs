using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
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
                throw new CommandReturnException(Context, "There's nothing in the queue... how sad.");

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

            if (splitSongRequest.Length == 0)
                throw new CommandReturnException(Context, "You must type a URL or a search term.");
            if (((SocketGuildUser)Context.User).VoiceChannel == null)
                throw new CommandReturnException(Context, "Join a voice channel first before adding song requests.");

            string songRequest = string.Join(" ", splitSongRequest);

            YoutubeVideoMetadata metadata;
            try
            {
                metadata = await YoutubeDl.GetMetadata(songRequest);
            }
            catch (Exception)
            {
                throw new CommandReturnException(Context, $"**Could not fetch video metadata.**\nThe host may be missing some required dependencies.");
            }

            config.SongRequests.Enqueue(new UserSongRequest()
            {
                VideoMetadata = metadata,
                TimeRequested = DateTime.Now,
                RequestedBy = Context.User
            });

            await Messenger.SendSuccessAsync(Context.Channel, $"Added song request to the number `{config.SongRequests.Count}` spot in the queue:\n\n**{metadata.title}**\n{metadata.webpage_url}");

            // Play song if it is the first in queue.
            if (!CheckIfAudioClientDisconnected(config.AudioClient) && config.CurrentlyPlayingSongRequest == null)
                _ = ContinueAsync();
        }

        [Command("skip")]
        [Alias("next")]
        [Summary("Stops the currently playing request and starts the next request if it exists.")]
        public Task Skip()
        {
            _ = ContinueAsync();
            return Task.CompletedTask;
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
                    throw new CommandReturnException(Context, "I'm already in this voice channel.");
            }

            try
            {
                config.AudioClient = await voiceChannelToJoin.ConnectAsync();
                _ = ContinueAsync();
            }
            catch (NullReferenceException)
            {
                throw new CommandReturnException(Context, "Join a voice channel first.");
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

            config.CurrentlyPlayingSongRequest = null;

            if (config.AudioClient == null || config.AudioClient?.ConnectionState == ConnectionState.Disconnected)
                throw new CommandReturnException(Context, "I'm not currently in a voice channel.");

            await config.AudioClient.StopAsync();
        }

        [Command("np")]
        [Alias("nowplaying")]
        [Summary("Shows details about the currently playing song request.")]
        public async Task NowPlayingAsync()
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);

            if (config.CurrentlyPlayingSongRequest == null || CheckIfAudioClientDisconnected(config.AudioClient))
                throw new CommandReturnException(Context, "Nothing is playing right now.");

            await DisplayCurrentlyPlayingRequestAsync();
        }

        private async Task DisplayCurrentlyPlayingRequestAsync()
        {
            UserSongRequest request = DataManager.GetVoiceConfigForGuild(Context.Guild).CurrentlyPlayingSongRequest;

            if (request == null) return;

            try
            {
                await ReplyAsync(embed: BuildNowPlayingEmbed(request));
            }
            catch
            {
                await Messenger.SendWarningAsync(Context.Channel, $"Displaying metadata failed for the following video:\n{request?.VideoMetadata?.webpage_url}");
            }
        }

        private async Task PlayRequestAsync(UserSongRequest request, CancellationToken cancellationToken)
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);

            using (var ffmpeg = CreateStreamFromYoutubeUrl(request.VideoMetadata.webpage_url))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = config.AudioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                try
                {
                    config.CurrentlyPlayingSongRequest = request;
                    await DisplayCurrentlyPlayingRequestAsync();
                    await output.CopyToAsync(discord, cancellationToken);
                }
                finally
                {
                    config.CurrentlyPlayingSongRequest = null;
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
                config.CurrentlyPlayingSongRequest = null;
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

        public static Embed BuildNowPlayingEmbed(UserSongRequest request)
        {
            var authorBuilder = new EmbedAuthorBuilder()
            {
                Name = "Now Playing",
                IconUrl = "https://cdn4.iconfinder.com/data/icons/social-messaging-ui-color-shapes-2-free/128/social-youtube-circle-512.png",
                Url = request.VideoMetadata.webpage_url
            };
            var footerBuilder = new EmbedFooterBuilder()
            {
                Text = $"👁️  {request.VideoMetadata.view_count ?? 0}      |      👍  {request.VideoMetadata.like_count ?? 0}      |      👎  {request.VideoMetadata.dislike_count ?? 0}"
            };

            var embedBuilder = new EmbedBuilder()
            {
                Author = authorBuilder,
                Title = request.VideoMetadata.title,
                ThumbnailUrl = request.VideoMetadata.thumbnails.LastOrDefault().url,
                Description = $"Requested at {request.TimeRequested.ToString("HH:mm")} by {request.RequestedBy.Mention}",
                Footer = footerBuilder,
                Color = Color.LightGrey
            };

            return embedBuilder.Build();
        }

        private bool CheckIfAudioClientDisconnected(IAudioClient audioClient)
            => audioClient == null || audioClient?.ConnectionState == ConnectionState.Disconnected;
    }
}