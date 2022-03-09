using System.Collections.Generic;
using NUnit.Framework;
using WebIngest.Common.Models;
using WebIngest.Common.Models.OriginConfiguration;

namespace WebIngest.Tests.Core.Unit.Extraction
{
    [TestFixture]
    public class SelectorTests
    {
        [Test]
        public void TransformSelectorVars()
        {
            var config = new OriginTypeConfiguration()
            {
                PassThroughVars = new Dictionary<string, object>()
                {
                    {"name", "Marcus Aurelius"},
                    {"title", "Caesar"}
                }
            };
            var propertyMapping = new PropertyMapping()
            {
                Selector = "Hi my name is {{name}} and my title is {{title}}"
            };
            var expectation = "Hi my name is Marcus Aurelius and my title is Caesar";
            Assert.AreEqual(expectation, propertyMapping.TransformSelectorVars(config));
        }


        [Test]
        public void SelectorLiteral()
        {
            var propertyMapping = new PropertyMapping()
            {
                Selector = "@The soul becomes dyed with the color of its thoughts."
            };
            Assert.IsTrue(propertyMapping.SelectorIsLiteral);
        }
    }
}