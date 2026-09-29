using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Core.Routing;
using PepperDash.Essentials.Devices.Common;

namespace EssentialsDemoRoom.MockDevices
{
    /// <summary>
    /// Config for a <see cref="MockHdmiSource"/>.
    /// </summary>
    public class MockHdmiSourceConfig
    {
        /// <summary>
        /// Whether the source reports sync detected on startup. Defaults to true - a source you
        /// have to explicitly break is a better demo default than one that starts broken.
        /// </summary>
        [JsonProperty("startsWithSync", NullValueHandling = NullValueHandling.Ignore)]
        public bool StartsWithSync { get; set; } = true;
    }

    /// <summary>
    /// A source with a simulated wired (HDMI-style) connection: unlike <see cref="GenericSource"/>,
    /// it reports whether a signal is present, via <see cref="IVideoSync"/> - the framework declares
    /// that interface but, as of this writing, nothing in Essentials implements it and no messenger
    /// exposes it. This fills that gap for the demo; if it turns out to be broadly useful the same
    /// way <c>MockLightingDevice</c> did, it's a reasonable candidate to move into
    /// <c>PepperDash.Essentials.Devices.Common</c> too.
    /// </summary>
    public class MockHdmiSource : EssentialsDevice, IRoutingSource, IUsageTracking, IUiDisplayInfo, IVideoSync, ICommunicationMonitor
    {
        private readonly MockStatusMonitor monitor;

        /// <inheritdoc />
        /// <remarks>Always online: there's no real connection behind this mock to lose.</remarks>
        public StatusMonitorBase CommunicationMonitor => monitor;

        /// <inheritdoc />
        public uint DisplayUiType => DisplayUiConstants.TypeNoControls;

        /// <inheritdoc />
        public RoutingOutputPort AnyOut { get; }

        /// <inheritdoc />
        public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; }

        /// <inheritdoc />
        public UsageTracking UsageTracker { get; set; }

        /// <inheritdoc />
        public bool VideoSyncDetected { get; private set; }

        /// <inheritdoc />
        public event EventHandler VideoSyncChanged;

        public MockHdmiSource(string key, string name, MockHdmiSourceConfig config)
            : base(key, name)
        {
            VideoSyncDetected = config?.StartsWithSync ?? true;

            monitor = new MockStatusMonitor(this);
            monitor.SetStatus(MonitorStatus.IsOk);

            AnyOut = new RoutingOutputPort(RoutingPortNames.AnyOut, eRoutingSignalType.Audio | eRoutingSignalType.Video,
                eRoutingPortConnectionType.Hdmi, null, this);
            OutputPorts = new RoutingPortCollection<RoutingOutputPort> { AnyOut };
        }

        /// <summary>
        /// Simulates plugging/unplugging the source. Exposed to Mobile Control via
        /// <see cref="IVideoSyncMessenger"/>'s <c>/setVideoSync</c> action so the "no signal" state
        /// this demo shows off is actually reachable, not just a static screenshot.
        /// </summary>
        public void SetVideoSyncDetected(bool detected)
        {
            if (VideoSyncDetected == detected) return;

            VideoSyncDetected = detected;

            this.LogInformation("Video sync {state}", detected ? "detected" : "lost");

            VideoSyncChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        protected override void CreateMobileControlMessengers()
        {
            var mobileControl = DeviceManager.AllDevices.OfType<IMobileControl>().FirstOrDefault();

            if (mobileControl == null)
            {
                this.LogInformation("Mobile Control not found; skipping messenger registration");
                return;
            }

            mobileControl.AddDeviceMessenger(new IVideoSyncMessenger($"{Key}-videoSync", $"/device/{Key}", this));

            base.CreateMobileControlMessengers();
        }
    }

    /// <summary>
    /// Builds a <see cref="MockHdmiSource"/> for any config entry typed "mockHdmiSource".
    /// </summary>
    public class MockHdmiSourceFactory : EssentialsPluginDeviceFactory<MockHdmiSource>
    {
        public MockHdmiSourceFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";
            TypeNames = new List<string> { "mockHdmiSource" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var config = dc.Properties?.ToObject<MockHdmiSourceConfig>();

            return new MockHdmiSource(dc.Key, dc.Name, config);
        }
    }
}
