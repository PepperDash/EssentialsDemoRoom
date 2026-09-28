using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.RoomBridges;

namespace EssentialsDemoRoom
{
    /// <summary>
    /// Supplements the room's Mobile Control state, on top of what Essentials already wires up
    /// automatically.
    /// </summary>
    /// <remarks>
    /// Almost everything a room needs - routing (<c>/source</c>, <c>/directRoute</c>,
    /// <c>/defaultsource</c>), volume (<c>/volumes/master/*</c>), tech password validation,
    /// warming/cooling, and the room's own <c>fullStatus</c> push - is already provided by
    /// <see cref="MobileControlEssentialsRoomBridge"/>, auto-attached to any device implementing
    /// <see cref="PepperDash.Essentials.Core.IEssentialsRoom"/> via
    /// <c>AddDefaultMessengersForDevice</c> (see <c>DemoRoom.CreateMobileControlMessengers</c>,
    /// which calls <c>base.CreateMobileControlMessengers()</c> to trigger it). That bridge and this
    /// messenger deliberately share the same <c>/room/{key}</c> message path so their state pushes
    /// merge into one client-side room-state object and their registered actions route correctly.
    ///
    /// This messenger exists only for the few things the framework bridge does not cover:
    /// - <c>roomType</c>, so the client can discriminate room types if a second one is ever added.
    /// - <c>configuration.defaultDisplayKey</c> and <c>configuration.defaultPresentationSourceKey</c>,
    ///   both declared on the framework's own <c>RoomConfiguration</c> type but never populated by
    ///   it - this fills that gap rather than duplicating the fields it already sends.
    /// - <c>selectedSourceKey</c>, declared on the base <c>RoomStateMessage</c> too but always sent
    ///   as a hardcoded null by the framework bridge - neither it nor <c>RunRouteActionMessenger</c>
    ///   (which only handles the inbound <c>/source</c> action, no outbound state) track it.
    ///   <see cref="DemoRoom.SelectedSourceKey"/> fills that in for this room's "basic mode" - one
    ///   source selection for the whole room.
    /// - <c>techSystemStatusDeviceKeys</c> and <c>techRackSensorDeviceKey</c>, which tell the client
    ///   which devices to ask about for the tech System Status page - there's no framework concept
    ///   of "the devices to monitor" for a room, so these are demo-specific config
    ///   (<see cref="DemoRoomTechConfig.SystemStatusDeviceKeys"/>/<see cref="DemoRoomTechConfig.RackSensorDeviceKey"/>),
    ///   not something the base bridge could populate.
    /// - <c>techDisplays</c>, which tells the client which devices back the tech Displays page and,
    ///   for a display with a projector screen/lift, which companion devices drive those extra
    ///   controls (<see cref="DemoRoomTechConfig.Displays"/>) - same reasoning as the System Status
    ///   keys above.
    /// - <c>techRoutingDeviceKey</c>, which tells the client which device backs the tech Routing page
    ///   (<see cref="DemoRoomTechConfig.RoutingDeviceKey"/>) - same reasoning again.
    ///
    /// Deliberately NOT sent: <c>configuration.techPassword</c>. That field exists on
    /// <see cref="RoomConfiguration"/> too, but publishing the literal PIN to every connected
    /// client would defeat the point of gating the tech menu behind it -
    /// <see cref="PepperDash.Essentials.Core.ITechPassword"/>'s auto-wired messenger already
    /// handles validation without ever exposing the value.
    /// </remarks>
    public class DemoRoomMessenger : MessengerBase
    {
        private readonly DemoRoom room;

        public DemoRoomMessenger(string key, string messagePath, DemoRoom room)
            : base(key, messagePath, room)
        {
            this.room = room;

            this.room.SelectedSourceKeyChanged += (sender, args) => PostSelectedSourceKey();
        }

        protected override void RegisterActions()
        {
            // The framework bridge (sharing this same message path) also registers /fullStatus,
            // independently, for its own fields - both fire and their pushes merge client-side.
            AddAction("/fullStatus", (id, content) => SendFullStatus(id));

            base.RegisterActions();
        }

        private void SendFullStatus(string id = null)
        {
            var message = new DemoRoomStateMessage
            {
                RoomType = room.RoomType,
                SelectedSourceKey = room.SelectedSourceKey,
                TechSystemStatusDeviceKeys = room.Props.Tech?.SystemStatusDeviceKeys,
                TechRackSensorDeviceKey = room.Props.Tech?.RackSensorDeviceKey,
                TechDisplays = room.Props.Tech?.Displays,
                TechRoutingDeviceKey = room.Props.Tech?.RoutingDeviceKey,
                Configuration = new RoomConfiguration
                {
                    DefaultDisplayKey = room.DefaultDisplay?.Key,
                    DefaultPresentationSourceKey = room.Props.DefaultSourceItem,
                }
            };

            PostStatusMessage(message, id);
        }

        private void PostSelectedSourceKey()
        {
            PostStatusMessage(new DemoRoomStateMessage { SelectedSourceKey = room.SelectedSourceKey });
        }
    }

    /// <summary>
    /// The demo room's supplemental state. Property names here become fields on the app's room
    /// state object, merged into the same client-side object the framework bridge's
    /// <c>RoomStateMessage</c> populates - keep this in sync with the app's DemoRoomState type.
    /// </summary>
    public class DemoRoomStateMessage : DeviceStateMessageBase
    {
        [JsonProperty("roomType", NullValueHandling = NullValueHandling.Ignore)]
        public string RoomType { get; set; }

        [JsonProperty("selectedSourceKey", NullValueHandling = NullValueHandling.Ignore)]
        public string SelectedSourceKey { get; set; }

        [JsonProperty("techSystemStatusDeviceKeys", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> TechSystemStatusDeviceKeys { get; set; }

        [JsonProperty("techRackSensorDeviceKey", NullValueHandling = NullValueHandling.Ignore)]
        public string TechRackSensorDeviceKey { get; set; }

        [JsonProperty("techDisplays", NullValueHandling = NullValueHandling.Ignore)]
        public List<DemoRoomTechDisplayConfig> TechDisplays { get; set; }

        [JsonProperty("techRoutingDeviceKey", NullValueHandling = NullValueHandling.Ignore)]
        public string TechRoutingDeviceKey { get; set; }

        [JsonProperty("configuration", NullValueHandling = NullValueHandling.Ignore)]
        public RoomConfiguration Configuration { get; set; }
    }
}
