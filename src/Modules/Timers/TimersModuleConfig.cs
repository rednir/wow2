using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace wow2.Modules.Timers
{
    public class TimersModuleConfig
    {
        [JsonIgnore]
        public List<UserTimer> UserTimers { get; } = new();
    }
}