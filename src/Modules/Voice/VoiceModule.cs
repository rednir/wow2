using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Audio;

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
        public async Task ListAsync()
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);

            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            int i = 0;
            foreach (UserSongRequest songRequest in config.SongRequests)
            {
                var fieldBuilderForSongRequest = new EmbedFieldBuilder()
                {
                    Name = $"{i + 1}) {songRequest.VideoMetadata.title}",
                    Value = $"Requested by {songRequest.Author.Username} at `{songRequest.TimeRequested.ToString("HH:mm")}`"
                };
                listOfFieldBuilders.Add(fieldBuilderForSongRequest);
                i++;
            }

            await ReplyAsync(
                embed: MessageEmbedPresets.Fields(listOfFieldBuilders, "Up Next")
            );
        }

        [Command("clear")]
        [Alias("empty", "remove", "reset")]
        public async Task ClearAsync()
        {
            DataManager.GetVoiceConfigForGuild(Context.Guild).SongRequests.Clear();
            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"The song request queue has been cleared.")
            );
        }

        [Command("add", RunMode = RunMode.Async)]
        [Alias("play")]
        public async Task AddAsync([Name("request")] params string[] splitSongRequest)
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
                Author = Context.User
            });

            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"Added song request to the number `{config.SongRequests.Count}` spot in the queue:\n\n**{metadata.title}**\n{metadata.webpage_url}")
            );

            if (!CheckIfAudioClientDisconnected(config))
                _ = ContinueAsync();
        }

        // TODO: return if joining a vc that the bot is already in.
        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinAsync()
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);
            IVoiceChannel voiceChannel = ((IGuildUser)Context.User).VoiceChannel ?? null;

            try
            {
                config.AudioClient = await voiceChannel.ConnectAsync();
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
        public async Task LeaveAsync()
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);

            if (config.AudioClient == null || config.AudioClient?.ConnectionState == ConnectionState.Disconnected)
                throw new CommandReturnException("I'm not currently in a voice channel.", Context);

            await config.AudioClient.StopAsync();
        }

        private async Task StreamAudioAsync(string url)
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);

            using (var ffmpeg = CreateStream(url))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = config.AudioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await output.CopyToAsync(discord); }
                finally { await discord.FlushAsync(); }
            }

            _ = ContinueAsync();
        }

        private async Task ContinueAsync()
        {
            Console.WriteLine("cont");
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);
            UserSongRequest nextRequest;

            if (config.SongRequests.TryDequeue(out nextRequest))
            {
                Console.WriteLine("bout play " + nextRequest.VideoMetadata.title);
                await StreamAudioAsync(nextRequest.VideoMetadata.webpage_url);
            }
            else
            {
                // queue is empty so disconnect?
            }
        }

        private Process CreateStream(string url)
        {
            return Process.Start(new ProcessStartInfo
            {
                // TODO: Requiring bash is not ideal
                FileName = "bash",
                Arguments = $"-c \"youtube-dl '{url}' -o - | ffmpeg -hide_banner -loglevel panic -i - -ac 2 -f s16le -ar 48000 pipe:1\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private bool CheckIfAudioClientDisconnected(VoiceModuleConfig config)
            => config.AudioClient == null || config.AudioClient?.ConnectionState == ConnectionState.Disconnected;
    }
}