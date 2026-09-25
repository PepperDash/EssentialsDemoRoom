using Newtonsoft.Json;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.RoomBridges;

namespace EssentialsDemoRoom
{
    /// <summary>
    /// Bridges <see cref="DemoRoom"/> to Mobile Control. Everything the React app knows about this
    /// room arrives through the messages defined here.
    /// </summary>
    /// <remarks>
    /// A messenger does two things: it registers actions the client can invoke by path
    /// (<see cref="RegisterActions"/>), and it posts state back to the client
    /// (<see cref="PostStatusMessage(DeviceStateMessageBase, string)"/>).
    /// </remarks>
    public class DemoRoomMessenger : MessengerBase
    {
        private readonly DemoRoom room;

        public DemoRoomMessenger(string key, string messagePath, DemoRoom room)
            : base(key, messagePath, room)
        {
            this.room = room;
        }

        protected override void RegisterActions()
        {
            // The React app requests the room's state at boot. Essentials v3 clients send
            // /fullStatus; /status is kept for v2-era clients.
            AddAction("/fullStatus", (id, content) => SendFullStatus(id));
            AddAction("/status", (id, content) => SendFullStatus(id));

            // TODO: add demo actions here, e.g.
            // AddAction("/roomOn", (id, content) => room.PowerOnToDefaultOrLastSource());
            // AddAction("/roomOff", (id, content) => room.Shutdown());

            base.RegisterActions();
        }

        private void SendFullStatus(string id = null)
        {
            var message = new DemoRoomStateMessage
            {
                RoomType = room.RoomType,
                DefaultAudioDeviceKey = room.DefaultAudioDeviceKey,
                DefaultDisplayKey = room.DefaultDisplay?.Key,
                Configuration = new RoomConfiguration
                {
                    HasRoutingControls = true,
                    DestinationList = ConfigReader.ConfigObject.GetDestinationListForKey(room.DestinationListKey)
                }
            };

            PostStatusMessage(message, id);
        }
    }

    /// <summary>
    /// Room state pushed to the React app. Property names here become the fields on the app's
    /// room-state object, so keep them in sync with the app's DemoRoomState type.
    /// </summary>
    public class DemoRoomStateMessage : DeviceStateMessageBase
    {
        [JsonProperty("roomType")]
        public string RoomType { get; set; }

        [JsonProperty("defaultAudioDeviceKey", NullValueHandling = NullValueHandling.Ignore)]
        public string DefaultAudioDeviceKey { get; set; }

        [JsonProperty("defaultDisplayKey", NullValueHandling = NullValueHandling.Ignore)]
        public string DefaultDisplayKey { get; set; }

        [JsonProperty("configuration", NullValueHandling = NullValueHandling.Ignore)]
        public RoomConfiguration Configuration { get; set; }
    }
}
