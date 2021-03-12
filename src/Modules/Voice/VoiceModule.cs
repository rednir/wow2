using System.Threading.Tasks;
using Discord.Commands;

namespace wow2.Modules.Voice
{
    [Name("Voice")]
    [Group("vc")]
    [Alias("voice")]
    [Summary("For playing Youtube audio in a voice channel.")]
    public class VoiceModule : ModuleBase<SocketCommandContext>
    {
        [Command("add")]
        [Alias("play")]
        public async Task AddAsync(params string[] splitSongRequest)
        {
            var config = DataManager.GetVoiceConfigForGuild(Context.Guild);
            string songRequest = string.Join(" ", splitSongRequest);

            config.SongRequests.Enqueue(new UserSongRequest() {
                Author = Context.User
            });
        }
    }
}