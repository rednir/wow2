using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Timers;

namespace wow2.Bot.Modules.ServerIcon
{
    public class ServerIconModuleConfig
    {
        [JsonIgnore]
        public Timer IconRotateTimer { get; set; }

        public double? IconRotateTimerInterval { get; set; }

        public List<Icon> IconsToRotate { get; set; } = new();

        public int IconsToRotateIndex { get; set; }
    }
}