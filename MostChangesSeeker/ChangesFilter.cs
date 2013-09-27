using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace MostChangesSeeker
{
	internal class ChangesFilter
	{
		internal List<string> FileExtensions { get; set; }

		internal List<ChangeType> ChangeTypes { get; set; }
	}
}
