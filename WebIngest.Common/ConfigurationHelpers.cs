using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using WebIngest.Common.Extensions;

namespace WebIngest.Common
{
    public class EnvOptionalAttribute : Attribute
    {
    }

    public static class ConfigurationHelpers
    {
        // execute the get-var on each of the required helpers below to force early exception if not present
        public static void SelfTest(this IConfiguration config)
        {
            var functions =
                typeof(ConfigurationHelpers)
                    .GetMethods(BindingFlags.Public & BindingFlags.Static)
                    .Where(x =>
                        !x.GetCustomAttributes(
                                typeof(EnvOptionalAttribute),
                                false
                            )
                            .Any()
                    );

            foreach (var func in functions)
            {
                if(func.Name != MethodBase.GetCurrentMethod()?.Name)
                    func.CreateDelegate<Action<IConfiguration>>().Invoke(config);
            }
        }
        
        private static string GetVariable(this IConfiguration configuration, string key, bool allowNulls = false)
        {
            EnvironmentExtensions.LoadEnv();

            var variable =
                configuration[key]
                ?? configuration["WI_" + key]
                ?? Environment.GetEnvironmentVariable(key)
                ?? Environment.GetEnvironmentVariable("WI_" + key);

            if (!allowNulls && string.IsNullOrEmpty(variable))
                throw new ArgumentException($"Please set {key} env var");

            return variable;
        }


        public static string GetSuperAdminUsername(this IConfiguration configuration) =>
            configuration.GetVariable("ADMIN_USERNAME");
        public static string GetSuperAdminPassword(this IConfiguration configuration) =>
            configuration.GetVariable("ADMIN_PASSWORD");

        public static string GetJwtSecurityKey(this IConfiguration configuration) =>
            configuration.GetVariable("JWTSECURITYKEY");
        public static string GetJwtExpiryInDays(this IConfiguration configuration) =>
            configuration.GetVariable("JWTEXPIRYINDAYS");
        public static string GetJwtIssuer(this IConfiguration configuration) =>
            configuration.GetVariable("JWTISSUER");
        public static string GetJwtAudience(this IConfiguration configuration) =>
            configuration.GetVariable("JWTAUDIENCE");
        
        public static string GetRedisConnString(this IConfiguration configuration)
        {
            string redisHostname = configuration.GetVariable("REDIS_HOST");
            string redisHostPort = configuration.GetVariable("REDIS_PORT");
            return
                $"{redisHostname}:{redisHostPort},allowAdmin=true,connectRetry=5,connectTimeout=10000,keepAlive=10,name=WebIngest";
        }
        
        public const string ConfigSchemaName = "webingestconfig";
        
        public static string DataSchemaName(this IConfiguration configuration) =>
            configuration.GetVariable("DATA_SCHEMA_NAME", true) ?? "webingestdata";

        private static string GetPgHostConnString(this IConfiguration configuration)
        {
            string pgUsername = configuration.GetVariable("PG_SU_UNAME");
            string pgPassword = configuration.GetVariable("PG_SU_PASS");
            string pgHostname = configuration.GetVariable("PG_HOST");
            string pgHostPort = configuration.GetVariable("PG_PORT");
            return
                $"User ID={pgUsername};Password={pgPassword};Host={pgHostname};Port={pgHostPort};";
        }

        public static string GetPgMaintenanceDbConnString(this IConfiguration configuration)
        {
            string pgDatabase = configuration.GetVariable("PG_MAINTENANCE_DB", true);
            if (string.IsNullOrEmpty(pgDatabase))
                pgDatabase = "postgres";

            return GetPgHostConnString(configuration) + $"Database={pgDatabase};";
        }

        public static string GetPgDbConnString(this IConfiguration configuration, string applicationName = null)
        {
            string pgDatabase = configuration.GetVariable("PG_DB");
            return GetPgHostConnString(configuration) + $"Database={pgDatabase};ApplicationName=WebIngest-{applicationName}";
        }

        /****** OPTIONAL VARIABLES ******/

        [EnvOptional]
        public static bool GetPgStoreData(this IConfiguration configuration) =>
            configuration.GetVariable("PG_STORE_DATA", true) == "true";

        [EnvOptional]
        public static bool GetElasticStoreData(this IConfiguration configuration) =>
            configuration.GetVariable("ELASTIC_STORE_DATA", true) == "true";

        [EnvOptional]
        public static string GetElasticUser(this IConfiguration configuration) =>
            configuration.GetVariable("ELASTIC_USER");

        [EnvOptional]
        public static string GetElasticPassword(this IConfiguration configuration) =>
            configuration.GetVariable("ELASTIC_PASS");

        [EnvOptional]
        public static string[] GetElasticHosts(this IConfiguration configuration) =>
            configuration.GetVariable("ELASTIC_HOSTS").Split(",");
    }
}