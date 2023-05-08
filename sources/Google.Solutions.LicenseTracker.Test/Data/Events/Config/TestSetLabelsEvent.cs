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
    internal class TestSetLabelsEvent
    {
        [Test]
        public void WhenFirstOperationAndLabelsNotEmpty_ThenLabelsAreSet()
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
                    'methodName': 'v1.compute.instances.setLabels',
                    'authorizationInfo': [
                    ],
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                    'request': {
                      'labels': [
                        {
                          'key': 'label-1',
                          'value': 'value-1'
                        },
                        {
                          'key': 'label-2',
                          'value': 'value-2'
                        }
                      ],
                      'labelFingerprint': '�e�J�|�#',
                      '@type': 'type.googleapis.com/compute.instances.setLabels'
                    },
                    'response': {
                      'id': '8627959985012578103',
                      'name': 'operation-1683499480843-5fb224489037d-88c23c8c-48058ca8',
                      'zone': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b',
                      'operationType': 'compute.instance.setLabels',
                      'targetLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                      'targetId': '1718410000000000000',
                      'status': 'RUNNING',
                      'user': 'jpassing@google.com',
                      'progress': '0',
                      'insertTime': '2023-05-07T15:44:40.965-07:00',
                      'startTime': '2023-05-07T15:44:40.967-07:00',
                      'selfLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b/operations/operation-1683499480843-5fb224489037d-88c23c8c-48058ca8',
                      'selfLinkWithId': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b/operations/8627959985012578103',
                      '@type': 'type.googleapis.com/operation'
                    },
                    'resourceLocation': {
                      'currentLocations': [
                        'asia-southeast1-b'
                      ]
                    }
                  },
                  'insertId': 'x10zite877ko',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'project_id': 'project-1',
                      'instance_id': '1718410000000000000',
                      'zone': 'asia-southeast1-b'
                    }
                  },
                  'timestamp': '2023-05-07T22:44:40.873337Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1683499480843-5fb224489037d-88c23c8c-48058ca8',
                    'producer': 'compute.googleapis.com',
                    'first': true
                  },
                  'receiveTimestamp': '2023-05-07T22:44:41.709095788Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetLabelsEvent.IsSetLabelsEvent(r));

            var e = (SetLabelsEvent)r.ToEvent();

            Assert.AreEqual("NOTICE", e.Severity);
            Assert.AreEqual(1718410000000000000, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("asia-southeast1-b", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual(
                new InstanceLocator("project-1", "asia-southeast1-b", "instance-1"),
                e.InstanceReference);

            Assert.IsNotNull(e.Labels);
            CollectionAssert.AreEqual(
                new[] { "label-1", "label-2" },
                e.Labels!.Keys);

            Assert.AreEqual("value-1", e.Labels["label-1"]);
            Assert.AreEqual("value-2", e.Labels["label-2"]);
        }

        [Test]
        public void WhenFirstOperationAndLabelsEmpty_ThenLabelsAreSet()
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
                    'methodName': 'v1.compute.instances.setLabels',
                    'authorizationInfo': [
                    ],
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                    'request': {
                      'labelFingerprint': '�e�J�|�#',
                      '@type': 'type.googleapis.com/compute.instances.setLabels'
                    },
                    'response': {
                      'id': '8627959985012578103',
                      'name': 'operation-1683499480843-5fb224489037d-88c23c8c-48058ca8',
                      'zone': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b',
                      'operationType': 'compute.instance.setLabels',
                      'targetLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                      'targetId': '1718410000000000000',
                      'status': 'RUNNING',
                      'user': 'jpassing@google.com',
                      'progress': '0',
                      'insertTime': '2023-05-07T15:44:40.965-07:00',
                      'startTime': '2023-05-07T15:44:40.967-07:00',
                      'selfLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b/operations/operation-1683499480843-5fb224489037d-88c23c8c-48058ca8',
                      'selfLinkWithId': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/asia-southeast1-b/operations/8627959985012578103',
                      '@type': 'type.googleapis.com/operation'
                    },
                    'resourceLocation': {
                      'currentLocations': [
                        'asia-southeast1-b'
                      ]
                    }
                  },
                  'insertId': 'x10zite877ko',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'project_id': 'project-1',
                      'instance_id': '1718410000000000000',
                      'zone': 'asia-southeast1-b'
                    }
                  },
                  'timestamp': '2023-05-07T22:44:40.873337Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1683499480843-5fb224489037d-88c23c8c-48058ca8',
                    'producer': 'compute.googleapis.com',
                    'first': true
                  },
                  'receiveTimestamp': '2023-05-07T22:44:41.709095788Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetLabelsEvent.IsSetLabelsEvent(r));

            var e = (SetLabelsEvent)r.ToEvent();

            Assert.AreEqual("NOTICE", e.Severity);
            Assert.AreEqual(1718410000000000000, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("asia-southeast1-b", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual(
                new InstanceLocator("project-1", "asia-southeast1-b", "instance-1"),
                e.InstanceReference);
            Assert.IsNull(e.Labels);
        }

        [Test]
        public void WhenLastOperation_ThenLabelsIsEmpty()
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
                    'methodName': 'v1.compute.instances.setLabels',
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                    'request': {
                      '@type': 'type.googleapis.com/compute.instances.setLabels'
                    }
                  },
                  'insertId': '-1nuel3dln14',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'zone': 'asia-southeast1-b',
                      'project_id': 'project-1',
                      'instance_id': '1718410000000000000'
                    }
                  },
                  'timestamp': '2023-05-07T22:55:31.901094Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1683500130103-5fb226b3bf282-13608163-11e0059b',
                    'producer': 'compute.googleapis.com',
                    'last': true
                  },
                  'receiveTimestamp': '2023-05-07T22:55:32.235267077Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetLabelsEvent.IsSetLabelsEvent(r));

            var e = (SetLabelsEvent)r.ToEvent();

            Assert.AreEqual("NOTICE", e.Severity);
            Assert.AreEqual(1718410000000000000, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("asia-southeast1-b", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual(
                new InstanceLocator("project-1", "asia-southeast1-b", "instance-1"),
                e.InstanceReference);

            Assert.IsNull(e.Labels);
        }

        [Test]
        public void WhenOperationFailed_ThenLabelsAreSet()
        {
            var json = @"
               {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'status': {
                      'code': 3,
                      'message': 'Labels fingerprint either invalid or resource labels have changed'
                    },
                    'authenticationInfo': {
                    },
                    'requestMetadata': {
                    },
                    'serviceName': 'compute.googleapis.com',
                    'methodName': 'v1.compute.instances.setLabels',
                    'authorizationInfo': [
                    ],
                    'resourceName': 'projects/project-1/zones/asia-southeast1-b/instances/instance-1',
                    'request': {
                      'labels': [
                        {
                          'key': 'label-1',
                          'value': 'value-1'
                        },
                        {
                          'key': 'label-2',
                          'value': 'value-2'
                        }
                      ],
                      '@type': 'type.googleapis.com/compute.instances.setLabels'
                    },
                    'response': {
                      'error': {
                        'errors': [
                          {
                            'domain': 'global',
                            'reason': 'conditionNotMet',
                            'message': 'Labels fingerprint either invalid or resource labels have changed',
                            'locationType': 'header',
                            'location': 'If-Match'
                          }
                        ],
                        'code': 412,
                        'message': 'Labels fingerprint either invalid or resource labels have changed'
                      },
                      '@type': 'type.googleapis.com/error'
                    },
                    'resourceLocation': {
                      'currentLocations': [
                        'asia-southeast1-b'
                      ]
                    }
                  },
                  'insertId': '3m3n54e26jqk',
                  'resource': {
                    'type': 'gce_instance',
                    'labels': {
                      'instance_id': '1718410000000000000',
                      'zone': 'asia-southeast1-b',
                      'project_id': 'project-1'
                    }
                  },
                  'timestamp': '2023-05-07T22:43:46.748295Z',
                  'severity': 'ERROR',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'receiveTimestamp': '2023-05-07T22:43:47.564490147Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetLabelsEvent.IsSetLabelsEvent(r));

            var e = (SetLabelsEvent)r.ToEvent();

            Assert.AreEqual("ERROR", e.Severity);
            Assert.AreEqual(1718410000000000000, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("asia-southeast1-b", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual(
                new InstanceLocator("project-1", "asia-southeast1-b", "instance-1"),
                e.InstanceReference);

            Assert.IsNotNull(e.Labels);
            Assert.AreEqual(2, e.Labels!.Count());
        }
    }
}
