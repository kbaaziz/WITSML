//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

// ----------------------------------------------------------------------
// <auto-generated>
//     Changes to this file may cause incorrect behavior and will be lost
//     if the code is regenerated.
// </auto-generated>
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wellbores
{
    public abstract partial class Wellbore131TestBase : IntegrationTestBase
    {
        public const string QueryMissingNamespace = "<wellbores version=\"1.3.1.1\"><wellbore /></wellbores>";
        public const string QueryInvalidNamespace = "<wellbores xmlns=\"www.witsml.org/schemas/123\" version=\"1.3.1.1\"></wellbores>";
        public const string QueryMissingVersion = "<wellbores xmlns=\"http://www.witsml.org/schemas/131\"></wellbores>";
        public const string QueryEmptyRoot = "<wellbores xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\"></wellbores>";
        public const string QueryEmptyObject = "<wellbores xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\"><wellbore /></wellbores>";
        public const string BasicXMLTemplate = "<wellbores xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\"><wellbore uidWell=\"{0}\" uid=\"{1}\">{2}</wellbore></wellbores>";

        public Well Well { get; set; }
        public Wellbore Wellbore { get; set; }
        public DevKit131Aspect DevKit { get; set; }
        public TestContext TestContext { get; set; }
        public List<Wellbore> QueryEmptyList { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit131Aspect(TestContext);

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version131.Value)
                .ToArray();

            Well = new Well
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Well"),
                TimeZone = DevKit.TimeZone
            };
            Wellbore = new Wellbore
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Wellbore"),
                UidWell = Well.Uid,
                NameWell = Well.Name,
                MDCurrent = new MeasuredDepthCoord(0, MeasuredDepthUom.ft)
            };

            QueryEmptyList = DevKit.List(new Wellbore());

            BeforeEachTest();
            OnTestSetUp();
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            AfterEachTest();
            OnTestCleanUp();
            DevKit.Container.Dispose();
            DevKit = null;
        }

        partial void BeforeEachTest();

        partial void AfterEachTest();

        protected virtual void OnTestSetUp() { }

        protected virtual void OnTestCleanUp() { }

        protected virtual void AddParents()
        {
            DevKit.AddAndAssert<WellList, Well>(Well);
        }
    }
}