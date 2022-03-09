using System;

namespace WebIngest.Common.Interfaces
{
	public abstract class DatedEntity
	{
		public DateTime Created { get; set; } = DateTime.UtcNow;

		public DateTime Updated { get; set; } = DateTime.UtcNow;
	}
}
