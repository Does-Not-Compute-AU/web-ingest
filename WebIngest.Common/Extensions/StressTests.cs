using System;
using System.Linq;
using System.Threading.Tasks;
using WebIngest.Common.Extensions;

namespace WebIngest.Common.Utils
{
	public static class StressTests
	{
		public static void CalculatePrimes()
		{
			var chunks =
				Enumerable.Range(0, int.MaxValue)
					.Chunk(int.MaxValue / Environment.ProcessorCount * 2);

			Parallel.ForEach(chunks, set =>
			{
				for (int i = set.First(); i < set.Last(); i++)
				{
					enqueue:
					try
					{
						IsPrime(i);
					}
					catch (Exception ex)
					{
						goto enqueue;
					}
				}
			});
		}

		public static bool IsPrime(int number)
		{
			if (number == 1) return false;
			if (number == 2) return true;
			if (number % 2 == 0) return false;

			var boundary = (int)Math.Floor(Math.Sqrt(number));

			for (int i = 3; i <= boundary; i += 2)
			{
				if (number % i == 0) return false;
			}

			return true;
		}
	}
}
