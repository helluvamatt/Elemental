using System;

namespace Elemental.Async
{
	public class TrackedResult : BasicResult
	{
		public Nullable<int> ResultId { get; set; }
	}
}
