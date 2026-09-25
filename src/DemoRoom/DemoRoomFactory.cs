using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace EssentialsDemoRoom
{
    /// <summary>
    /// Builds a <see cref="DemoRoom"/> for any device in the config file whose type matches
    /// one of <see cref="EssentialsPluginDeviceFactory{T}.TypeNames"/>.
    /// </summary>
    /// <remarks>
    /// Essentials discovers this class by reflection when the plugin loads, so nothing else has to
    /// register it. The type name here is what goes in the config file's "type" field.
    /// </remarks>
    public class DemoRoomFactory : EssentialsPluginDeviceFactory<DemoRoom>
    {
        public DemoRoomFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";
            TypeNames = new List<string> { "essentialsDemoRoom" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc) => new DemoRoom(dc);
    }
}
