using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;

namespace EssentialsDemoRoom.MockDevices
{
    /// <summary>
    /// Config for a <see cref="MockRackSensorDevice"/>.
    /// </summary>
    public class MockRackSensorConfig
    {
        /// <summary>
        /// Starting temperature reading, in whole/tenths degrees Fahrenheit (e.g. 78.0).
        /// </summary>
        [JsonProperty("startingTemperatureF", NullValueHandling = NullValueHandling.Ignore)]
        public double StartingTemperatureF { get; set; } = 72.0;

        /// <summary>
        /// Starting relative humidity reading, 0-100.
        /// </summary>
        [JsonProperty("startingHumidityPercent", NullValueHandling = NullValueHandling.Ignore)]
        public ushort StartingHumidityPercent { get; set; } = 45;
    }

    /// <summary>
    /// A single mock rack sensor reporting both temperature (<see cref="ITemperatureSensor"/>) and
    /// humidity (<see cref="IHumiditySensor"/>) for the System Status tech page's "Rack Temp"/"Rack
    /// Humidity" readings. Both interfaces are declared by the framework and auto-messengered, but
    /// as of this writing nothing in Essentials implements either.
    /// </summary>
    public class MockRackSensorDevice : EssentialsDevice, ITemperatureSensor, IHumiditySensor
    {
        private bool isCelsius;

        /// <inheritdoc />
        public IntFeedback TemperatureFeedback { get; }

        /// <inheritdoc />
        public BoolFeedback TemperatureInCFeedback { get; }

        /// <inheritdoc />
        public IntFeedback HumidityFeedback { get; }

        public MockRackSensorDevice(string key, string name, MockRackSensorConfig config)
            : base(key, name)
        {
            var startingTemperatureTenths = (int)((config?.StartingTemperatureF ?? 72.0) * 10);
            var startingHumidity = config?.StartingHumidityPercent ?? 45;

            TemperatureFeedback = new IntFeedback(() => startingTemperatureTenths);
            TemperatureInCFeedback = new BoolFeedback(() => isCelsius);
            HumidityFeedback = new IntFeedback(() => startingHumidity);

            // Seed the feedbacks' cached values immediately - IntFeedback/BoolFeedback only reflect
            // the current backing value after FireUpdate has run once, so without this the UI would
            // report 0/false until something else happened to trigger an update.
            TemperatureFeedback.FireUpdate();
            TemperatureInCFeedback.FireUpdate();
            HumidityFeedback.FireUpdate();
        }

        /// <inheritdoc />
        public void SetTemperatureFormat(bool setToC)
        {
            if (isCelsius == setToC) return;

            isCelsius = setToC;
            TemperatureInCFeedback.FireUpdate();
        }
    }

    /// <summary>
    /// Builds a <see cref="MockRackSensorDevice"/> for any config entry typed "mockRackSensor".
    /// </summary>
    public class MockRackSensorFactory : EssentialsPluginDeviceFactory<MockRackSensorDevice>
    {
        public MockRackSensorFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";
            TypeNames = new List<string> { "mockRackSensor" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var config = dc.Properties?.ToObject<MockRackSensorConfig>();

            return new MockRackSensorDevice(dc.Key, dc.Name, config);
        }
    }
}
