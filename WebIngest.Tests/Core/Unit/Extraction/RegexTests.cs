using NUnit.Framework;
using WebIngest.Common.Models;

namespace WebIngest.Tests.Core.Unit.Extraction
{
    [TestFixture]
    public class RegexTests
    {
        [Test]
        public void RegexReplaceSurrounding()
        {
            string testString = "Very little is needed to make a happy life; it is all within yourself, in your way of thinking.";
            var transform = new RegexTransform()
            {
                FindPattern = @".*\;\s([\w\s]*)\,.*",
                ReplacePattern = "$1"
            };
            string expectation = "it is all within yourself";
            Assert.AreEqual(expectation, transform.DoRegexReplace(testString));
        }

        [Test]
        public void RegexReplaceInternal()
        {
            string testString = "If it is not right do not do it; if it is not true do not say it.";
            var transform = new RegexTransform()
            {
                FindPattern = "it",
                ReplacePattern = "that"
            };
            string expectation = "If that is not right do not do that; if that is not true do not say that.";
            Assert.AreEqual(expectation, transform.DoRegexReplace(testString));
        }
        
        [Test]
        public void RegexRemove()
        {
            string removeString = "soul ";
            string testString = "The soul becomes dyed with the color of its thoughts.";
            var transform = new RegexTransform()
            {
                FindPattern = removeString,
                ReplacePattern = ""
            };
            string expectation = "The becomes dyed with the color of its thoughts.";
            // oh no, you've lost your soul :(
            Assert.AreEqual(expectation, transform.DoRegexReplace(testString));
        }
    }
}