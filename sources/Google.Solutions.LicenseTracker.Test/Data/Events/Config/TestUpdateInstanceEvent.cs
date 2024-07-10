//
// Copyright 2023 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.LicenseTracker.Data.Events;
using Google.Solutions.LicenseTracker.Data.Events.Config;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Data.Logs;
using NUnit.Framework;

namespace Google.Solutions.LicenseTracker.Test.Data.Events.Config
{
    internal class TestUpdateInstanceEvent
    {
        [Test]
        public void WhenFirstOperation_ThenMachineTypeIsSet()
        {
            var json = @"
                {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'authenticationInfo': {
                    },
                    'requestMetadata': {
                    },
                    'serviceName': 'compute.googleapis.com',
                    'methodName': 'beta.compute.instances.update',
                    'authorizationInfo': [
                      {
                      }
                    ],
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                    'request': {
                      'name': 'instance-1',
                      'description': '',
                      'tags': {
                        'fingerprint': '�e�J�|�#'
                      },
                      'machineType': 'projects/project-1/zones/asia-southeast1-b/machineTypes/e2-micro',
                      'canIpForward': false,
                      'networkInterfaces': [
                        {
                          'network': 'https://www.googleapis.com/compute/beta/projects/project-1/global/networks/default',
                          'networkIP': '10.165.0.12',
                          'name': 'nic0',
                          'subnetwork': 'https://www.googleapis.com/compute/beta/projects/project-1/regions/asia-southeast1/subnetworks/asia-southeast1',
                          'fingerprint': '%v\u0018���#',
                          'stackType': 'IPV4_ONLY'
                        }
                      ],
                      'disks': [
                        {
                          'type': 'PERSISTENT',
                          'mode': 'READ_WRITE',
                          'source': 'https://www.googleapis.com/compute/beta/projects/project-1/zones/asia-southeast1-b/disks/instance-1',
                          'deviceName': 'debian-1',
                          'boot': true,
                          'autoDelete': true,
                          'interface': 'SCSI',
                          'guestOsFeatures': [
                            {
                              'type': 'UEFI_COMPATIBLE'
                            },
                            {
                              'type': 'VIRTIO_SCSI_MULTIQUEUE'
                            }
                          ],
                          'diskSizeGb': '10'
                        }
                      ],
                      'scheduling': {
                        'onHostMaintenance': 'MIGRATE',
                        'automaticRestart': true,
                        'preemptible': false,
                        'provisioningModel': 'STANDARD'
                      },
                      'labels': [
                        {
                          'value': 'value-1',
                          'key': 'label-1'
                        },
                        {
                          'key': 'label-2',
                          'value': ''
                        }
                      ],
                      'labelFingerprint': '�e�J�|�#',
                      'deletionProtection': false,
                      'displayDevice': {
                        'enableDisplay': false
                      },
                      'shieldedInstanceConfig': {
                        'enableSecureBoot': false,
                        'enableVtpm': true,
                        'enableIntegrityMonitoring': true
                      },
                      'shieldedInstanceIntegrityPolicy': {
                        'updateAutoLearnPolicy': true
                      },
                      'confidentialInstanceConfig': {
                        'enableConfidentialCompute': false
                      },
                      'fingerprint': 'At6Oswjy',
                      'keyRevocationActionType': 'NONE_ON_KEY_REVOCATION',
                      '@type': 'type.googleapis.com/compute.instances.update'
                    },
                    'response': {
                      'id': '706532201',
                      'name': 'operation-1682310997625-5fa0d8d6a4ae6-0c729f56-b2c12260',
                      'zone': 'https://www.googleapis.com/compute/beta/projects/project-1/zones/asia-southeast1-b',
                      'operationType': 'update',
                      'targetLink': 'https://www.googleapis.com/compute/beta/projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                      'targetId': '49125100000000000',
                      'status': 'RUNNING',
                      'user': 'jpassing@google.com',
                      'progress': '0',
                      'insertTime': '2023-04-23T21:36:38.514-07:00',
                      'startTime': '2023-04-23T21:36:38.530-07:00',
                      'selfLink': 'https://www.googleapis.com/compute/...',
                      'selfLinkWithId': 'https://www.googleapis.com/comput...',
                      '@type': 'type.googleapis.com/operation'
                    },
                    'resourceLocation': {
                      'currentLocations': [
                        'asia-southeast1-b'
                      ]
                    }
                  },
                  'insertId': '-cqzcade7o19i',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'project_id': 'project-1',
                      'instance_id': '49125100000000000',
                      'zone': 'asia-southeast1-b'
                    }
                  },
                  'timestamp': '2023-04-24T04:36:37.687084Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1682310997625-5fa0d8d6a4ae6-0c729f56-b2c12260',
                    'producer': 'compute.googleapis.com',
                    'first': true
                  },
                  'receiveTimestamp': '2023-04-24T04:36:39.563259397Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(UpdateInstanceEvent.IsUpdateInstanceEvent(r));

            var e = (UpdateInstanceEvent)r.ToEvent();

            Assert.AreEqual(49125100000000000, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("asia-southeast1-b", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual(
                new InstanceLocator("project-1", "asia-southeast1-b", "instance-1"),
                e.InstanceReference);
            Assert.AreEqual(
                new MachineTypeLocator("project-1", "asia-southeast1-b", "e2-micro"),
                e.MachineType);
            Assert.AreEqual(
                "MIGRATE",
                e.SchedulingPolicy?.MaintenancePolicy);
            Assert.IsNull(e.SchedulingPolicy?.MinNodeCpus);

            Assert.IsNotNull(e.Labels);
            CollectionAssert.AreEqual(
                new[] { "label-1", "label-2" },
                e.Labels!.Keys);

            Assert.AreEqual("value-1", e.Labels["label-1"]);
            Assert.AreEqual("", e.Labels["label-2"]);
        }

        [Test]
        public void WhenInstanceUsesCpuOvercommit_ThenSchedulingPolicyIsSet()
        {
            var json = @"
                {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'authenticationInfo': {
                    },
                    'requestMetadata': {
                    },
                    'serviceName': 'compute.googleapis.com',
                    'methodName': 'beta.compute.instances.update',
                    'authorizationInfo': [
                    ],
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                    'request': {
                      'name': 'instance-1',
                      'description': '',
                      'tags': {
                        'fingerprint': '�e�J�|�#'
                      },
                      'machineType': 'projects/project-1/zones/asia-southeast1-b/machineTypes/n1-standard-8',
                      'canIpForward': false,
                      'networkInterfaces': [
                        {
                          'network': 'https://www.googleapis.com/compute/beta/projects/project-1/global/networks/default',
                          'networkIP': '10.165.15.198',
                          'name': 'nic0',
                          'subnetwork': 'https://www.googleapis.com/compute/beta/projects/project-1/regions/asia-southeast1/subnetworks/asia-southeast1',
                          'fingerprint': '\b�s��\u0006;',
                          'stackType': 'IPV4_ONLY'
                        }
                      ],
                      'disks': [
                        {
                          'type': 'PERSISTENT',
                          'mode': 'READ_WRITE',
                          'source': 'https://www.googleapis.com/compute/beta/projects/project-1/zones/asia-southeast1-b/disks/instance-1',
                          'deviceName': 'debian-1',
                          'boot': true,
                          'autoDelete': true,
                          'interface': 'SCSI',
                          'guestOsFeatures': [
                            {
                              'type': 'UEFI_COMPATIBLE'
                            },
                            {
                              'type': 'VIRTIO_SCSI_MULTIQUEUE'
                            },
                            {
                              'type': 'GVNIC'
                            }
                          ],
                          'diskSizeGb': '10'
                        }
                      ],
                      'scheduling': {
                        'onHostMaintenance': 'TERMINATE',
                        'automaticRestart': true,
                        'preemptible': false,
                        'nodeAffinitys': [
                          {
                            'key': 'compute.googleapis.com/node-name',
                            'operator': 'IN',
                            'values': [
                              'n1-overcommit-n7xg'
                            ]
                          }
                        ],
                        'minNodeCpus': '4',
                        'provisioningModel': 'STANDARD'
                      },
                      'labelFingerprint': '�e�J�|�#',
                      'deletionProtection': false,
                      'displayDevice': {
                        'enableDisplay': false
                      },
                      'shieldedInstanceConfig': {
                        'enableSecureBoot': false,
                        'enableVtpm': true,
                        'enableIntegrityMonitoring': true
                      },
                      'shieldedInstanceIntegrityPolicy': {
                        'updateAutoLearnPolicy': true
                      },
                      'confidentialInstanceConfig': {
                        'enableConfidentialCompute': false
                      },
                      'fingerprint': '��^�v艽',
                      'keyRevocationActionType': 'NONE_ON_KEY_REVOCATION',
                      '@type': 'type.googleapis.com/compute.instances.update'
                    },
                    'response': {
                    },
                    'resourceLocation': {
                      'currentLocations': [
                        'asia-southeast1-b'
                      ]
                    }
                  },
                  'insertId': 'gde4j3e39luq',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'instance_id': '49125100000000000',
                      'project_id': 'project-1',
                      'zone': 'asia-southeast1-b'
                    }
                  },
                  'timestamp': '2023-04-24T05:49:15.346522Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1682315355280-5fa0e9126d492-8be81ea8-2ae3396e',
                    'producer': 'compute.googleapis.com',
                    'first': true
                  },
                  'receiveTimestamp': '2023-04-24T05:49:16.542365712Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(UpdateInstanceEvent.IsUpdateInstanceEvent(r));

            var e = (UpdateInstanceEvent)r.ToEvent();
            Assert.AreEqual(
                "TERMINATE",
                e.SchedulingPolicy?.MaintenancePolicy);
            Assert.AreEqual(4, e.SchedulingPolicy?.MinNodeCpus);
            Assert.IsNull(e.Labels);
        }

        [Test]
        public void WhenLastOperation_ThenMachineTypeIsNull()
        {
            var json = @"
                {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'authenticationInfo': {
                    },
                    'requestMetadata': {
                    },
                    'serviceName': 'compute.googleapis.com',
                    'methodName': 'beta.compute.instances.update',
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                    'request': {
                      '@type': 'type.googleapis.com/compute.instances.update'
                    }
                  },
                  'insertId': '-l2yfjtd6c70',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'zone': 'asia-southeast1-b',
                      'instance_id': '49125100000000000',
                      'project_id': 'project-1'
                    }
                  },
                  'timestamp': '2023-04-24T04:36:39.347512Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1682310997625-5fa0d8d6a4ae6-0c729f56-b2c12260',
                    'producer': 'compute.googleapis.com',
                    'last': true
                  },
                  'receiveTimestamp': '2023-04-24T04:36:39.659682857Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(UpdateInstanceEvent.IsUpdateInstanceEvent(r));

            var e = (UpdateInstanceEvent)r.ToEvent();

            Assert.AreEqual(49125100000000000, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("asia-southeast1-b", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual(
                new InstanceLocator("project-1", "asia-southeast1-b", "instance-1"),
                e.InstanceReference);
            Assert.IsNull(e.MachineType);
            Assert.IsNull(e.SchedulingPolicy);
        }
    }
}
