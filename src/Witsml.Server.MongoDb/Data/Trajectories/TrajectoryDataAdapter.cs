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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Energistics.DataAccess;
using Energistics.Datatypes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// MongoDb data adapter that encapsulates CRUD functionality for Trajectory objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TChild">The type of the child.</typeparam>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{T}" />
    public abstract class TrajectoryDataAdapter<T, TChild> : MongoDbDataAdapter<T> where T : IWellboreObject where TChild : IUniqueId
    {
        /// <summary>
        /// The field to query Mongo File
        /// </summary>
        private const string FileQueryField = "Uri";

        /// <summary>
        /// The file name
        /// </summary>
        private const string FileName = "FileName";

        /// <summary>
        /// Initializes a new instance of the <see cref="TrajectoryDataAdapter{T, TChild}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        protected TrajectoryDataAdapter(IContainer container, IDatabaseProvider databaseProvider, string dbCollectionName) : base(container, databaseProvider, dbCollectionName)
        {
            Logger.Debug("Instance created.");
        }

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <param name="context">The response context.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public override List<T> Query(WitsmlQueryParser parser, ResponseContext context)
        {
            var entities = QueryEntities(parser);

            if (parser.IncludeTrajectoryStations())
            {
                ValidateGrowingObjectDataRequest(parser, entities);

                var headers = GetEntities(entities.Select(x => x.GetUri()))
                    .ToDictionary(x => x.GetUri());

                entities.ForEach(x =>
                {
                    // TODO: Implement trajectory station range query, if requested
                    var header = headers[x.GetUri()];

                    //Query the trajectory stations
                    QueryTrajectoryStations(x, header, parser, context);
                });
            }
            else if (!OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
            {
                entities.ForEach(ClearTrajectoryStations);
            }

            return entities;
        }

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be added.</param>
        public override void Add(WitsmlQueryParser parser, T dataObject)
        {
            using (var transaction = DatabaseProvider.BeginTransaction())
            {
                SetIndexRange(dataObject);
                UpdateMongoFile(dataObject, false);
                InsertEntity(dataObject, transaction);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<T> GetAll(EtpUri? parentUri = null)
        {
            Logger.DebugFormat("Fetching all Trajectorys; Parent URI: {0}", parentUri);

            return GetAllQuery(parentUri)
                .OrderBy(x => x.Name)
                .ToList();
        }

        /// <summary>
        /// Gets an <see cref="IQueryable{T}" /> instance to by used by the GetAll method.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>An executable query.</returns>
        protected override IQueryable<T> GetAllQuery(EtpUri? parentUri)
        {
            var query = GetQuery().AsQueryable();

            if (parentUri != null)
            {
                var ids = parentUri.Value.GetObjectIds().ToDictionary(x => x.ObjectType, y => y.ObjectId);
                var uidWellbore = ids[ObjectTypes.Wellbore];
                var uidWell = ids[ObjectTypes.Well];

                query = query.Where(x => x.UidWell == uidWell && x.UidWellbore == uidWellbore);
            }

            return query;
        }

        /// <summary>
        /// Gets a list of the property names to project during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of property names.</returns>
        protected override List<string> GetProjectionPropertyNames(WitsmlQueryParser parser)
        {
            var returnElements = parser.ReturnElements();

            return OptionsIn.ReturnElements.IdOnly.Equals(returnElements)
                ? new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell", "UidWellbore", "NameWellbore" }
                : OptionsIn.ReturnElements.StationLocationOnly.Equals(returnElements)
                ? new List<string>
                {
                    IdPropertyName, "UidWell", "UidWellbore", "TrajectoryStation.DateTimeStn", "TrajectoryStation.TypeTrajStation",
                    "TrajectoryStation.MD", "TrajectoryStation.Tvd", "TrajectoryStation.Incl", "TrajectoryStation.Azi",
                    "TrajectoryStation.Location"
                }
                : parser.IncludeTrajectoryStations()
                ? new List<string> { IdPropertyName, "UidWell", "UidWellbore", "TrajectoryStation" }
                : OptionsIn.ReturnElements.Requested.Equals(returnElements)
                ? new List<string>()
                : null;
        }

        /// <summary>
        /// Gets a list of the element names to ignore during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected override List<string> GetIgnoredElementNamesForQuery(WitsmlQueryParser parser)
        {
            return new List<string> { "mdMn", "mdMx" };
        }

        /// <summary>
        /// Gets a list of the element names to ignore during an update.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected override List<string> GetIgnoredElementNamesForUpdate(WitsmlQueryParser parser)
        {
            return GetIgnoredElementNamesForQuery(parser)
                .Concat(new[]
                {
                    "objectGrowing"
                })
                .ToList();
        }

        /// <summary>
        /// Saves trajectory stations data in mongo file if trajectory stations count exceeds maximun count; removes if not.
        /// </summary>
        /// <param name="entity">The data object.</param>
        /// <param name="deleteFile">if set to <c>true</c> [delete file].</param>
        protected void UpdateMongoFile(T entity, bool deleteFile = true)
        {
            var uri = entity.GetUri();
            Logger.DebugFormat($"Updating MongoDb Trajectory Stations files: {uri}");

            var bucket = GetMongoFileBucket();
            var stations = GetTrajectoryStation(entity);

            if (stations.Count >= WitsmlSettings.MaxStationCount)
            {
                var bytes = Encoding.UTF8.GetBytes(stations.ToJson());

                var loadOptions = new GridFSUploadOptions
                {
                    Metadata = new BsonDocument
                    {
                        { FileName, new Guid().ToString() },
                        { FileQueryField, uri.ToString() },
                        { "DataBytes", bytes.Length }
                    }
                };

                if (deleteFile)
                    DeleteMongoFile(bucket, uri);

                bucket.UploadFromBytes(uri, bytes, loadOptions);
                ClearTrajectoryStations(entity);
            }
            else
            {
                if (deleteFile)
                    DeleteMongoFile(bucket, uri);
            }
        }

        private IGridFSBucket GetMongoFileBucket()
        {
            var db = DatabaseProvider.GetDatabase();
            return new GridFSBucket(db, new GridFSBucketOptions
            {
                BucketName = DbCollectionName,
                ChunkSizeBytes = WitsmlSettings.ChunkSizeBytes
            });
        }

        private void DeleteMongoFile(IGridFSBucket bucket, string fileId)
        {
            Logger.DebugFormat($"Deleting MongoDb Channel Data file: {fileId}");

            var filter = Builders<GridFSFileInfo>.Filter.Eq(fi => fi.Metadata[FileQueryField], fileId);
            var mongoFile = bucket.Find(filter).FirstOrDefault();

            if (mongoFile == null)
                return;

            bucket.Delete(mongoFile.Id);
        }

        private void QueryTrajectoryStations(T entity, T header, WitsmlQueryParser parser, ResponseContext context)
        {
            if (!QueryStationFile(entity, header))
                return;

            var uri = entity.GetUri();
            var stations = GetMongoFileStationData(uri);
            FormatStationData(entity, stations, parser);
        }

        private List<TChild> GetMongoFileStationData(string uri)
        {
            Logger.Debug("Getting MongoDb Trajectory Station files.");

            var bucket = GetMongoFileBucket();

            var filter = Builders<GridFSFileInfo>.Filter.Eq(fi => fi.Metadata[FileQueryField], uri);
            var mongoFile = bucket.Find(filter).FirstOrDefault();

            if (mongoFile == null)
                return null;

            var bytes = bucket.DownloadAsBytes(mongoFile.Id);
            var json = Encoding.UTF8.GetString(bytes);
            return BsonSerializer.Deserialize<List<TChild>>(json);
        }

        /// <summary>
        /// Clears the trajectory stations.
        /// </summary>
        /// <param name="entity">The entity.</param>
        protected abstract void ClearTrajectoryStations(T entity);

        /// <summary>
        /// Formats the station data based on query parameters.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="stations">The trajectory stations.</param>
        /// <param name="parser">The parser.</param>
        protected abstract void FormatStationData(T entity, List<TChild> stations, WitsmlQueryParser parser);

        /// <summary>
        /// Determines whether the current trajectory has station data.
        /// </summary>
        /// <param name="header">The trajectory.</param>
        /// <returns>
        ///   <c>true</c> if the specified trajectory has data; otherwise, <c>false</c>.
        /// </returns>
        protected abstract bool HasData(T header);

        /// <summary>
        /// Check if need to query mongo file for station data.
        /// </summary>
        /// <param name="entity">The result data object.</param>
        /// <param name="header">The full header object.</param>
        /// <returns><c>true</c> if needs to query mongo file; otherwise, <c>false</c>.</returns>
        protected abstract bool QueryStationFile(T entity, T header);

        /// <summary>
        /// Sets the MD index ranges.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        protected abstract void SetIndexRange(T dataObject);

        /// <summary>
        /// Gets the trajectory station.
        /// </summary>
        /// <param name="dataObject">The trajectory data object.</param>
        /// <returns>The trajectory station collection.</returns>
        protected abstract List<TChild> GetTrajectoryStation(T dataObject);
    }
}
