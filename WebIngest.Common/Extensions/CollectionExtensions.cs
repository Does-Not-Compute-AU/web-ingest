using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace WebIngest.Common.Extensions
{
	public static class CollectionExtensions
	{
		private static Random rng = new Random();

		public static void AddRange<T>(this ConcurrentBag<T> @this, IEnumerable<T> items)
		{
			foreach (var item in items)
				@this.Add(item);
		}
		public static void AddRange<T, U>(this ConcurrentDictionary<T, U> @this, IEnumerable<KeyValuePair<T, U>> items)
		{
			foreach (var item in items)
			{
				while (!@this.TryAdd(item.Key, item.Value))
				{
					Thread.Sleep(25);
				}
			}
		}
		public static void EnqueueRange<T>(this ConcurrentQueue<T> @this, IEnumerable<T> toAdd)
		{
			foreach (var element in toAdd)
			{
				@this.Enqueue(element);
			}
		}
		public static IList<T> DequeueRange<T>(this ConcurrentQueue<T> @this, int size)
		{
			List<T> res = new List<T>();
			int iterator = 0;
			while (iterator < size && @this.Count > 0)
			{
				iterator++;
				T temp;
				while (!@this.TryDequeue(out temp))
				{
					Thread.Sleep(50);
				}
				res.Add(temp);
			}
			return res;
		}


		public static IList<T> Shuffle<T>(this IList<T> @this)
		{
			var n = @this.Count;
			while (n > 1)
			{
				n--;
				var k = rng.Next(n + 1);
				(@this[k], @this[n]) = (@this[n], @this[k]);
			}
			return @this;
		}

		public static T GetRandom<T>(this IEnumerable<T> @this)
		{
			int rand = rng.Next(0, @this.Count() - 1);
			return @this.ElementAt(rand);
		}

		public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int numBatches)
		{
			var chunkSize = source.Count() / numBatches;
			return source.Chunk(chunkSize);
		}

		public static string StringJoin<T>(this IEnumerable<T> source, string separator)
		{
			return string.Join(separator, source);
		}
		
		public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
		{
			return items.GroupBy(property).Select(x => x.First());
		}
	}
}
