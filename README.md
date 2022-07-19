# License Tracker

License Tracker is a tool that lets you track VM and sole-tenant node usage for
the purpose of license reporting. 

The tool is designed to be deployed as a [Cloud Run job](https://cloud.google.com/run/docs/create-jobs)
and invoked periodically, typically once a day. The tool analyzes the Compute Engine audit logs of your
Google Cloud projects and writes its results to a BigQuery dataset. You can then use this dataset
to visualize VM and sole-tenant node usage and determine your consumption of BYOL licenses:

![Architecture](https://cloud.google.com/static/compute/images/tracking-vm-and-sole-tenant-usage-architecture.svg)

For detailed instructions on deploying and using the License Tracker tool, see 
[Tracking VM and sole-tenant node usage for license reporting](https://cloud.google.com/compute/docs/nodes/determining-server-usage)
on the Google Cloud website.

--- 

_License Tracker is an open-source project and not an officially supported Google product._

_All files in this repository are under the
[Apache License, Version 2.0](LICENSE.txt) unless noted otherwise._
