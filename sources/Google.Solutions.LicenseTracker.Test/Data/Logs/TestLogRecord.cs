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

using Google.Solutions.LicenseTracker.Data.Logs;
using NUnit.Framework;
using System;

namespace Google.Solutions.LicenseTracker.Data.Test.Logs
{
    [TestFixture]
    public class TestLogRecord 
    {
        [Test]
        public void WhenSystemEventJsonValid_ThenFieldsAreDeserialized()
        {
            var json = @"
                 {
                   'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'authenticationInfo': {
                       'principalEmail': 'system@google.com'
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'NotifyInstanceLocation',
                     'request': {
                       '@type': 'type.googleapis.com/NotifyInstanceLocation'
                     },
                     'metadata': {
                       'serverId': 'b67639853d26e39b79a4fb306fd7d297',
                       'timestamp': '2020-03-23T10:35:09.025059Z',
                       '@type': 'type.googleapis.com/google.cloud.audit.GceInstanceLocationMetadata'
                     }
                   },
                   'insertId': 'kj1zbe4j2eg',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'project_id': 'project-1',
                       'instance_id': '22470777052',
                       'zone': 'asia-east1-c'
                     }
                   },
                   'timestamp': '2020-03-23T10:35:10Z',
                   'severity': 'INFO',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fsystem_event',
                   'receiveTimestamp': '2020-03-23T10:35:11.269405964Z'
                 }";

            var record = LogRecord.Deserialize(json)!;

            Assert.AreEqual("kj1zbe4j2eg", record.InsertId);
            Assert.AreEqual("projects/project-1/logs/cloudaudit.googleapis.com%2Fsystem_event", record.LogName);
            Assert.AreEqual("INFO", record.Severity);
            Assert.AreEqual(new DateTime(2020, 3, 23, 10, 35, 10), record.Timestamp);

            Assert.IsNotNull(record.Resource);
            Assert.AreEqual("gce_instance", record.Resource?.Type);
            Assert.AreEqual("project-1", record.Resource?.Labels?["project_id"]);
            Assert.AreEqual("22470777052", record.Resource?.Labels?["instance_id"]);
            Assert.AreEqual("asia-east1-c", record.Resource?.Labels?["zone"]);

            Assert.AreEqual("project-1", record.ProjectId);
            Assert.IsTrue(record.IsSystemEvent);
            Assert.IsFalse(record.IsActivityEvent);
        }
    }
}
