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

using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Data.Events;
using Google.Solutions.LicenseTracker.Data.Events.Lifecycle;
using Google.Solutions.LicenseTracker.Data.Logs;
using NUnit.Framework;

namespace Google.Solutions.LicenseTracker.Test.Data.Events.Lifecycle
{
    [TestFixture]
    public class TestBetaBulkInsertInstanceEvent 
    {
        [Test]
        public void WhenFirstOperation_ThenInstanceIsNull()
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
                'methodName': 'v1.compute.instances.bulkInsert',
                'authorizationInfo': [
                ],
                'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances',
                'request': {
                  'count': '3',
                  'namePattern': 'inst-####',
                  'instanceProperties': {
                    'machineType': 'e2-standard-2',
                    'networkInterfaces': [
                      {
                        'network': 'https://www.googleapis.com/compute/v1/projects/project-1/global/networks/default',
                        'subnetwork': 'https://www.googleapis.com/compute/v1/projects/project-1/regions/asia-southeast1/subnetworks/asia-southeast1',
                        'stackType': 'IPV4_ONLY'
                      }
                    ],
                    'disks': [
                      {
                        'type': 'PERSISTENT',
                        'mode': 'READ_WRITE',
                        'deviceName': 'instance-3',
                        'boot': true,
                        'initializeParams': {
                          'sourceImage': 'projects/debian-cloud/global/images/debian-11-bullseye-v20230411',
                          'diskSizeGb': '10',
                          'diskType': 'pd-balanced'
                        },
                        'autoDelete': true
                      }
                    ],
                    'scheduling': {
                      'onHostMaintenance': 'TERMINATE',
                      'preemptible': true
                    },
                    'labels': [
                      {
                        'key': 'label-1',
                        'value': ''
                      }
                    ]
                  },
                  '@type': 'type.googleapis.com/compute.instances.bulkInsert'
                },
                'response': {
                  'id': '1455973463846409613',
                  'name': 'operation-1683516256483-5fb262c70f6fc-5fa7b29b-30037aad',
                  'zone': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b',
                  'operationType': 'bulkInsert',
                  'targetLink': 'https://www.googleapis.com/compute/v1/projects/project-1',
                  'targetId': '884959775919',
                  'status': 'RUNNING',
                  'user': 'jpassing@google.com',
                  'progress': '0',
                  'insertTime': '2023-05-07T20:24:18.279-07:00',
                  'startTime': '2023-05-07T20:24:18.288-07:00',
                  'selfLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b/operations/operation-1683516256483-5fb262c70f6fc-5fa7b29b-30037aad',
                  'selfLinkWithId': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b/operations/1455973463846409613',
                  'operationGroupId': 'cbb02a73-5f61-4ca5-99dd-c521a8241d1d',
                  '@type': 'type.googleapis.com/operation'
                },
                'resourceLocation': {
                  'currentLocations': [
                    'asia-southeast1-b'
                  ]
                }
              },
              'insertId': 'lxqcofe7tkza',
              'resource': {
                'type': 'audited_resource',
                'labels': {
                  'method': 'compute.instances.bulkInsert',
                  'service': 'compute.googleapis.com',
                  'project_id': 'project-1'
                }
              },
              'timestamp': '2023-05-08T03:24:16.728126Z',
              'severity': 'NOTICE',
              'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
              'operation': {
                'id': 'operation-1683516256483-5fb262c70f6fc-5fa7b29b-30037aad',
                'producer': 'compute.googleapis.com',
                'first': true
              },
              'receiveTimestamp': '2023-05-08T03:24:19.331725006Z'
            }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(BulkInsertInstanceEvent.IsBulkInsertInstanceEvent(r));

            var e = (BulkInsertInstanceEvent)r.ToEvent();

            Assert.IsTrue(e.IsFirst);
            Assert.IsFalse(e.IsLast);
            Assert.AreEqual(0, e.InstanceId);
        }

        [Test]
        public void WhenLastOperation_ThenFieldsAreExtracted()
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
                    'methodName': 'v1.compute.instances.bulkInsert',
                    'authorizationInfo': [
                    ],
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/inst-0003',
                    'request': {
                      '@type': 'type.googleapis.com/compute.instances.bulkInsert'
                    }
                  },
                  'insertId': '-dvajh0e1yca4',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'instance_id': '8693801480000000000',
                      'project_id': 'project-1',
                      'zone': 'asia-southeast1-b'
                    }
                  },
                  'timestamp': '2023-05-08T03:24:24.732468Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1683516259402-5fb262c9d8409-aac872ad-92d014db',
                    'producer': 'compute.googleapis.com',
                    'first': true,
                    'last': true
                  },
                  'receiveTimestamp': '2023-05-08T03:24:25.274282410Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(BulkInsertInstanceEvent.IsBulkInsertInstanceEvent(r));

            var e = (BulkInsertInstanceEvent)r.ToEvent();

            Assert.IsTrue(e.IsFirst);
            Assert.IsTrue(e.IsLast);
            Assert.AreEqual(8693801480000000000, e.InstanceId);
            Assert.AreEqual("inst-0003", e.InstanceReference?.Name);
            Assert.AreEqual("asia-southeast1-b", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.IsFalse(e.IsError);
            Assert.IsTrue(e.IsStartingInstance);
            Assert.IsFalse(e.IsTerminatingInstance);
            Assert.AreEqual(
                new InstanceLocator("project-1", "asia-southeast1-b", "inst-0003"),
                e.InstanceReference);
        }
    }
}
