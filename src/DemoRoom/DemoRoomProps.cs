using Newtonsoft.Json;
using PepperDash.Essentials.Room.Config;

namespace EssentialsDemoRoom
{
    /// <summary>
    /// Configuration properties for the Essentials v3 demo room.
    /// </summary>
    /// <remarks>
    /// Inherits the standard AV room properties (source/destination/audio-control-point list keys,
    /// default audio key, shutdown timings, tech password, etc.) from
    /// <see cref="EssentialsAvRoomPropertiesConfig"/>. Only demo-specific additions belong here.
    /// </remarks>
    public class DemoRoomProps : EssentialsAvRoomPropertiesConfig
    {
        /// <summary>
        /// Destination-list item key for the room's default display. When unset, the room falls back to
        /// the "defaultDisplay" destination and then to the first destination in the list.
        /// </summary>
        [JsonProperty("defaultDisplayKey", NullValueHandling = NullValueHandling.Ignore)]
        public string DefaultDisplayKey { get; set; }

        /// <summary>
        /// When true, routing video to a non-program-audio destination also routes audio to the
        /// program audio destination.
        /// </summary>
        [JsonProperty("enableAudioFollowsVideo")]
        public bool EnableAudioFollowsVideo { get; set; } = true;
    }
}
