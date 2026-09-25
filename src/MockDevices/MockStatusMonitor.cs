using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace EssentialsDemoRoom.MockDevices
{
    /// <summary>
    /// A <see cref="StatusMonitorBase"/> whose status is set directly rather than derived from
    /// polling a real connection. <see cref="StatusMonitorBase.Status"/>'s setter is protected, so
    /// this exposes <see cref="SetStatus"/> for mock devices that have no real communication to
    /// monitor and no need for the base class's timer-driven warning/error escalation.
    /// </summary>
    public class MockStatusMonitor : StatusMonitorBase
    {
        public MockStatusMonitor(IKeyed parent) : base(parent, 30000, 60000) { }

        /// <inheritdoc />
        /// <remarks>No-op: there is no real connection to start monitoring.</remarks>
        public override void Start() { }

        /// <inheritdoc />
        /// <remarks>No-op: there is no real connection to stop monitoring.</remarks>
        public override void Stop() { }

        /// <summary>
        /// Sets the monitor's current status.
        /// </summary>
        public void SetStatus(MonitorStatus status) => Status = status;
    }
}
