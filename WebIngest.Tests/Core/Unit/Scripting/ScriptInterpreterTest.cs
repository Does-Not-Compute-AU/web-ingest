using System;
using System.Collections.Generic;
using NUnit.Framework;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models.OriginConfiguration.Types;
using WebIngest.Core.Scripting;

namespace WebIngest.Tests.Core.Unit.Scripting
{
    public class ScriptInterpreterTest
    {
        [Test]
        public void ThrowsCompilationException()
        {
            Assert.Catch<Exception>(() =>
            {
                string simpleScript = "NOTREALCSHARPCODE";
                new ScriptCompiler(simpleScript)
                    .GenerateSourceFromScript()
                    .Compile();
            });
        }

        [Test]
        public void ReturnsNull()
        {
            string simpleScript = "return null;";
            var res = new ScriptCompiler(simpleScript)
                .GenerateSourceFromScript()
                .Compile()
                .Execute()
                .GetResult();

            Assert.AreEqual(null, res);
        }

        [Test]
        public void ReturnsStrings()
        {
            string simpleScript = "Console.WriteLine(\"Pardon?\"); return \"Hello World\";";
            var res = new ScriptCompiler(simpleScript)
                .GenerateSourceFromScript()
                .Compile()
                .Execute()
                .GetResult<string>();

            const string expectation = "Hello World";
            Assert.AreEqual(expectation, res);
        }

        [Test]
        public void ReturnsIntegers()
        {
            string simpleScript = "Console.WriteLine(\"Pardon?\"); return 10;";
            var res = new ScriptCompiler(simpleScript)
                .GenerateSourceFromScript()
                .Compile()
                .Execute()
                .GetResult<int>();

            const int expectation = 10;
            Assert.AreEqual(expectation, res);
        }

        [Test]
        public void ReturnsHttpConfigurationWithProxy()
        {
            var script =
                "var wordList = new List<string>(){ \r\n    \"WordA\", \"WordB\", \"WordC\"\r\n};\r\nvar urlList = new List<string>();\r\nforeach (var word in wordList)\r\n{\r\n    urlList.Add($\"https://www.google.com/search?q={word}\");\r\n}\r\n\r\nreturn new HttpConfiguration()\r\n{\r\n    Urls = urlList,\r\n    ProxyAddress = \"http://localhost:8080\"\r\n};";
            var res = new ScriptCompiler(script)
                .GenerateSourceFromScript()
                .Compile()
                .Execute()
                .GetResult<HttpConfiguration>();

            var expectation = new HttpConfiguration
            {
                Urls = new List<string>
                {
                    "https://www.google.com/search?q=WordA",
                    "https://www.google.com/search?q=WordB",
                    "https://www.google.com/search?q=WordC"
                },
                ProxyAddress = "http://localhost:8080"
            };

            Assert.AreEqual(expectation.ToJson(), res.ToJson());
        }
    }
}