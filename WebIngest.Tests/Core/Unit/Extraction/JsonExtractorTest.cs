using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models;
using WebIngest.Common.Models.OriginConfiguration;
using WebIngest.Core.Extraction;

namespace WebIngest.Tests.Core.Unit.Extraction
{
    [TestFixture]
    public class JsonExtractorTest
    {
        private static readonly object[] MockInput =
        {
            new
            {
                currency = "AUD",
                instrument = "GOOG",
                bestAsk = 10.00,
                bestBid = 9.00
            },
            new
            {
                currency = "AUD",
                instrument = "AAPL",
                bestAsk = 12.00,
                bestBid = 11.00
            }
        };

        private static readonly List<Dictionary<string, object>> MockOutput = new()
        {
            new()
            {
                {"KnowledgeSource", "The Cosmos"},
                {"BaseCurrency", "AUD"},
                {"Symbol", "GOOG"},
                {"BuyRate", 10m},
                {"SellRate", 9m}
            },
            new()
            {
                {"KnowledgeSource", "The Cosmos"},
                {"BaseCurrency", "AUD"},
                {"Symbol", "AAPL"},
                {"BuyRate", 12m},
                {"SellRate", 11m}
            }
        };

        [Test]
        public void ValuesFromJsonSingle()
        {
            // Test parse of Single object
            var parsedResult = JsonExtractor
                .ValuesFromJson(
                    MockMapping,
                    null,
                    MockOriginConfig,
                    MockInput
                        .First()
                        .ToJson()
                ).First();
            Assert.AreEqual(parsedResult, MockOutput.First());
        }

        [Test]
        public void ValuesFromJsonMulti()
        {
            //Test parse of object array
            var parsedResult = JsonExtractor
                .ValuesFromJson(
                    MockMapping,
                    null,
                    MockOriginConfig,
                    MockInput
                        .ToJson()
                );
            Assert.AreEqual(parsedResult, MockOutput);
        }

        private static readonly OriginTypeConfiguration MockOriginConfig = new()
        {
            PassThroughVars = new Dictionary<string, object>()
            {
                {"KnowledgeSource", "The Cosmos"}
            }
        };

        private static readonly Mapping MockMapping =
            new()
            {
                DataType = new()
                {
                    Name = "CryptoPair",
                    Properties = new List<DataTypeProperty>()
                    {
                        new()
                        {
                            PropertyName = "KnowledgeSource",
                            PropertyType = PropertyType.TEXT
                        },
                        new()
                        {
                            PropertyName = "BaseCurrency",
                            PropertyType = PropertyType.TEXT
                        },
                        new()
                        {
                            PropertyName = "Symbol",
                            PropertyType = PropertyType.TEXT
                        },
                        new()
                        {
                            PropertyName = "BuyRate",
                            PropertyType = PropertyType.MONEY
                        },
                        new()
                        {
                            PropertyName = "SellRate",
                            PropertyType = PropertyType.MONEY
                        }
                    }
                },
                PropertyMappings = new List<PropertyMapping>()
                {
                    new()
                    {
                        DataTypeProperty = "KnowledgeSource",
                        Selector = "@{{KnowledgeSource}}"
                    },
                    new()
                    {
                        DataTypeProperty = "BaseCurrency",
                        Selector = "currency"
                    },
                    new()
                    {
                        DataTypeProperty = "Symbol",
                        Selector = "instrument"
                    },

                    new()
                    {
                        DataTypeProperty = "BuyRate",
                        Selector = "bestAsk"
                    },
                    new()
                    {
                        DataTypeProperty = "SellRate",
                        Selector = "bestBid"
                    }
                }
            };
    }
}