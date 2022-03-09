using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Nest;
using WebIngest.Common;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models;

namespace WebIngest.Core.Data.EntityStorage
{
    public class ElasticStorageService : IEntityStorage
    {
        private readonly string _dataSchemaName;
        private readonly IConfiguration _configuration;
        private readonly ElasticClient _client;

        public ElasticStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
            _dataSchemaName = configuration.DataSchemaName();

            var nodes = configuration.GetElasticHosts().Select(x => new Uri(x));
            var pool = new StaticConnectionPool(nodes);
            var settings = new ConnectionSettings(pool);

            var elasticUser = configuration.GetElasticUser();
            var elasticPass = configuration.GetElasticPassword();

            settings.EnableHttpCompression();
            settings.BasicAuthentication(elasticUser, elasticPass);
            _client = new ElasticClient(settings);
        }

        public long CountStorageEntries(DataType dataType, DataOrigin dataOrigin = null)
        {
            var req = new CountRequest(GetIndexName(dataType));

            if (dataOrigin != null)
                req.Query = new MatchQuery()
                {
                    Field = "DataOrigin",
                    Query = dataOrigin.Name
                };

            return _client
                .Count(req)
                .Count;
        }

        public void CreateStorageLocation(DataType dataType)
        {
            // do nothing, not necessary in elasticsearch because the index is created when entity is pushed
        }

        public void DeleteStorageLocation(DataType dataType)
        {
            _client.Indices.Delete(GetIndexName(dataType));
        }

        public void BulkInsertEntities(
            DataOrigin dataOrigin,
            Mapping mapping,
            IEnumerable<IDictionary<string, object>> entities
        )
        {
            var timestamp = DateTime.Now;
            foreach (var entity in entities)
            {
                entity["Timestamp"] = timestamp;
                entity["DataOrigin"] = dataOrigin.Name;
            }

            var indexName = GetIndexName(mapping.DataType);
            var result = _client.IndexMany(entities, indexName);
            if (result.Errors)
            {
                var errors = result.ItemsWithErrors.Take(10).StringJoin(Environment.NewLine);
                throw new ApplicationException(
                    $"{result.ItemsWithErrors.Count()} Errors Saving to Elastic, Showing Top 10: " + errors);
            }
        }

        private IndexName GetIndexName(DataType dataType)
        {
            return _dataSchemaName + "-" + dataType.Name.ToLower() + "-" + DateTime.Now.ToString("yyyy-MM-dd");
        }
    }
}