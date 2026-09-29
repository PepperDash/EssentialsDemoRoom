using System.Collections.Generic;
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

        /// <summary>
        /// Tech menu config: the framework's tech password plus the demo's System Status devices.
        /// </summary>
        /// <remarks>
        /// Shadows the base <see cref="EssentialsRoomPropertiesConfig.Tech"/> so the "tech" object
        /// deserializes as <see cref="DemoRoomTechConfig"/>, while still reading/writing the base
        /// property so anything using the base type sees the same instance.
        /// </remarks>
        [JsonProperty("tech")]
        public new DemoRoomTechConfig Tech
        {
            get => base.Tech as DemoRoomTechConfig;
            set => base.Tech = value;
        }
    }

    /// <summary>
    /// The "tech" room config object, extended with demo-specific System Status settings.
    /// </summary>
    public class DemoRoomTechConfig : EssentialsRoomTechConfig
    {
        /// <summary>
        /// Device keys shown on the tech System Status page, in display order. Each must implement
        /// <see cref="PepperDash.Essentials.Core.ICommunicationMonitor"/> - there's no framework
        /// concept of "the devices to monitor" for a room, so this is how the demo tells the client
        /// which ones to ask about.
        /// </summary>
        [JsonProperty("systemStatusDeviceKeys", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> SystemStatusDeviceKeys { get; set; } = new List<string>();

        /// <summary>
        /// Device key of the rack sensor (<see cref="PepperDash.Essentials.Core.DeviceTypeInterfaces.ITemperatureSensor"/>/
        /// <see cref="PepperDash.Essentials.Core.DeviceTypeInterfaces.IHumiditySensor"/>) shown on
        /// the tech System Status page's "Rack Temp"/"Rack Humidity" readings.
        /// </summary>
        [JsonProperty("rackSensorDeviceKey", NullValueHandling = NullValueHandling.Ignore)]
        public string RackSensorDeviceKey { get; set; }

        /// <summary>
        /// Displays shown on the tech Displays page, in display order. There's no framework concept
        /// of "the displays in a room" beyond the destination list (which only covers routing
        /// destinations, not every controllable display), so this is how the demo tells the client
        /// which devices to offer and, for a display with a projector screen/lift, which companion
        /// devices back its extra controls.
        /// </summary>
        [JsonProperty("displays", NullValueHandling = NullValueHandling.Ignore)]
        public List<DemoRoomTechDisplayConfig> Displays { get; set; } = new List<DemoRoomTechDisplayConfig>();

        /// <summary>
        /// Device key of the matrix router shown on the tech Routing page. Must implement
        /// <see cref="PepperDash.Essentials.Core.IHasNamedRoutingSlots"/> - there's no framework
        /// concept of "the room's tech-level matrix router", so this is how the demo tells the client
        /// which device to ask about.
        /// </summary>
        [JsonProperty("routingDeviceKey", NullValueHandling = NullValueHandling.Ignore)]
        public string RoutingDeviceKey { get; set; }
    }

    /// <summary>
    /// One entry in <see cref="DemoRoomTechConfig.Displays"/>.
    /// </summary>
    public class DemoRoomTechDisplayConfig
    {
        /// <summary>
        /// Device key of the display itself. Must implement
        /// <see cref="PepperDash.Essentials.Core.IHasPowerControlWithFeedback"/> and
        /// <see cref="PepperDash.Essentials.Core.DeviceTypeInterfaces.IHasInputs{T}"/> (of <c>string</c>).
        /// </summary>
        [JsonProperty("deviceKey")]
        public string DeviceKey { get; set; }

        /// <summary>
        /// Device key of this display's projector screen (<see cref="PepperDash.Essentials.Core.DeviceTypeInterfaces.IProjectorScreenLiftControl"/>),
        /// if it has one.
        /// </summary>
        [JsonProperty("screenDeviceKey", NullValueHandling = NullValueHandling.Ignore)]
        public string ScreenDeviceKey { get; set; }

        /// <summary>
        /// Device key of this display's projector lift (<see cref="PepperDash.Essentials.Core.DeviceTypeInterfaces.IProjectorScreenLiftControl"/>),
        /// if it has one.
        /// </summary>
        [JsonProperty("liftDeviceKey", NullValueHandling = NullValueHandling.Ignore)]
        public string LiftDeviceKey { get; set; }
    }
}
