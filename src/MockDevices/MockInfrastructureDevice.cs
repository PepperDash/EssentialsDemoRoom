using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace EssentialsDemoRoom.MockDevices
{
    /// <summary>
    /// Config for a <see cref="MockInfrastructureDevice"/>.
    /// </summary>
    public class MockInfrastructureDeviceConfig
    {
        /// <summary>
        /// The comm status this device reports. Defaults to <see cref="MonitorStatus.IsOk"/> - a
        /// rack full of healthy gear is a better demo default than one that starts broken.
        /// </summary>
        [JsonProperty("startingStatus", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public MonitorStatus StartingStatus { get; set; } = MonitorStatus.IsOk;
    }

    /// <summary>
    /// A piece of rack infrastructure (switcher, processor, network gear, etc.) with no AV function
    /// of its own - unlike this demo's other mocks, which are real routing/audio/lighting endpoints
    /// that happen to also be monitorable, this exists purely to give the System Status tech page
    /// something to show via <see cref="ICommunicationMonitor"/>. Its status is set once from
    /// config via <see cref="MockStatusMonitor"/> rather than simulated over time, since there's no
    /// real connection behind it to actually monitor.
    /// </summary>
    public class MockInfrastructureDevice : EssentialsDevice, ICommunicationMonitor
    {
        private readonly MockStatusMonitor monitor;

        /// <inheritdoc />
        public StatusMonitorBase CommunicationMonitor => monitor;

        public MockInfrastructureDevice(string key, string name, MockInfrastructureDeviceConfig config)
            : base(key, name)
        {
            monitor = new MockStatusMonitor(this);
            monitor.SetStatus(config?.StartingStatus ?? MonitorStatus.IsOk);
        }
    }

    /// <summary>
    /// Builds a <see cref="MockInfrastructureDevice"/> for any config entry typed
    /// "mockInfrastructureDevice".
    /// </summary>
    public class MockInfrastructureDeviceFactory : EssentialsPluginDeviceFactory<MockInfrastructureDevice>
    {
        public MockInfrastructureDeviceFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";
            TypeNames = new List<string> { "mockInfrastructureDevice" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var config = dc.Properties?.ToObject<MockInfrastructureDeviceConfig>();

            return new MockInfrastructureDevice(dc.Key, dc.Name, config);
        }
    }
}
