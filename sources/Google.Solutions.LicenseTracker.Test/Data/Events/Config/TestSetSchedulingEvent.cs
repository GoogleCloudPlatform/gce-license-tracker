﻿//
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
    internal class TestSetSchedulingEvent
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
                    'methodName': 'beta.compute.instances.setScheduling',
                    'authorizationInfo': [
                    ],
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                    'request': {
                      'onHostMaintenance': 'TERMINATE',
                      'automaticRestart': true,
                      'preemptible': false,
                      'nodeAffinitys': [
                        {
                          'key': 'compute.googleapis.com/node-group-name',
                          'operator': 'IN',
                          'values': [
                            'n1-overcommit'
                          ]
                        }
                      ],
                      'minNodeCpus': '4',
                      'provisioningModel': 'STANDARD',
                      '@type': 'type.googleapis.com/compute.instances.setScheduling'
                    },
                    'response': {
                      '@type': 'type.googleapis.com/operation'
                    },
                    'resourceLocation': {
                      'currentLocations': [
                        'asia-southeast1-b'
                      ]
                    }
                  },
                  'insertId': '-7hju21ee5ooy',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'project_id': 'project-1',
                      'instance_id': '49125100000000000',
                      'zone': 'asia-southeast1-b'
                    }
                  },
                  'timestamp': '2023-04-24T05:48:59.367796Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1682315339313-5fa0e903330c9-f387bbc8-0c2a9e22',
                    'producer': 'compute.googleapis.com',
                    'first': true
                  },
                  'receiveTimestamp': '2023-04-24T05:49:00.158061753Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetSchedulingEvent.IsSetSchedulingEvent(r));

            var e = (SetSchedulingEvent)r.ToEvent();

            Assert.AreEqual(49125100000000000, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("asia-southeast1-b", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);

            Assert.AreEqual(
                "TERMINATE",
                e.SchedulingPolicy?.MaintenancePolicy);
            Assert.AreEqual(4, e.SchedulingPolicy?.MinNodeCpus);
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
                    'methodName': 'beta.compute.instances.setScheduling',
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                    'request': {
                      '@type': 'type.googleapis.com/compute.instances.setScheduling'
                    }
                  },
                  'insertId': 'vn7lb2dfubq',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'instance_id': '49125100000000000',
                      'project_id': 'project-1',
                      'zone': 'asia-southeast1-b'
                    }
                  },
                  'timestamp': '2023-04-24T05:49:01.901287Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1682315339313-5fa0e903330c9-f387bbc8-0c2a9e22',
                    'producer': 'compute.googleapis.com',
                    'last': true
                  },
                  'receiveTimestamp': '2023-04-24T05:49:02.176189908Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetSchedulingEvent.IsSetSchedulingEvent(r));

            var e = (SetSchedulingEvent)r.ToEvent();

            Assert.AreEqual(49125100000000000, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("asia-southeast1-b", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual(
                new InstanceLocator("project-1", "asia-southeast1-b", "instance-1"),
                e.InstanceReference);
            Assert.IsNull(e.SchedulingPolicy);
        }
    }
}
