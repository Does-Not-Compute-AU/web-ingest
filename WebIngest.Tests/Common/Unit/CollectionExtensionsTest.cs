using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WebIngest.Common.Extensions;

namespace WebIngest.Tests.Common.Unit
{
    public class CollectionExtensionsTest
    {
        [Test]
        public void ChunkTest()
        {
            var collection = new [] {1, 2, 3, 4, 5, 6};
            var chunked = collection.Chunk(2).ToList();
            Assert.AreEqual(chunked, new List<IEnumerable<int>>()
            {
                new []{ 1, 2 },
                new []{ 3, 4 },
                new []{ 5, 6 }
            });
        }
        
        [Test]
        public void BatchTest()
        {
            var collection = new [] {1, 2, 3, 4, 5, 6};
            var chunked = collection.Batch(3).ToList();
            Assert.AreEqual(chunked, new List<IEnumerable<int>>()
            {
                new []{ 1, 2 },
                new []{ 3, 4 },
                new []{ 5, 6 }
            });
        }
    }
}