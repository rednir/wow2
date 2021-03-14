using System;
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
                embed: MessageEmbedPresets.Verbose($"The song request queue has been cleared.", VerboseMessageSeverity.Info)
            );
        }

        [Command("add", RunMode = RunMode.Async)]
        [Alias("play")]
        public async Task AddAsync(params string[] splitSongRequest)
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);
            string songRequest = string.Join(" ", splitSongRequest);

            if (((SocketGuildUser)Context.User).VoiceChannel == null)
            {
                await ReplyAsync(
                    embed: MessageEmbedPresets.Verbose($"Join a voice channel first before adding song requests.", VerboseMessageSeverity.Warning)
                );
                return;
            }

            YoutubeVideoMetadata metadata;
            try
            {
                metadata = await YoutubeDl.GetMetadata(songRequest);
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(
                    embed: MessageEmbedPresets.Verbose($"The following error was thrown when trying to downloading video metadata:\n```{ex.Message}```", VerboseMessageSeverity.Warning)
                );
                return;
            }

            config.SongRequests.Enqueue(new UserSongRequest()
            {
                VideoMetadata = metadata,
                TimeRequested = DateTime.Now,
                Author = Context.User
            });

            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"Added song request to the number `{config.SongRequests.Count}` spot in the queue:\n\n**{metadata.title}**\n{metadata.webpage_url}", VerboseMessageSeverity.Info)
            );

            // TODO: if no song is playing and bot is in vc, play song.
        }

        // TODO: return if joining a vc that the bot is already in.
        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinAsync()
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);
            IVoiceChannel voiceChannel = ((IGuildUser)Context.User).VoiceChannel ?? null;
            
            try
            {
                config.AudioClient = await voiceChannel.ConnectAsync(true);
            }
            catch (NullReferenceException)
            {
                await ReplyAsync(
                    embed: MessageEmbedPresets.Verbose($"Join a voice channel first.", VerboseMessageSeverity.Warning)
                );
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
            {
                await ReplyAsync(
                    embed: MessageEmbedPresets.Verbose($"I'm not currently in a voice channel.", VerboseMessageSeverity.Warning)
                );
                return;
            }

            await config.AudioClient.StopAsync();
        }
    }
}