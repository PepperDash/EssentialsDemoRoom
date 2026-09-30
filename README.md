![PepperDash Essentials Plugin Logo](/images/essentials-plugin-blue.png)

# Essentials Demo Room

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4) ![PepperDash Essentials](https://img.shields.io/badge/PepperDash%20Essentials-≥%20v3.0.0-blue)

Room plugin for the PepperDash Essentials v3 demo system.

**Documentation:** tutorials and how-to guides for the demo are at https://pepperdash.github.io/EssentialsDemoConfig/.

## License

Provided under MIT license.

## Overview

This is one of three repos that make up the Essentials v3 demo:

| Repo | Contains |
| --- | --- |
| [EssentialsDemoRoom](https://github.com/PepperDash/EssentialsDemoRoom) | This repo — the room plugin holding the demo's business logic |
| [EssentialsDemoConfig](https://github.com/PepperDash/EssentialsDemoConfig) | The configuration file, and the [demo documentation](https://pepperdash.github.io/EssentialsDemoConfig/) |
| [EssentialsDemoReactApp](https://github.com/PepperDash/EssentialsDemoReactApp) | The Mobile Control React app the demo's UI runs in |

Every device the demo room drives is a mock — either an Essentials mock device (`MockDisplay`,
`MockAudioDevice`, `MockRoutingMidpoint`, and friends) or mocked logic in this plugin. Nothing here
talks to real AV hardware, so the program runs on any 4-series processor on its own.

## What's in here

`src/DemoRoom` holds the room:

- **`DemoRoom.cs`** — the room device, plus the `IDemoRoom` interface that declares its capabilities.
  In Essentials v3 a room advertises what it can do by implementing capability interfaces
  (`IRunRouteAction`, `IHasDefaultDisplay`, `IHasCurrentVolumeControls`, …); the React app reads
  those to decide what to render.
- **`DemoRoomProps.cs`** — the strongly typed `properties` object from this room's config entry.
- **`DemoRoomFactory.cs`** — maps the config `"type": "essentialsDemoRoom"` to the room class.
  Essentials finds factories by reflection, so there is nothing else to register.
- **`DemoRoomMessenger.cs`** — the Mobile Control messenger. Every message between the room and the
  React app passes through here.

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) v3 libraries, referenced via NuGet:

- `PepperDashEssentials`

`dotnet build` restores them; no manual NuGet step is needed.

## Build

```bash
dotnet build src/epi-essentials-demo.4Series.csproj
```

The build targets .NET 8 and produces `output/epi-essentials-demo.4Series.<version>.cplz`, the
program library you load alongside Essentials on the processor. For the demo you do not need to
deploy this by hand — the EssentialsDemoConfig repo bundles it, the config file and the React app
into a single `.cpz`.

## NuGet package

A NuGet package is generated on every build. To change its name or details, edit these properties in
the `.csproj`:

1. `PackageId` — the name used to pull the package from NuGet once published
2. `PackageProjectUrl` — the URL of this repo
3. `AssemblyTitle` — the dll name shown on the processor when the plugin loads
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "DemoRoomTech",
    "group": "Group",
    "properties": {
        "SystemStatusDeviceKeys": [
            "SampleString"
        ],
        "RackSensorDeviceKey": "SampleString",
        "Displays": [
            {
                "deviceKey": "SampleString",
                "ScreenDeviceKey": "SampleString",
                "LiftDeviceKey": "SampleString"
            }
        ],
        "RoutingDeviceKey": "SampleString"
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->

<!-- END Supported Types -->
<!-- START Join Maps -->

<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IHasPowerControlWithFeedback
- IHasInputs<string>
- ITemperatureSensor
- IHumiditySensor
- IProjectorScreenLiftControl
- IRoutingSource
- IUsageTracking
- IUiDisplayInfo
- IVideoSync
- ICommunicationMonitor
- IDemoRoom
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- StatusMonitorBase
- MessengerBase
- EssentialsDevice
- ReconfigurableDevice
- EssentialsAvRoomPropertiesConfig
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void SetStatus(MonitorStatus status)
- public void PowerOn()
- public void PowerOff()
- public void PowerToggle()
- public void Select()
- public void SetTemperatureFormat(bool setToC)
- public void Raise()
- public void Lower()
- public void SetVideoSyncDetected(bool detected)
- public void SetShutdownPromptSeconds(int seconds)
- public void PowerOnToDefaultOrLastSource()
- public bool RunDefaultPresentRoute()
- public void StartShutdown(eShutdownType type)
- public void Shutdown()
- public void RunRouteAction(string routeKey, string sourceListKey)
- public void RunRouteAction(string routeKey, string sourceListKey, Action successCallback)
- public void RunDirectRoute(string sourceKey, string destinationKey, eRoutingSignalType signalType = eRoutingSignalType.AudioVideo)
- public void SetDefaultLevels()
- public void ValidateTechPassword(string password)
- public void SetTechPassword(string oldPassword, string newPassword)
- public void AddCustomActivationAction(Action action)
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- PowerIsOnFeedback
- TemperatureInCFeedback
- IsInUpPosition
- OnFeedback
- IsWarmingUpFeedback
- IsCoolingDownFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- TemperatureFeedback
- HumidityFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->

<!-- END String Feedbacks -->
