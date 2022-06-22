# License Tracker

License Tracker is a command-line tool that lets you track VM and sole-tenant
node usage for the purpose of license reporting.

The tool works by analyzing Compute Engine audit logs to determine _placements_ 
for each VM instance. A placement describes the time period during which
a VM instance is running on a specific physical server. Each time a VM is migrated 
from one physical server to another marks the end of one placement and starts another.

When you run the License Tracker tool the first time, it analyzes Compute Engine
usage of the past 90 days and writes its result to BigQuery. On subsequent runs,
the tool analyzes usage that occurred between its last invocation and the beginning
of the current day (0:00 UTC) and updates the BigQuery dataset accordingly.

For detailed instructions on deploying and using the License Tracker tool, see 
[Tracking VM and sole-tenant node usage for license reportings](https://cloud.google.com/compute/docs/nodes/determining-server-usage)
on the Google Cloud website.

--- 

_License Tracker is an open-source project and not an officially supported Google product._

_All files in this repository are under the
[Apache License, Version 2.0](LICENSE.txt) unless noted otherwise._
