using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core.Routing;

namespace EssentialsDemoRoom.MockDevices
{
    /// <summary>
    /// Mobile Control messenger for any device implementing <see cref="IVideoSync"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="IVideoSync"/> is declared in <c>PepperDash.Essentials.Core.Routing</c> but, as of
    /// this writing, has no implementations and no messenger anywhere in Essentials - it isn't in
    /// <c>MessengerFactoryRegistry</c>, so nothing auto-attaches this even for a device that does
    /// implement it. Written against the interface, not <c>MockHdmiSource</c> specifically, so any
    /// future <c>IVideoSync</c> device can reuse it the same way <c>ILightingScenesMessenger</c>
    /// works for anything implementing <c>ILightingScenes</c>.
    /// </remarks>
    public class IVideoSyncMessenger : MessengerBase
    {
        private readonly IVideoSync device;

        public IVideoSyncMessenger(string key, string messagePath, IVideoSync device)
            : base(key, messagePath, device as IKeyName)
        {
            this.device = device;

            device.VideoSyncChanged += (sender, args) => SendStatus();
        }

        protected override void RegisterActions()
        {
            AddAction("/fullStatus", (id, content) => SendStatus(id));

            AddAction("/setVideoSync", (id, content) =>
            {
                if (device is MockHdmiSource mock)
                    mock.SetVideoSyncDetected(content.ToObject<bool>());
            });

            base.RegisterActions();
        }

        private void SendStatus(string id = null)
        {
            PostStatusMessage(new VideoSyncStateMessage { VideoSyncDetected = device.VideoSyncDetected }, id);
        }
    }

    public class VideoSyncStateMessage : DeviceStateMessageBase
    {
        [JsonProperty("videoSyncDetected")]
        public bool VideoSyncDetected { get; set; }
    }
}
