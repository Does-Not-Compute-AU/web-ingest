using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using WebIngest.Common.Models.OriginConfiguration;
using WebIngest.Core.Scripting;

namespace WebIngest.Core.Jobs.FetchJobs
{
    public static class ScriptedJobs
    {
        public static IEnumerable<Expression<Action>> GetJobsForScripted(
            string dataSourceName,
            OriginTypeConfiguration originConfig
        )
        {
            var resultConfig = new ScriptCompiler(originConfig.ScriptedConfiguration.CSharpScript)
                .GenerateSourceFromScript()
                .Compile()
                .Execute()
                .GetResult<object>();

            var returnType = resultConfig.GetType();


            // allow return of multiple configs, return all jobs for corresponding configs
            if (
                returnType == typeof(OriginTypeConfiguration[])
                || returnType == typeof(List<OriginTypeConfiguration>)
            )
                return 
                    (resultConfig as IEnumerable<OriginTypeConfiguration>)!
                    .SelectMany(x =>
                        DataOriginJobs.GetJobsForConfig(x, dataSourceName)
                    );

            // allow return of single config
            if (returnType == typeof(OriginTypeConfiguration))
                return DataOriginJobs.GetJobsForConfig((OriginTypeConfiguration) resultConfig, dataSourceName);

            // if not either of above two, not sure what else was intended...
            throw new NotImplementedException($"Unexpected script return type: {returnType}");
        }
    }
}