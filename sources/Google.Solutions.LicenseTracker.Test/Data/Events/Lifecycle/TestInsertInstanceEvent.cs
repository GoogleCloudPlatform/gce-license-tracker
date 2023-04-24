//
// Copyright 2022 Google LLC
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

namespace Google.Solutions.LicenseTracker.Data.Test.Events.Lifecycle
{
    [TestFixture]
    public class TestInsertInstanceEvent 
    {
        [Test]
        public void WhenInstanceUsesImage_ThenFieldsAreExtracted()
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
                 'methodName': 'v1.compute.instances.insert',
                 'authorizationInfo': [
                 ],
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                   'name': 'instance-group-1-xbtt',
                   'machineType': 'projects/project-1/zones/us-central1-a/machineTypes/n1-standard-4',
                   'canIpForward': false,
                   'networkInterfaces': [
                     {
                       'network': 'projects/project-1/global/networks/default',
                       'accessConfigs': [
                         {
                           'type': 'ONE_TO_ONE_NAT',
                           'name': 'External NAT',
                           'networkTier': 'PREMIUM'
                         }
                       ],
                       'subnetwork': 'projects/project-1/regions/us-central1/subnetworks/default'
                     }
                   ],
                   'disks': [
                     {
                       'type': 'PERSISTENT',
                       'mode': 'READ_WRITE',
                       'deviceName': 'instance-1',
                       'boot': true,
                       'initializeParams': {
                         'sourceImage': 'projects/project-1/global/images/image-1',
                         'diskSizeGb': '127',
                         'diskType': 'projects/project-1/zones/us-central1-a/diskTypes/pd-standard'
                       },
                       'autoDelete': true
                     }
                   ],
                   'serviceAccounts': [
                     {
                       'email': '111-compute@developer.gserviceaccount.com',
                       'scopes': [
                         'https://www.googleapis.com/auth/devstorage.read_only',
                         'https://www.googleapis.com/auth/logging.write',
                         'https://www.googleapis.com/auth/monitoring.write',
                         'https://www.googleapis.com/auth/servicecontrol',
                         'https://www.googleapis.com/auth/service.management.readonly',
                         'https://www.googleapis.com/auth/trace.append'
                       ]
                     }
                   ],
                   'scheduling': {
                     'onHostMaintenance': 'TERMINATE',
                     'automaticRestart': false,
                     'preemptible': false,
                     'nodeAffinitys': [
                       {
                         'key': 'license',
                         'operator': 'IN',
                         'values': [
                           'byol'
                         ]
                       }
                     ]
                   },
                   'displayDevice': {
                     'enableDisplay': false
                   },
                   'links': [
                     {
                       'target': 'projects/project-1/locations/us-central1-a/instances/instance-group-1-xbtt',
                       'type': 'MEMBER_OF',
                       'source': 'projects/project-1/locations/us-central1-a/instanceGroupManagers/instance-group-1@3579973466633327805'
                     }
                   ],
                   'requestId': '4a68f20d-9f80-32f3-adc4-acf842d7ae0b',
                   '@type': 'type.googleapis.com/compute.instances.insert'
                 },
                 'response': {
                   'id': '5042353291971988238',
                   'name': 'operation-1588508129141-5a4bd5ec2a16d-418ba83e-11fc353d',
                   'zone': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a',
                   'clientOperationId': '4a68f20d-9f80-32f3-adc4-acf842d7ae0b',
                   'operationType': 'insert',
                   'targetLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/instances/instance-group-1-xbtt',
                   'targetId': '518436304627895054',
                   'status': 'RUNNING',
                   'user': '111@cloudservices.gserviceaccount.com',
                   'progress': '0',
                   'insertTime': '2020-05-03T05:15:29.813-07:00',
                   'startTime': '2020-05-03T05:15:29.817-07:00',
                   'selfLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/operations/operation-1588508129141-5a4bd5ec2a16d-418ba83e-11fc353d',
                   'selfLinkWithId': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/operations/5042353291971988238',
                   '@type': 'type.googleapis.com/operation'
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': '3vuqdhe1iqbu',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'zone': 'us-central1-a',
                   'instance_id': '11111111631960822',
                   'project_id': 'project-1'
                 }
               },
               'timestamp': '2020-05-03T12:15:29.009Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1588508129141-5a4bd5ec2a16d-418ba83e-11fc353d',
                 'producer': 'compute.googleapis.com',
                 'first': true
               },
               'receiveTimestamp': '2020-05-03T12:15:30.903794912Z'
             }
             ";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(InsertInstanceEvent.IsInsertInstanceEvent(r));

            var e = (InsertInstanceEvent)r.ToEvent();

            Assert.AreEqual(11111111631960822, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("us-central1-a", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.AreEqual(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
            Assert.AreEqual(
                new ImageLocator("project-1", "image-1"),
                e.Image);
            Assert.AreEqual(
                new MachineTypeLocator("project-1", "us-central1-a", "n1-standard-4"),
                e.MachineType);
        }

        [Test]
        public void WhenSeverityIsError_ThenFieldsAreExtracted()
        {
            var json = @"
               {
                'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'status': {
                    'code': 3,
                    'message': 'INVALID_ARGUMENT'
                    },
                    'authenticationInfo': {
                    },
                    'requestMetadata': {
                    'callerIp': '34.91.94.164',
                    'callerSuppliedUserAgent': 'google-cloud-sdk',
                    'requestAttributes': {},
                    'destinationAttributes': {}
                    },
                    'serviceName': 'compute.googleapis.com',
                    'methodName': 'v1.compute.instances.insert',
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                    '@type': 'type.googleapis.com/compute.instances.insert'
                    }
                },
                'insertId': '-vwncp9d6006',
                'resource': {
                    'type': 'gce_instance',
                    'labels': {
                    'zone': 'us-central1-a',
                    'instance_id': '11111111631960822',
                    'project_id': 'project-1'
                    }
                },
                'timestamp': '2020-04-24T08:13:39.103Z',
                'severity': 'ERROR',
                'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                'operation': {
                    'id': 'operation-1587715943067-5a404ecca6fa4-dc7e343f-dbc3ca83',
                    'producer': 'compute.googleapis.com',
                    'last': true
                },
                'receiveTimestamp': '2020-04-24T08:13:40.134230447Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(InsertInstanceEvent.IsInsertInstanceEvent(r));

            var e = (InsertInstanceEvent)r.ToEvent();

            Assert.AreEqual(11111111631960822, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("us-central1-a", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual("ERROR", e.Severity);
            Assert.AreEqual(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
            Assert.IsNull(e.Image);
            Assert.IsNull(e.MachineType);
        }

        [Test]
        public void WhenInstanceUsesZonalImageFamilyView_ThenFieldsAreExtracted()
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
                    'methodName': 'v1.compute.instances.insert',
                    'authorizationInfo': [ ],
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                      'networkInterfaces': [
                        {
                          'subnetwork': 'https://compute.googleapis.com/compute/beta/projects/project-1/regions/us-central1/subnetworks/us-central1'
                        }
                      ],
                      'machineType': 'projects/project-1/zones/us-central1-a/machineTypes/e2-standard-2',
                      'disks': [
                        {
                          'initializeParams': {
                            'sourceImage': 'projects/windows-cloud/zones/-/imageFamilyViews/windows-2022'
                          },
                          'boot': true
                        }
                      ],
                      'name': 'instance-1',
                      '@type': 'type.googleapis.com/compute.instances.insert'
                    },
                    'response': {
                      '@type': 'type.googleapis.com/operation',
                      'targetId': '111',
                      'user': 'jpassing@google.com',
                      'selfLinkWithId': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/operations/2464248364097669279',
                      'startTime': '2022-07-11T16:46:56.854-07:00',
                      'id': '2464248364097669279',
                      'zone': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a',
                      'status': 'RUNNING',
                      'selfLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/operations/operation-1657583215653-5e3902ac13463-7e1528c1-503810fa',
                      'targetLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/instances/instance-1',
                      'progress': '0',
                      'insertTime': '2022-07-11T16:46:56.853-07:00',
                      'name': 'operation-1657583215653-5e3902ac13463-7e1528c1-503810fa',
                      'operationType': 'insert'
                    },
                    'resourceLocation': {
                      'currentLocations': [
                        'us-central1-a'
                      ]
                    }
                  },
                  'insertId': 'mr5yvue2rr4a',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'instance_id': '111',
                      'project_id': '111',
                      'zone': 'us-central1-a'
                    }
                  },
                  'timestamp': '2022-07-11T23:46:55.701048Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1657583215653-5e3902ac13463-7e1528c1-503810fa',
                    'producer': 'compute.googleapis.com',
                    'first': true
                  },
                  'receiveTimestamp': '2022-07-11T23:46:57.870337362Z'
                }
             ";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(InsertInstanceEvent.IsInsertInstanceEvent(r));

            var e = (InsertInstanceEvent)r.ToEvent();

            Assert.AreEqual(111, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("us-central1-a", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.AreEqual(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
            Assert.AreEqual(
                new ImageFamilyViewLocator("windows-cloud", "us-central1-a", "windows-2022"),
                e.Image);
        }

        [Test]
        public void WhenInstanceUsesGlobalImageFamily_ThenFieldsAreExtracted()
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
                    'methodName': 'v1.compute.instances.insert',
                    'authorizationInfo': [ ],
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                      'networkInterfaces': [
                        {
                          'subnetwork': 'https://compute.googleapis.com/compute/beta/projects/project-1/regions/us-central1/subnetworks/us-central1'
                        }
                      ],
                      'machineType': 'projects/project-1/zones/us-central1-a/machineTypes/e2-standard-2',
                      'disks': [
                        {
                          'initializeParams': {
                            'sourceImage': 'projects/windows-cloud/global/images/family/windows-2022'
                          },
                          'boot': true
                        }
                      ],
                      'name': 'instance-1',
                      '@type': 'type.googleapis.com/compute.instances.insert'
                    },
                    'response': {
                      '@type': 'type.googleapis.com/operation',
                      'targetId': '111',
                      'user': 'jpassing@google.com',
                      'selfLinkWithId': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/operations/2464248364097669279',
                      'startTime': '2022-07-11T16:46:56.854-07:00',
                      'id': '2464248364097669279',
                      'zone': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a',
                      'status': 'RUNNING',
                      'selfLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/operations/operation-1657583215653-5e3902ac13463-7e1528c1-503810fa',
                      'targetLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/instances/instance-1',
                      'progress': '0',
                      'insertTime': '2022-07-11T16:46:56.853-07:00',
                      'name': 'operation-1657583215653-5e3902ac13463-7e1528c1-503810fa',
                      'operationType': 'insert'
                    },
                    'resourceLocation': {
                      'currentLocations': [
                        'us-central1-a'
                      ]
                    }
                  },
                  'insertId': 'mr5yvue2rr4a',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'instance_id': '111',
                      'project_id': '111',
                      'zone': 'us-central1-a'
                    }
                  },
                  'timestamp': '2022-07-11T23:46:55.701048Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1657583215653-5e3902ac13463-7e1528c1-503810fa',
                    'producer': 'compute.googleapis.com',
                    'first': true
                  },
                  'receiveTimestamp': '2022-07-11T23:46:57.870337362Z'
                }
             ";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(InsertInstanceEvent.IsInsertInstanceEvent(r));

            var e = (InsertInstanceEvent)r.ToEvent();

            Assert.AreEqual(111, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("us-central1-a", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.AreEqual(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
            Assert.AreEqual(
                new ImageLocator("windows-cloud", "family/windows-2022"),
                e.Image);
        }
    }
}
