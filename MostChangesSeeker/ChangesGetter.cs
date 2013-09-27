using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace MostChangesSeeker
{
	internal class ChangesGetter
	{
		private VersionControlServer m_vcs;

		internal void Connect(string sourceControlUrl)
		{
			var credentials = new UICredentialsProvider();
			var tpc = new TfsTeamProjectCollection(new Uri(sourceControlUrl), credentials);
			tpc.EnsureAuthenticated();
			m_vcs = tpc.GetService<VersionControlServer>();
		}

		internal int GetLatestChangesetId()
		{
			return m_vcs.GetLatestChangesetId();
		}

		internal IEnumerable<Changeset> GetChanges(string serverPath, ChangesetsFilter filter)
		{
			IEnumerable csList = m_vcs.QueryHistory(
				serverPath,
				VersionSpec.Latest,
				0,
				RecursionType.Full,
				null, //any user
				null, // from first changeset
				null, // to last changeset
				int.MaxValue,
				true, // with changes
				false,
				false,
				true); // sorted

			var enumerator = csList.GetEnumerator();
			while (enumerator.MoveNext())
			{
				var changeset = (Changeset) enumerator.Current;

				var workItem = changeset.WorkItems.FirstOrDefault(wi => filter.WorkItemTypes.Any(t => t == wi.Type.Name));
				if (workItem == null)
					continue;

				yield return changeset;
			}
		}
	}
}
