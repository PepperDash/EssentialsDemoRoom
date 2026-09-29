using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;

namespace EssentialsDemoRoom.MockDevices
{
    /// <summary>
    /// One input a <see cref="MockProjectorDisplay"/> can select.
    /// </summary>
    public class MockProjectorDisplayInputConfig
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Config for a <see cref="MockProjectorDisplay"/>.
    /// </summary>
    public class MockProjectorDisplayConfig
    {
        /// <summary>
        /// The selectable inputs this projector reports, in order. Unlike <c>MockDisplay</c> (always
        /// HDMI1-4 + DisplayPort, hardcoded in its constructor), this device's inputs are config-driven
        /// so a projector can be given exactly the inputs its tech page is meant to show.
        /// </summary>
        [JsonProperty("inputs")]
        public List<MockProjectorDisplayInputConfig> Inputs { get; set; } = new List<MockProjectorDisplayInputConfig>();

        [JsonProperty("startingInputKey", NullValueHandling = NullValueHandling.Ignore)]
        public string StartingInputKey { get; set; }

        [JsonProperty("startsOn")]
        public bool StartsOn { get; set; } = true;
    }

    /// <summary>
    /// A mock projector: power on/off plus a config-driven set of selectable inputs, with no warmup/
    /// cooldown lag or routing/volume surface - unlike <c>MockDisplay</c>, this exists purely to back
    /// the tech Displays page's "Projector" entry, which needs a specific, small input list
    /// (<see cref="MockProjectorDisplayConfig.Inputs"/>) that <c>MockDisplay</c> has no way to
    /// configure.
    /// </summary>
    public class MockProjectorDisplay : EssentialsDevice, IHasPowerControlWithFeedback, IHasInputs<string>
    {
        private bool powerIsOn;

        /// <inheritdoc />
        public BoolFeedback PowerIsOnFeedback { get; }

        /// <inheritdoc />
        public ISelectableItems<string> Inputs { get; }

        public MockProjectorDisplay(string key, string name, MockProjectorDisplayConfig config)
            : base(key, name)
        {
            powerIsOn = config?.StartsOn ?? true;
            PowerIsOnFeedback = new BoolFeedback(() => powerIsOn);

            var items = new Dictionary<string, ISelectableItem>();
            var inputs = new MockProjectorDisplayInputs { Items = items };

            foreach (var inputConfig in config?.Inputs ?? new List<MockProjectorDisplayInputConfig>())
            {
                items[inputConfig.Key] = new MockProjectorDisplayInput(inputConfig.Key, inputConfig.Name, inputs);
            }

            Inputs = inputs;

            var startingInputKey = config?.StartingInputKey;
            if (startingInputKey != null && items.ContainsKey(startingInputKey))
            {
                items[startingInputKey].IsSelected = true;
                Inputs.CurrentItem = startingInputKey;
            }

            PowerIsOnFeedback.FireUpdate();
        }

        /// <inheritdoc />
        public void PowerOn()
        {
            if (powerIsOn) return;

            powerIsOn = true;
            PowerIsOnFeedback.FireUpdate();
        }

        /// <inheritdoc />
        public void PowerOff()
        {
            if (!powerIsOn) return;

            powerIsOn = false;
            PowerIsOnFeedback.FireUpdate();
        }

        /// <inheritdoc />
        public void PowerToggle()
        {
            if (powerIsOn) PowerOff();
            else PowerOn();
        }
    }

    /// <summary>
    /// Builds a <see cref="MockProjectorDisplay"/> for any config entry typed "mockProjectorDisplay".
    /// </summary>
    public class MockProjectorDisplayFactory : EssentialsPluginDeviceFactory<MockProjectorDisplay>
    {
        public MockProjectorDisplayFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";
            TypeNames = new List<string> { "mockProjectorDisplay" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var config = dc.Properties?.ToObject<MockProjectorDisplayConfig>();

            return new MockProjectorDisplay(dc.Key, dc.Name, config);
        }
    }

    /// <summary>
    /// <see cref="ISelectableItems{TKey}"/> backing a <see cref="MockProjectorDisplay"/>'s inputs.
    /// </summary>
    public class MockProjectorDisplayInputs : ISelectableItems<string>
    {
        private Dictionary<string, ISelectableItem> items;

        /// <inheritdoc />
        public Dictionary<string, ISelectableItem> Items
        {
            get => items;
            set
            {
                if (items == value) return;

                items = value;
                ItemsUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private string currentItem;

        /// <inheritdoc />
        public string CurrentItem
        {
            get => currentItem;
            set
            {
                if (currentItem == value) return;

                currentItem = value;
                CurrentItemChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        public event EventHandler ItemsUpdated;

        /// <inheritdoc />
        public event EventHandler CurrentItemChanged;
    }

    /// <summary>
    /// <see cref="ISelectableItem"/> backing one of a <see cref="MockProjectorDisplay"/>'s inputs.
    /// </summary>
    public class MockProjectorDisplayInput : ISelectableItem
    {
        private readonly MockProjectorDisplayInputs parent;
        private bool isSelected;

        /// <inheritdoc />
        public string Key { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected == value) return;

                isSelected = value;
                ItemUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        public event EventHandler ItemUpdated;

        public MockProjectorDisplayInput(string key, string name, MockProjectorDisplayInputs parent)
        {
            Key = key;
            Name = name;
            this.parent = parent;
        }

        /// <inheritdoc />
        public void Select()
        {
            foreach (var input in parent.Items)
            {
                input.Value.IsSelected = input.Key == Key;
            }

            parent.CurrentItem = Key;
        }
    }
}
