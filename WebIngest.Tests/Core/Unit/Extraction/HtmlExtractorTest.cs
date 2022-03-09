using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NUnit.Framework;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models;
using WebIngest.Common.Models.OriginConfiguration;
using WebIngest.Core.Extraction;

namespace WebIngest.Tests.Core.Unit.Extraction
{
    [TestFixture]
    public class HtmlExtractorTest
    {
        [Test]
        public void ValuesFromHtmlTestSimple()
        {
            var input = File.ReadAllText("./Core/Unit/Files/TestHtmlFile.html");

            var expectedOutput = new Dictionary<string, object>()
            {
                {"Title", "TestTitle"},
                {"Id", "TestById"},
                {"Class", "TestByClass"},
                {"StaticNumber", 1.569m},
            };

            var parsedSingleObject =
                HtmlExtractor
                    .ValuesFromHtml(
                        MockMappingSimple,
                        null,
                        new OriginTypeConfiguration()
                        {
                            PassThroughVars = new Dictionary<string, object>()
                            {
                                {"StaticNumber", "1.569"}
                            }
                        },
                        input
                    )
                    .First();
            Assert.AreEqual(parsedSingleObject, expectedOutput);
        }


        [Test]
        public void ValuesFromHtmlTestMulti()
        {
            var input = File.ReadAllText("./Core/Unit/Files/TestHtmlFile.html");

            var parsedMultiObjects = HtmlExtractor
                .ValuesFromHtml(
                    MockMappingMulti,
                    null,
                    MockOriginConfigMulti,
                    input
                );

            Assert.AreEqual(parsedMultiObjects, new List<Dictionary<string, object>>()
            {
                new()
                {
                    {"Category", "Technology"},
                    {"SKU", "coal-tooth-brush"},
                    {"ProductName", "Super Coal Soft Toothy Boi"},
                    {"Price", 11.99m}
                },
                new()
                {
                    {"Category", "Technology"},
                    {"SKU", "RZ02-02500300"},
                    {"ProductName", "Razer Goliathus Extended Chroma Soft Gaming Mouse Mat"},
                    {"Price", 119m}
                },
                new()
                {
                    {"Category", "Technology"},
                    {"SKU", "WH1000XM4B"},
                    {"ProductName", "Sony WH-1000XM4 Wireless Noise Cancelling Over-Ear Headphones (Black)"},
                    {"Price", 346m}
                },
            });
        }

        private static readonly Mapping MockMappingSimple =
            new()
            {
                DataType = new()
                {
                    Name = "Html Page",
                    Properties = new List<DataTypeProperty>()
                    {
                        new()
                        {
                            PropertyName = "Title",
                            PropertyType = PropertyType.TEXT
                        },
                        new()
                        {
                            PropertyName = "Id",
                            PropertyType = PropertyType.TEXT
                        },
                        new()
                        {
                            PropertyName = "Class",
                            PropertyType = PropertyType.TEXT
                        },
                        new()
                        {
                            PropertyName = "StaticNumber",
                            PropertyType = PropertyType.MONEY
                        }
                    }
                },
                PropertyMappings = new List<PropertyMapping>()
                {
                    new()
                    {
                        DataTypeProperty = "Title",
                        Selector = "title"
                    },
                    new()
                    {
                        DataTypeProperty = "Id",
                        Selector = "#testById"
                    },

                    new()
                    {
                        DataTypeProperty = "Class",
                        Selector = "div.testClass"
                    },
                    new()
                    {
                        DataTypeProperty = "StaticNumber",
                        Selector = "@{{StaticNumber}}"
                    }
                }
            };

        private static readonly OriginTypeConfiguration MockOriginConfigMulti =
            new()
            {
                PassThroughVars = new Dictionary<string, object>()
                {
                    {"Category", "Technology"}
                }
            };

        private static readonly Mapping MockMappingMulti =
            new()
            {
                DataType = new()
                {
                    Name = "Product",
                    Properties = new List<DataTypeProperty>()
                    {
                        new()
                        {
                            PropertyName = "Category",
                            PropertyType = PropertyType.TEXT
                        },
                        new()
                        {
                            PropertyName = "SKU",
                            PropertyType = PropertyType.TEXT
                        },
                        new()
                        {
                            PropertyName = "ProductName",
                            PropertyType = PropertyType.TEXT
                        },
                        new()
                        {
                            PropertyName = "Price",
                            PropertyType = PropertyType.MONEY
                        }
                    }
                },
                PropertyMappings = new List<PropertyMapping>()
                {
                    new()
                    {
                        DataTypeProperty = "Category",
                        Selector = "@{{Category}}"
                    },
                    new()
                    {
                        DataTypeProperty = "SKU",
                        Selector = "#productList>div.product>div.part-num"
                    },
                    new()
                    {
                        DataTypeProperty = "ProductName",
                        Selector = "#productList>div.product>div.name"
                    },
                    new()
                    {
                        DataTypeProperty = "Price",
                        Selector = "#productList>div.product>div.price"
                    }
                }
            };
    }
}