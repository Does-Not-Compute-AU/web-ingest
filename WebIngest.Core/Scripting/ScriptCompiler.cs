using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.Loader;
using System.Threading.Tasks;
using TurnerSoftware.SitemapTools;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models.OriginConfiguration;
using WebIngest.Common.Models.OriginConfiguration.Types;
using WebIngest.Core.Scraping;
using WebIngest.Core.Scraping.WebClients;


namespace WebIngest.Core.Scripting
{
    public class ScriptCompiler
    {
        private const string EntryMethodName = "GetResult";
        private string _scriptSource;
        private string _fullSource;
        private byte[] _compiledBytes;
        private object _scriptResult;

        private static readonly IEnumerable<Type> ClassWhitelist = new[]
        {
            typeof(object),
            typeof(Object),
            typeof(ExpandoObject),
            typeof(Console),
            typeof(Component),
            typeof(BitArray),
            typeof(Enumerable),
            typeof(List<>),
            typeof(Task<>),
            
            typeof(Uri),
            typeof(WebClient),
            typeof(WebProxy),
            typeof(IWebProxy),
            typeof(HttpClient),
            typeof(HttpClientHandler),
            typeof(IDataReader),
            
            // webingest domain
            typeof(IngestWebClient),
            typeof(ScrapingHelpers),
            typeof(WebIngestClientHelpers),
            typeof(JsonExtensions),
            typeof(Common.Extensions.CollectionExtensions),
            typeof(OriginTypeConfiguration),
            typeof(HttpConfiguration),
            
            // 3rd party libs
            typeof(SitemapQuery),
            typeof(SitemapEntry)
        };

        public ScriptCompiler(string scriptSource)
        {
            _scriptSource = scriptSource;
        }

        private static IEnumerable<string> UsingNamespaces => 
            ClassWhitelist
                .Select(x => x.Namespace)
                .Distinct();
        private static IEnumerable<Assembly> ReferencedAssemblies => 
            ClassWhitelist
                .Select(x => x.Assembly)
                .Distinct();

        public ScriptCompiler GenerateSourceFromScript()
        {
            if (String.IsNullOrEmpty(_scriptSource))
                throw new Exception("You must first initialise the object with the script source code");

            var sb = new StringBuilder();
            foreach (var import in UsingNamespaces)
            {
                sb.Append($"using {import};\n");
            }

            sb.Append("namespace CSharpScriptCompiler{ \n");
            sb.Append("public class CSharpScriptCompiler { \n");
            sb.Append("public static void Main(string[] args){ }\n");
            sb.Append("public static object " + EntryMethodName + "(){\n");
            sb.Append(_scriptSource);
            sb.Append("}\n");
            sb.Append("}\n");
            sb.Append("}\n");

            _fullSource = sb.ToString();
            return this;
        }

        public ScriptCompiler Compile()
        {
            if (String.IsNullOrEmpty(_fullSource))
                throw new Exception($"Please first call {nameof(GenerateSourceFromScript)}");

            using (var peStream = new MemoryStream())
            {
                var result = GenerateCode(_fullSource).Emit(peStream);
                if (!result.Success)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine("Errors in Compilation:");

                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        stringBuilder.AppendLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                    }

                    throw new Exception(stringBuilder.ToString());
                }

                peStream.Seek(0, SeekOrigin.Begin);

                _compiledBytes = peStream.ToArray();
                return this;
            }
        }

        public ScriptCompiler Execute(string[] args = null)
        {
            var assemblyLoadContextWeakRef = LoadAndExecute(_compiledBytes, args);

            for (var i = 0; i < 8 && assemblyLoadContextWeakRef.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return this;
        }

        public object GetResult()
        {
            return _scriptResult;
        }

        public T GetResult<T>()
        {
            return (T)_scriptResult;
        }

        private static CSharpCompilation GenerateCode(string sourceCode)
        {
            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var referenceLocations = new List<string>
            {
                Assembly.Load("netstandard").Location,
                Assembly.Load("System.Runtime").Location,
                typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location,
                typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location
            };

            referenceLocations.AddRange(ReferencedAssemblies.Select(x => x.Location));

            var references = referenceLocations
                .Distinct()
                .Select(x => MetadataReference.CreateFromFile(x));

            return CSharpCompilation.Create("Script.dll",
                new[] {parsedSyntaxTree},
                references: references,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default)
                );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private WeakReference LoadAndExecute(byte[] compiledAssembly, string[] args)
        {
            using (var asm = new MemoryStream(compiledAssembly))
            {
                var assemblyLoadContext = new AssemblyLoadContext(null, true);

                var assembly = assemblyLoadContext.LoadFromStream(asm);

                var entry = assembly.EntryPoint;

                _ = entry != null && entry.GetParameters().Length > 0
                    ? entry.Invoke(null, new object[] {args})
                    : entry.Invoke(null, null);

                _scriptResult = assembly
                    .GetTypes()
                    .First()
                    .GetMethod(EntryMethodName)
                    ?.Invoke(null, null);

                assemblyLoadContext.Unload();

                return new WeakReference(assemblyLoadContext);
            }
        }
    }
}