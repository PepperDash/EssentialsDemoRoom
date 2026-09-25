using System;
using System.Collections.Generic;
using System.Linq;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common.Room;
using PepperDash.Essentials.Room.Config;

namespace EssentialsDemoRoom
{
    /// <summary>
    /// Capability surface of the demo room. The Mobile Control messenger and any future consumers
    /// depend on this interface rather than on <see cref="DemoRoom"/> directly.
    /// </summary>
    /// <remarks>
    /// Each interface here is an Essentials v3 "capability" contract. The React app discovers what a
    /// room can do from the interfaces its room device implements, so adding a capability is usually
    /// a matter of adding an interface and implementing its members.
    /// </remarks>
    public interface IDemoRoom
        : IEssentialsRoom,
          IRunRouteAction,
          IRunDirectRouteAction,
          IHasDefaultDisplay,
          IHasCurrentVolumeControls,
          IWarmingCooling,
          IEssentialsRoomPropertiesConfig,
          ITechPassword
    {
    }

    /// <summary>
    /// Room device for the Essentials v3 demo system.
    /// </summary>
    /// <remarks>
    /// Scaffold only. Routing, volume and power are wired to the standard v3 plumbing so the room is
    /// functional end to end against mock devices, but the demo-specific business logic is still to
    /// come. Everything this room drives is a mock device from Essentials, so the program runs on a
    /// bare processor with no AV hardware attached.
    /// </remarks>
    public class DemoRoom : ReconfigurableDevice, IDemoRoom
    {
        private readonly List<Action> customActivateActions = new List<Action>();
        private bool isShuttingDown;

        /// <summary>
        /// Destinations resolved from the configured destination list, keyed by destination-list item key.
        /// </summary>
        private Dictionary<string, IRoutingSinkWithFeedback> displays = new Dictionary<string, IRoutingSinkWithFeedback>();

        private IRoutingSinkWithFeedback programAudioDestination;

        public DemoRoom(DeviceConfig config)
            : base(config)
        {
            RoomType = config.Type;
            Props = config.Properties?.ToObject<DemoRoomProps>() ?? new DemoRoomProps();

            SourceList = ConfigReader.ConfigObject.GetSourceListForKey(SourceListKey)
                ?? new Dictionary<string, SourceListItem>();

            EnvironmentalControlDevices = new List<EssentialsDevice>();

            OnFeedback = new BoolFeedback("OnFeedback", () => isOn);
            IsWarmingUpFeedback = new BoolFeedback("IsWarmingUpFeedback", () => false);
            IsCoolingDownFeedback = new BoolFeedback("IsCoolingDownFeedback", () => false);

            ShutdownPromptSeconds = Props.ShutdownPromptSeconds > 0 ? Props.ShutdownPromptSeconds : 30;
            ShutdownPromptTimer = new SecondsCountdownTimer($"{Key}-shutdownPromptTimer")
            {
                CountsDown = true,
                SecondsToCount = ShutdownPromptSeconds
            };
            ShutdownPromptTimer.HasFinished += (sender, args) => Shutdown();

            // Devices referenced by this room are not guaranteed to exist yet when the constructor runs.
            // Pre-activation runs after every device is built but before any of them activate, which is
            // where cross-device lookups belong.
            AddPreActivationAction(ResolveDevices);
        }

        #region Configuration

        /// <summary>
        /// Strongly typed properties from the "properties" object of this room's config entry.
        /// </summary>
        public DemoRoomProps Props { get; private set; }

        /// <inheritdoc />
        public EssentialsRoomPropertiesConfig PropertiesConfig => Props;

        /// <summary>
        /// The config "type" this room was built from. The React app switches on this to pick a
        /// room business component.
        /// </summary>
        public string RoomType { get; private set; }

        /// <inheritdoc />
        public string SourceListKey => Props.SourceListKey;

        /// <inheritdoc />
        public string DestinationListKey => Props.DestinationListKey;

        /// <inheritdoc />
        public string AudioControlPointListKey => Props.AudioControlPointListKey;

        /// <inheritdoc />
        public string CameraListKey => Props.CameraListKey;

        /// <summary>
        /// Source-list items available to this room, keyed by source-list item key.
        /// </summary>
        public Dictionary<string, SourceListItem> SourceList { get; private set; }

        /// <summary>
        /// Key of the room's default audio device, used for the room volume control.
        /// </summary>
        public string DefaultAudioDeviceKey => Props.DefaultAudioKey;

        /// <inheritdoc />
        public string LogoUrlLightBkgnd => Props.LogoLight?.GetLogoUrlLight() ?? string.Empty;

        /// <inheritdoc />
        public string LogoUrlDarkBkgnd => Props.LogoDark?.GetLogoUrlDark() ?? string.Empty;

        #endregion

        #region Power / warming / cooling

        private bool isOn;

        /// <inheritdoc />
        public BoolFeedback OnFeedback { get; }

        /// <inheritdoc />
        /// <remarks>
        /// Always false. Mock devices power on instantly, so the demo room has no warm-up period.
        /// A room driving real displays would hold this true until the slowest display reports on.
        /// </remarks>
        public BoolFeedback IsWarmingUpFeedback { get; }

        /// <inheritdoc />
        /// <remarks>See <see cref="IsWarmingUpFeedback"/>.</remarks>
        public BoolFeedback IsCoolingDownFeedback { get; }

        /// <inheritdoc />
        public SecondsCountdownTimer ShutdownPromptTimer { get; }

        /// <inheritdoc />
        public int ShutdownPromptSeconds { get; private set; }

        /// <inheritdoc />
        public int ShutdownVacancySeconds => Props.ShutdownVacancySeconds;

        /// <inheritdoc />
        public eShutdownType ShutdownType => eShutdownType.Manual;

        /// <summary>
        /// Sets the number of seconds the shutdown prompt counts down before the room shuts down.
        /// </summary>
        public void SetShutdownPromptSeconds(int seconds)
        {
            ShutdownPromptSeconds = seconds;
            ShutdownPromptTimer.SecondsToCount = seconds;
        }

        /// <inheritdoc />
        public void PowerOnToDefaultOrLastSource()
        {
            // TODO: demo behavior - power on and route the default source.
            RunDefaultPresentRoute();
        }

        /// <inheritdoc />
        public bool RunDefaultPresentRoute()
        {
            if (string.IsNullOrEmpty(Props.DefaultSourceItem))
            {
                this.LogDebug("No defaultSourceItem configured; nothing to route");
                return false;
            }

            this.LogInformation("Running default present route '{sourceItem}'", Props.DefaultSourceItem);

            RunRouteAction(Props.DefaultSourceItem, SourceListKey);

            SetPowerState(true);

            return true;
        }

        /// <inheritdoc />
        public void StartShutdown(eShutdownType type)
        {
            switch (type)
            {
                case eShutdownType.Manual:
                    ShutdownPromptTimer.Start();
                    break;
                case eShutdownType.Vacancy:
                case eShutdownType.External:
                    Shutdown();
                    break;
                case eShutdownType.None:
                    break;
            }
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (isShuttingDown) return;

            try
            {
                isShuttingDown = true;

                this.LogInformation("Shutting down room");

                // "roomOff" is the conventional source-list item that clears routes.
                RunRouteAction("roomOff", SourceListKey);

                SetPowerState(false);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Error shutting down room");
            }
            finally
            {
                isShuttingDown = false;
            }
        }

        private void SetPowerState(bool on)
        {
            if (isOn == on) return;

            isOn = on;
            OnFeedback.FireUpdate();
        }

        #endregion

        #region Routing

        /// <inheritdoc />
        public IRoutingSinkWithFeedback DefaultDisplay { get; private set; }

        /// <inheritdoc />
        public void RunRouteAction(string routeKey, string sourceListKey) =>
            RunRouteAction(routeKey, sourceListKey, null);

        /// <inheritdoc />
        public void RunRouteAction(string routeKey, string sourceListKey, Action successCallback)
        {
            if (string.IsNullOrEmpty(sourceListKey))
                sourceListKey = SourceListKey;

            var sourceList = ConfigReader.ConfigObject.GetSourceListForKey(sourceListKey)
                ?? new Dictionary<string, SourceListItem>();

            if (!sourceList.TryGetValue(routeKey, out var sourceListItem))
            {
                this.LogWarning("No source-list item '{routeKey}' in source list '{sourceListKey}'", routeKey, sourceListKey);
                return;
            }

            this.LogInformation("Running route action '{routeKey}'", routeKey);

            foreach (var route in sourceListItem.RouteList ?? new List<SourceRouteListItem>())
            {
                DoRoute(route);
            }

            successCallback?.Invoke();
        }

        /// <inheritdoc />
        public void RunDirectRoute(string sourceKey, string destinationKey, eRoutingSignalType signalType = eRoutingSignalType.AudioVideo)
        {
            var destination = displays.Values.FirstOrDefault(d => d.Key == destinationKey);

            if (destination == null)
            {
                this.LogWarning("No destination '{destinationKey}' in this room", destinationKey);
                return;
            }

            if (!SourceList.TryGetValue(sourceKey, out var sourceListItem))
            {
                this.LogWarning("No source-list item '{sourceKey}' in source list '{sourceListKey}'", sourceKey, SourceListKey);
                return;
            }

            if (!(DeviceManager.GetDeviceForKey(sourceListItem.SourceKey) is IRoutingOutputs source))
            {
                destination.ReleaseRoute();
                SetSinkCurrentSource(destination, null, signalType);
                return;
            }

            destination.ReleaseAndMakeRoute(source, signalType);
            SetSinkCurrentSource(destination, sourceListItem, signalType);

            // Audio follows video: a video route to anything other than the program audio destination
            // also sends audio to the program audio destination.
            if (Props.EnableAudioFollowsVideo
                && signalType.HasFlag(eRoutingSignalType.Video)
                && programAudioDestination != null
                && programAudioDestination.Key != destinationKey)
            {
                programAudioDestination.ReleaseAndMakeRoute(source, eRoutingSignalType.Audio);
                SetSinkCurrentSource(programAudioDestination, sourceListItem, eRoutingSignalType.Audio);
            }
        }

        private void DoRoute(SourceRouteListItem route)
        {
            if (route == null) return;

            var destination = DeviceManager.GetDeviceForKey(route.DestinationKey) as IRoutingSinkWithFeedback;

            if (destination == null)
            {
                this.LogWarning("Route destination '{destinationKey}' is not a routing sink", route.DestinationKey);
                return;
            }

            var sourceListItem = SourceList.Values.FirstOrDefault(s => s.SourceKey == route.SourceKey);

            if (DeviceManager.GetDeviceForKey(route.SourceKey) is IRoutingOutputs source)
            {
                destination.ReleaseAndMakeRoute(source, route.Type);
                SetSinkCurrentSource(destination, sourceListItem, route.Type);
                return;
            }

            // No source device means "clear this destination" (e.g. the roomOff source-list item).
            destination.ReleaseRoute();
            SetSinkCurrentSource(destination, null, route.Type);
        }

        /// <summary>
        /// Writes the current source for a destination using the v3 ICurrentSources model. A null
        /// source clears both audio and video.
        /// </summary>
        private void SetSinkCurrentSource(IRoutingSinkWithFeedback sink, SourceListItem sourceListItem, eRoutingSignalType signalType)
        {
            if (sink == null) return;

            if (!(sourceListItem?.SourceDevice is IRoutingSource source))
            {
                sink.SetCurrentSource(eRoutingSignalType.AudioVideo, null);
                return;
            }

            sink.SetCurrentSource(signalType, source);
        }

        private IRoutingSinkWithFeedback ResolveDefaultDisplay()
        {
            if (displays.Count == 0) return null;

            if (!string.IsNullOrEmpty(Props.DefaultDisplayKey)
                && displays.TryGetValue(Props.DefaultDisplayKey, out var configured))
                return configured;

            if (displays.TryGetValue("defaultDisplay", out var conventional))
                return conventional;

            return displays.Values.FirstOrDefault();
        }

        #endregion

        #region Volume

        /// <inheritdoc />
        public IBasicVolumeControls CurrentVolumeControls { get; private set; }

        /// <inheritdoc />
        public bool ZeroVolumeWhenSwtichingVolumeDevices => Props.ZeroVolumeWhenSwtichingVolumeDevices;

        /// <inheritdoc />
        public event EventHandler<VolumeDeviceChangeEventArgs> CurrentVolumeDeviceChange;

        /// <inheritdoc />
        public void SetDefaultLevels()
        {
            // TODO: demo behavior - recall default levels on the mock audio device.
        }

        private void SetCurrentVolumeControls(IBasicVolumeControls newControls)
        {
            if (CurrentVolumeControls == newControls) return;

            var previous = CurrentVolumeControls;
            CurrentVolumeControls = newControls;

            CurrentVolumeDeviceChange?.Invoke(
                this,
                new VolumeDeviceChangeEventArgs(previous, newControls, ChangeType.DidChange));
        }

        #endregion

        #region Environmental controls

        /// <inheritdoc />
        public List<EssentialsDevice> EnvironmentalControlDevices { get; private set; }

        /// <inheritdoc />
        public bool HasEnvironmentalControlDevices => EnvironmentalControlDevices.Count > 0;

        #endregion

        #region Tech password

        /// <inheritdoc />
        public event EventHandler<TechPasswordEventArgs> TechPasswordValidateResult;

        /// <inheritdoc />
        public event EventHandler<EventArgs> TechPasswordChanged;

        /// <inheritdoc />
        public int TechPasswordLength => Props.Tech?.Password?.Length ?? 0;

        /// <inheritdoc />
        public void ValidateTechPassword(string password)
        {
            var isValid = Props.Tech?.Password == password;

            TechPasswordValidateResult?.Invoke(this, new TechPasswordEventArgs(isValid));
        }

        /// <inheritdoc />
        public void SetTechPassword(string oldPassword, string newPassword)
        {
            if (Props.Tech == null || Props.Tech.Password != oldPassword)
            {
                this.LogWarning("Tech password change rejected: current password does not match");
                return;
            }

            Props.Tech.Password = newPassword;

            // Persist the change back to the config file.
            Config.Properties = Newtonsoft.Json.Linq.JToken.FromObject(Props);
            SetConfig(Config);

            TechPasswordChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Mobile Control

        /// <inheritdoc />
        public bool IsMobileControlEnabled => true;

        /// <inheritdoc />
        public IMobileControlRoomMessenger MobileControlRoomBridge { get; set; }

        /// <summary>
        /// Registers this room's messenger with Mobile Control. Essentials calls this during
        /// activation; the messenger is what the React app talks to over the websocket.
        /// </summary>
        protected override void CreateMobileControlMessengers()
        {
            var mobileControl = DeviceManager.AllDevices.OfType<IMobileControl>().FirstOrDefault();

            if (mobileControl == null)
            {
                this.LogInformation("Mobile Control not found; skipping messenger registration");
                return;
            }

            mobileControl.AddDeviceMessenger(new DemoRoomMessenger($"{Key}-room", $"/room/{Key}", this));

            base.CreateMobileControlMessengers();
        }

        #endregion

        #region Activation

        /// <summary>
        /// Adds an action to run when this room activates. Lets other devices hook room startup
        /// without this class having to know about them.
        /// </summary>
        public void AddCustomActivationAction(Action action) => customActivateActions.Add(action);

        /// <summary>
        /// Resolves the devices this room drives. Runs in pre-activation, once every device exists.
        /// </summary>
        private void ResolveDevices()
        {
            try
            {
                displays = new Dictionary<string, IRoutingSinkWithFeedback>();

                var destinationList = ConfigReader.ConfigObject.GetDestinationListForKey(DestinationListKey);

                if (destinationList == null)
                {
                    this.LogWarning("No destination list '{destinationListKey}' found", DestinationListKey);
                }
                else
                {
                    foreach (var destination in destinationList)
                    {
                        if (!(DeviceManager.GetDeviceForKey(destination.Value.SinkKey) is IRoutingSinkWithFeedback sink))
                        {
                            this.LogWarning("Destination '{sinkKey}' is not a routing sink", destination.Value.SinkKey);
                            continue;
                        }

                        displays[destination.Key] = sink;

                        if (destination.Value.isProgramAudioDestination)
                            programAudioDestination = sink;
                    }
                }

                DefaultDisplay = ResolveDefaultDisplay();

                if (DeviceManager.GetDeviceForKey(DefaultAudioDeviceKey) is IBasicVolumeControls volumeControls)
                    SetCurrentVolumeControls(volumeControls);
                else if (!string.IsNullOrEmpty(DefaultAudioDeviceKey))
                    this.LogWarning("Default audio device '{key}' not found or has no volume controls", DefaultAudioDeviceKey);

                EnvironmentalControlDevices = (Props.Environment?.DeviceKeys ?? new List<string>())
                    .Select(DeviceManager.GetDeviceForKey)
                    .OfType<EssentialsDevice>()
                    .ToList();
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Error resolving room devices");
            }
        }

        protected override bool CustomActivate()
        {
            foreach (var action in customActivateActions)
            {
                action();
            }

            this.LogInformation("Room '{name}' activated", Name);

            return base.CustomActivate();
        }

        #endregion
    }
}
