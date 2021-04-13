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
using wow2.Verbose.Messages;

namespace wow2.Modules.Voice
{
    [Name("Voice")]
    [Group("vc")]
    [Alias("voice")]
    [Summary("For playing Youtube/Twitch audio in a voice channel.")]
    public class VoiceModule : ModuleBase<SocketCommandContext>
    {
        [Command("list")]
        [Alias("queue", "upnext")]
        [Summary("Show the song request queue.")]
        public async Task ListAsync(int page = 1)
        {
            var config = GetConfigForGuild(Context.Guild);

            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            int i = 0;
            foreach (UserSongRequest songRequest in config.SongRequests)
            {
                i++;

                var fieldBuilderForSongRequest = new EmbedFieldBuilder()
                {
                    Name = $"{i}) {songRequest.VideoMetadata.title}",
                    Value = $"{songRequest.VideoMetadata.webpage_url}\nRequested at {songRequest.TimeRequested.ToString("HH:mm")} by {songRequest.RequestedBy.Mention}"
                };
                listOfFieldBuilders.Add(fieldBuilderForSongRequest);
            }

            if (listOfFieldBuilders.Count == 0)
                throw new CommandReturnException(Context, "There's nothing in the queue... how sad.");

            await new GenericMessage(
                title: "Up Next",
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

            config.SongRequests.Clear();
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
                metadata = await YoutubeDl.GetMetadata(songRequest);
            }
            catch (ArgumentException)
            {
                throw new CommandReturnException(Context, $"One or more errors were returned.", "Could not fetch video metadata");
            }
            catch
            {
                throw new CommandReturnException(Context, $"The host may be missing some required dependencies.", "Could not fetch video metadata");
            }

            config.SongRequests.Enqueue(new UserSongRequest()
            {
                VideoMetadata = metadata,
                TimeRequested = DateTime.Now,
                RequestedBy = Context.User
            });

            await new SuccessMessage($"Added song request to the number `{config.SongRequests.Count}` spot in the queue:\n\n**{metadata.title}**\n{metadata.webpage_url}")
                .SendAsync(Context.Channel);

            // Play song if nothing else is playing.
            if (!CheckIfAudioClientDisconnected(config.AudioClient) && config.CurrentlyPlayingSongRequest == null)
                _ = ContinueAsync();
        }

        [Command("skip")]
        [Alias("next")]
        [Summary("Stops the currently playing request and starts the next request if it exists.")]
        public async Task SkipAsync()
        {
            var config = GetConfigForGuild(Context.Guild);

            if (config.CurrentlyPlayingSongRequest == null)
                throw new CommandReturnException(Context, "There's nothing playing right now.");

            if (config.ListOfUserIdsThatVoteSkipped.Count() + 1 < config.VoteSkipsNeeded)
            {
                if (config.ListOfUserIdsThatVoteSkipped.Contains(Context.User.Id))
                    throw new CommandReturnException(Context, "You've already sent a skip request.");

                config.ListOfUserIdsThatVoteSkipped.Add(Context.User.Id);
                await new InfoMessage(
                    description: $"Waiting for `{config.VoteSkipsNeeded - config.ListOfUserIdsThatVoteSkipped.Count()}` more vote(s) before skipping.\n",
                    title: "Sent skip request")
                        .SendAsync(Context.Channel);
                return;
            }
            else
            {
                // Required number of vote skips has been met. 
                _ = ContinueAsync();
            }
        }

        [Command("join")]
        [Summary("Joins the voice channel of the person that executed the command.")]
        public async Task JoinAsync()
        {
            var config = GetConfigForGuild(Context.Guild);
            IVoiceChannel voiceChannelToJoin = ((IGuildUser)Context.User).VoiceChannel ?? null;

            if (!CheckIfAudioClientDisconnected(config.AudioClient))
            {
                IGuildUser guildUser = await Program.GetClientGuildUserAsync(Context.Channel);
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
                // No need to notify the user of these exceptions.
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

        [Command("toggle-auto-np")]
        [Summary("Toggles whether the np command will be executed everytime a new song is playing.")]
        public async Task ToggleLikeReactionAsync()
        {
            GetConfigForGuild(Context.Guild).IsAutoNpOn = !GetConfigForGuild(Context.Guild).IsAutoNpOn;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"Auto execution of `vc np` is turned `{(GetConfigForGuild(Context.Guild).IsAutoNpOn ? "on" : "off")}`")
                .SendAsync(Context.Channel);
        }

        [Command("set-vote-skips-needed")]
        [Summary("Sets the number of votes needed to skip a song request to NUMBER.")]
        public async Task SetVoteSkipsNeeded([Name("NUMBER")] int newNumberOfSkips)
        {
            if (newNumberOfSkips > Context.Guild.MemberCount)
                throw new CommandReturnException(Context, "The number of votes required is greater than the amount of people in the server.", "Number too large");

            newNumberOfSkips = Math.Max(newNumberOfSkips, 1);
            GetConfigForGuild(Context.Guild).VoteSkipsNeeded = newNumberOfSkips;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"`{newNumberOfSkips}` votes are now required to skip a song request.")
                .SendAsync(Context.Channel);
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

            using (var ffmpeg = YoutubeDl.CreateStreamFromVideoUrl(request.VideoMetadata.webpage_url))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = config.AudioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                try
                {
                    config.CurrentlyPlayingSongRequest = request;
                    if (config.IsAutoNpOn)
                        await DisplayCurrentlyPlayingRequestAsync();

                    await output.CopyToAsync(discord, cancellationToken);
                }
                finally
                {
                    await discord.FlushAsync();
                }
            }

            _ = ContinueAsync();
        }

        /// <summary>Continue to the next song request, if it exists. Otherwise notify the user that the queue is empty.</summary>
        private async Task ContinueAsync()
        {
            var config = GetConfigForGuild(Context.Guild);
            UserSongRequest nextRequest;

            StopPlaying(config);

            if (CheckIfAudioClientDisconnected(config.AudioClient))
                return;

            if (config.SongRequests.TryDequeue(out nextRequest))
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

        private void StopPlaying(VoiceModuleConfig config)
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

            var authorBuilder = new EmbedAuthorBuilder()
            {
                Name = "Now Playing",
                IconUrl = request.VideoMetadata.extractor.StartsWith("twitch") ? twitchIconUrl : youtubeIconUrl,
                Url = request.VideoMetadata.webpage_url
            };
            var footerBuilder = new EmbedFooterBuilder()
            {
                Text = request.VideoMetadata.extractor.StartsWith("youtube") ?
                    $"👁️  {request.VideoMetadata.view_count ?? 0}      |      👍  {request.VideoMetadata.like_count ?? 0}      |      👎  {request.VideoMetadata.dislike_count ?? 0}      |      🕓  {formattedDuration}" : ""
            };

            var embedBuilder = new EmbedBuilder()
            {
                Author = authorBuilder,
                Title = request.VideoMetadata.extractor == "twitch:stream" ? $"*(LIVE)* {request.VideoMetadata.description}" : request.VideoMetadata.title,
                ThumbnailUrl = request.VideoMetadata.thumbnails.LastOrDefault().url,
                Description = $"Requested at {request.TimeRequested.ToString("HH:mm")} by {request.RequestedBy.Mention}",
                Footer = footerBuilder,
                Color = Color.LightGrey
            };

            return embedBuilder.Build();
        }

        private bool CheckIfAudioClientDisconnected(IAudioClient audioClient)
            => audioClient == null || audioClient?.ConnectionState == ConnectionState.Disconnected;

        private static VoiceModuleConfig GetConfigForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Voice;
    }
}