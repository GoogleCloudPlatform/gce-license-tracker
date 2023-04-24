using Google.Solutions.LicenseTracker.Data.Events;
using Google.Solutions.LicenseTracker.Data.Events.Config;
using Google.Solutions.LicenseTracker.Data.Events.Lifecycle;
using Google.Solutions.LicenseTracker.Data.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LicenseTracker.Data.History
{
    /// <summary>
    /// Reconstructs the history of machine type configurations
    /// for a given instance by analyzing events in reverse 
    /// chronological order.
    /// </summary>
    public class MachineTypeConfigurationHistoryBuilder
        : ConfigurationHistoryBuilderBase<MachineTypeLocator>
    {
        public MachineTypeConfigurationHistoryBuilder(
            ulong instanceId,
            MachineTypeLocator? currentMachineType)
            : base(instanceId, currentMachineType)
        {
        }

        public override void ProcessEvent(EventBase e)
        {
            if (e is InsertInstanceEvent insert && insert.MachineType != null)
            {
                this.changes.AddLast(new ConfigurationChange<MachineTypeLocator>(
                    insert.Timestamp,
                    insert.MachineType));
            }
            else if (e is SetMachineTypeEvent setType && setType.MachineType != null)
            {
                this.changes.AddLast(new ConfigurationChange<MachineTypeLocator>(
                    setType.Timestamp,
                    setType.MachineType));
            }
            else if (e is UpdateInstanceEvent update && update.MachineType != null)
            {
                this.changes.AddLast(new ConfigurationChange<MachineTypeLocator>(
                    update.Timestamp,
                    update.MachineType));
            }
            else
            {
                //
                // This event is not relevant for us.
                //
            }
        }
    }
}
