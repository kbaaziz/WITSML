﻿//----------------------------------------------------------------------- 
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

using System.ComponentModel.Composition;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Trajectory" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Trajectory}" />
    [Export(typeof(IWitsmlDataAdapter<Trajectory>))]
    [Export(typeof(IWitsml131Configuration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Trajectory131DataAdapter : TrajectoryDataAdapter<Trajectory, TrajectoryStation>, IWitsml131Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Trajectory131DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Trajectory131DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Trajectory131)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Trajectory"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            Logger.DebugFormat("Getting the supported capabilities for Trajectory data version {0}.", capServer.Version);

            capServer.Add(Functions.GetFromStore, ObjectTypes.Trajectory);
            capServer.Add(Functions.AddToStore, ObjectTypes.Trajectory);
            capServer.Add(Functions.UpdateInStore, ObjectTypes.Trajectory);
            capServer.Add(Functions.DeleteFromStore, ObjectTypes.Trajectory);
        }

        /// <summary>
        /// Clears the trajectory stations.
        /// </summary>
        /// <param name="entity">The entity.</param>
        protected override void ClearTrajectoryStations(Trajectory entity)
        {
            entity.TrajectoryStation = null;
        }
    }
}
