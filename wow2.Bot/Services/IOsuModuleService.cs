using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wow2.Bot.Modules.Osu;

namespace wow2.Bot.Services
{
    public interface IOsuModuleService
    {
        Task<UserData> GetUserAsync(string user, string mode = null);

        Task<Score[]> GetUserScoresAsync(ulong userId, string type, string mode = null);

        Task<Score> GetScoreAsync(ulong id, string mode);

        Task CheckForUserMilestonesAsync();
    }
}