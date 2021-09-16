using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Timers;

namespace wow2.Bot.Modules.ServerIcon
{
    // TODO: change the names of these properties to be less redundant.
    public class ServerIconModuleConfig
    {
        [JsonIgnore]
        public Timer IconRotateTimer { get; set; }

        public double? IconRotateTimerInterval { get; set; }

        public List<Icon> IconsToRotate { get; set; } = new();

        public int IconsToRotateIndex { get; set; }

        /// <summary>Gets or sets the next planned icon rotate. Used when initializing to make sure the timer picks up where it left off.</summary>
        public DateTime NextPlannedRotate { get; set; }
    }
}