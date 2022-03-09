using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using WebIngest.Common;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models;
using WebIngest.Core.Data.EntityStorage;

namespace WebIngest.Core.Data
{
    public class NpgsqlStorageService : IEntityStorage
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public NpgsqlStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetPgDbConnString("DataStorage");
            BootstrapStorage();
        }

        private void ExecuteSqlTransaction(string sqlCommand)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            using var cmd = transaction.Connection.CreateCommand();
            cmd.CommandText = sqlCommand;
            cmd.ExecuteNonQuery();
            transaction.Commit();
        }

        private void ExecuteSqlQuery(string sqlCommand)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sqlCommand;
            cmd.ExecuteNonQuery();
        }

        private T ExecuteSqlScalar<T>(string sqlCommand)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sqlCommand;
            var res = cmd.ExecuteScalar();

            return (T) res;
        }

        public void BootstrapStorage(params DataType[] dataTypes)
        {
            CreateDatabase();
            CreateSchema();
            foreach (var type in dataTypes)
                CreateStorageLocation(type);
        }

        private void CreateDatabase()
        {
            using var hostConnection = new NpgsqlConnection(_configuration.GetPgMaintenanceDbConnString());
            hostConnection.Open();

            var dbName = _configuration["PG_DB"];
            using var cmd = hostConnection.CreateCommand();
            cmd.CommandText =
                @$"SELECT 'CREATE DATABASE {dbName}' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '{dbName}')";
            cmd.ExecuteNonQuery();
        }

        public void CreateSchema()
        {
            ExecuteSqlQuery($"CREATE SCHEMA IF NOT EXISTS {_configuration.DataSchemaName()};");
        }

        public long CountStorageEntries(DataType dataType, DataOrigin dataOrigin = null)
        {
            var sql = $"SELECT COUNT(*) FROM {GetTableName(dataType)}";
            if (dataOrigin != null)
                sql += $" WHERE \"DataOrigin\"='{dataOrigin.Name}'";

            return ExecuteSqlScalar<long>(sql);
        }

        public void CreateStorageLocation(DataType dataType)
        {
            DeleteStorageLocation(dataType);

            StringBuilder sb = new StringBuilder();
            sb.Append(
                $"CREATE TABLE IF NOT EXISTS {GetTableName(dataType)} (\"Id\" SERIAL NOT NULL PRIMARY KEY, \"CreatedAt\" TIMESTAMPTZ NOT NULL DEFAULT NOW(), \"DataOrigin\" VARCHAR(40),");
            foreach (var prop in dataType.Properties)
            {
                sb.Append($"\"{prop.PropertyName}\" {GetSqlType(prop.PropertyType)},");
            }

            //Remove last comma
            sb.Remove(sb.Length - 1, 1);
            sb.Append(");");
            var createTableSql = sb.ToString();
            ExecuteSqlQuery(createTableSql);
        }

        public void DeleteStorageLocation(DataType dataType)
        {
            var deleteTableSql = $"DROP TABLE IF EXISTS {GetTableName(dataType)} CASCADE;";
            ExecuteSqlQuery(deleteTableSql);
        }

        public void BulkInsertEntities(
            DataOrigin dataOrigin,
            Mapping mapping,
            IEnumerable<IDictionary<string, object>> entities)
        {
            var propertyNames = mapping
                .DataType
                .Properties
                .Select(x => x.PropertyName)
                .ToList();

            var columns = "\"DataOrigin\",\"" + propertyNames.StringJoin("\",\"") + "\"";
            var copySql =
                $"COPY \"{_configuration.DataSchemaName()}\".\"{mapping.DataTypeName}\" ({columns}) FROM STDIN (FORMAT BINARY)";


            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            try
            {
                using var writer = connection.BeginBinaryImport(copySql);
                var parsedObjects = entities as Dictionary<string, object>[] ?? entities.ToArray();
                foreach (var parsedObject in parsedObjects)
                {
                    writer.StartRow();
                    writer.Write(dataOrigin.Name); // write data origin column first
                    foreach (var prop in mapping.DataType.Properties)
                    {
                        object propValue;
                        parsedObject.TryGetValue(prop.PropertyName, out propValue);

                        if (propValue != null)
                            writer.Write(propValue, GetNpgSqlType(prop.PropertyType));
                        else
                            writer.WriteNull();
                    }
                }

                writer.Complete();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }
        }

        private string GetTableName(DataType dataType) => $"{_configuration.DataSchemaName()}.\"{dataType.Name}\"";
        private static string GetSqlType(PropertyType type) => GetNpgSqlType(type).ToString();

        private static NpgsqlDbType GetNpgSqlType(PropertyType type) => type switch
        {
            PropertyType.MONEY => NpgsqlDbType.Money,
            PropertyType.NUMBER => NpgsqlDbType.Numeric,
            PropertyType.LONGTEXT => NpgsqlDbType.Text,
            _ => NpgsqlDbType.Varchar
        };
    }
}