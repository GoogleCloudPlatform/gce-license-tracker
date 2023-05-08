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
    internal class TestSetMachineTypeEvent
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
                    'methodName': 'v1.compute.instances.setMachineType',
                    'authorizationInfo': [
                      {
                      }
                    ],
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                    'request': {
                      'machineType': 'projects/project-1/zones/asia-southeast1-b/machineTypes/e2-medium',
                      '@type': 'type.googleapis.com/compute.instances.setMachineType'
                    },
                    'response': {
                      'id': '1751757467700197999',
                      'name': 'operation-1682293887661-5fa099194f5bc-15b3d8b2-b5f3fefa',
                      'zone': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b',
                      'operationType': 'setMachineType',
                      'targetLink': 'https://www.googleapis.com/compute/...',
                      'targetId': '34596100000000',
                      'status': 'RUNNING',
                      'progress': '0',
                      'insertTime': '2023-04-23T16:51:28.317-07:00',
                      'startTime': '2023-04-23T16:51:28.331-07:00',
                      'selfLink': 'https://www.googleapis.com/compute/...',
                      'selfLinkWithId': 'https://www.googleapis.com/compute/...',
                      '@type': 'type.googleapis.com/operation'
                    },
                    'resourceLocation': {
                      'currentLocations': [
                        'asia-southeast1-b'
                      ]
                    }
                  },
                  'insertId': 't5ar07ebpnws',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'instance_id': '34596100000000',
                      'project_id': 'project-1',
                      'zone': 'asia-southeast1-b'
                    }
                  },
                  'timestamp': '2023-04-23T23:51:27.716314Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1682293887661-5fa099194f5bc-15b3d8b2-b5f3fefa',
                    'producer': 'compute.googleapis.com',
                    'first': true
                  },
                  'receiveTimestamp': '2023-04-23T23:51:28.545118725Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetMachineTypeEvent.IsSetMachineTypeEvent(r));

            var e = (SetMachineTypeEvent)r.ToEvent();

            Assert.AreEqual(34596100000000, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("asia-southeast1-b", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual(
                new InstanceLocator("project-1", "asia-southeast1-b", "instance-1"),
                e.InstanceReference);
            Assert.AreEqual(
                new MachineTypeLocator("project-1", "asia-southeast1-b", "e2-medium"),
                e.MachineType);
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
                    'methodName': 'v1.compute.instances.setMachineType',
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                    'request': {
                      '@type': 'type.googleapis.com/compute.instances.setMachineType'
                    }
                  },
                  'insertId': '-13o0p5dgu6u',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'project_id': 'project-1',
                      'instance_id': '34596100000000',
                      'zone': 'asia-southeast1-b'
                    }
                  },
                  'timestamp': '2023-04-23T23:51:28.475695Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1682293887661-5fa099194f5bc-15b3d8b2-b5f3fefa',
                    'producer': 'compute.googleapis.com',
                    'last': true
                  },
                  'receiveTimestamp': '2023-04-23T23:51:28.937861249Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetMachineTypeEvent.IsSetMachineTypeEvent(r));

            var e = (SetMachineTypeEvent)r.ToEvent();

            Assert.AreEqual(34596100000000, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("asia-southeast1-b", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual(
                new InstanceLocator("project-1", "asia-southeast1-b", "instance-1"),
                e.InstanceReference);
            Assert.IsNull(e.MachineType);
        }
    }
}
