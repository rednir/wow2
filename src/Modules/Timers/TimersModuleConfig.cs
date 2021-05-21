using System.Collections.Generic;

namespace wow2.Modules.Timers
{
    public class TimersModuleConfig
    {
        public List<UserTimer> UserTimers { get; } = new();
    }
}