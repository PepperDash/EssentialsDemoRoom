using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;

namespace EssentialsDemoRoom.MockDevices
{
    /// <summary>
    /// Config for a <see cref="MockProjectorScreenLiftController"/>.
    /// </summary>
    public class MockProjectorScreenLiftControllerConfig
    {
        [JsonProperty("displayDeviceKey")]
        public string DisplayDeviceKey { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public eScreenLiftControlType Type { get; set; }

        [JsonProperty("startsInUpPosition")]
        public bool StartsInUpPosition { get; set; }
    }

    /// <summary>
    /// A mock projector screen/lift actuator: <see cref="Raise"/>/<see cref="Lower"/> flip position
    /// instantly rather than driving relays. The real <c>ScreenLiftController</c>
    /// (<c>PepperDash.Essentials.Devices.Common.Shades</c>) needs a resolvable <c>ISwitchedOutput</c>
    /// relay device to do anything - nothing in either this room plugin or the framework mocks one, so
    /// this exists as a self-contained alternative for the demo's Screen/Lift tech controls, the same
    /// way <c>MockDisplay</c> fakes its own power feedback without real hardware.
    /// </summary>
    public class MockProjectorScreenLiftController : EssentialsDevice, IProjectorScreenLiftControl
    {
        private bool inUpPosition;

        /// <inheritdoc />
        public BoolFeedback IsInUpPosition { get; }

        /// <inheritdoc />
        public bool InUpPosition => inUpPosition;

        /// <inheritdoc />
        public event EventHandler<EventArgs> PositionChanged;

        /// <inheritdoc />
        public string DisplayDeviceKey { get; }

        /// <inheritdoc />
        public eScreenLiftControlType Type { get; }

        public MockProjectorScreenLiftController(string key, string name, MockProjectorScreenLiftControllerConfig config)
            : base(key, name)
        {
            DisplayDeviceKey = config?.DisplayDeviceKey;
            Type = config?.Type ?? eScreenLiftControlType.screen;
            inUpPosition = config?.StartsInUpPosition ?? false;

            IsInUpPosition = new BoolFeedback(() => inUpPosition);
            IsInUpPosition.FireUpdate();
        }

        /// <inheritdoc />
        public void Raise() => SetPosition(true);

        /// <inheritdoc />
        public void Lower() => SetPosition(false);

        private void SetPosition(bool up)
        {
            if (inUpPosition == up) return;

            inUpPosition = up;
            IsInUpPosition.FireUpdate();
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Builds a <see cref="MockProjectorScreenLiftController"/> for any config entry typed
    /// "mockProjectorScreenLiftController".
    /// </summary>
    public class MockProjectorScreenLiftControllerFactory : EssentialsPluginDeviceFactory<MockProjectorScreenLiftController>
    {
        public MockProjectorScreenLiftControllerFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";
            TypeNames = new List<string> { "mockProjectorScreenLiftController" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var config = dc.Properties?.ToObject<MockProjectorScreenLiftControllerConfig>();

            return new MockProjectorScreenLiftController(dc.Key, dc.Name, config);
        }
    }
}
