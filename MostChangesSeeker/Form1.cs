using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace MostChangesSeeker
{
	public partial class Form1 : Form
	{
		readonly ChangesGetter m_changesGetter = new ChangesGetter();

		public Form1()
		{
			InitializeComponent();
		}

		private void SeekButtonClick(object sender, EventArgs e)
		{
			sourcePathTextBox.Enabled = false;
			seekButton.Enabled = false;
			backgroundWorker1.RunWorkerAsync();
		}

		private void BackgroundWorker1DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			m_changesGetter.Connect(tfsUrlTextBox.Text);
			int latestChangesetId = m_changesGetter.GetLatestChangesetId();

			var changesetsFilter = new ChangesetsFilter
			{
				WorkItemTypes = new List<string> { "Bug" }
			};
			var changesets = m_changesGetter.GetChanges(sourcePathTextBox.Text, changesetsFilter);

			int startChangesetId = 0;
			int changesetRange = 0;
			string prevPercentString = null;
			var changesFilter = new ChangesFilter
			{
				FileExtensions = new List<string> { ".cs" },
				ChangeTypes = new List<ChangeType> { ChangeType.Edit }
			};

			foreach (Changeset changeset in changesets)
			{
				if (startChangesetId == 0)
				{
					startChangesetId = changeset.ChangesetId;
					changesetRange = latestChangesetId - startChangesetId;
				}

				string percentString = ((changeset.ChangesetId - startChangesetId) * 100 / changesetRange) + " %";
				if (percentString != prevPercentString)
				{
					progressLabel.Invoke(new Action(() => progressLabel.Text = percentString));
					prevPercentString = percentString;
				}

				foreach (Change change in changeset.Changes)
				{
					if (changesFilter.ChangeTypes.All(c => (change.ChangeType & c) != c))
						continue;

					string fileName = change.Item.ServerItem;
					if (changesFilter.FileExtensions.All(f => f != Path.GetExtension(fileName)))
						continue;

					fileName = fileName.Split(new[] { sourcePathTextBox.Text }, StringSplitOptions.RemoveEmptyEntries)[0];
					var parts = fileName.Split(new[] { Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

					var childNodes = changesTreeView.Nodes[0].Nodes;
					foreach (string part in parts)
					{
						TreeNode node;
						if (!childNodes.ContainsKey(part))
						{
							node = new TreeNode(part) { Name = part };
							childNodes.Add(node);
						}
						else
						{
							node = childNodes.Find(part, false)[0];
						}
						childNodes = node.Nodes;
					}
					WorkItem workItem = changeset.WorkItems[0];
					childNodes.Add(workItem.Type.Name + " " + workItem.Id);
				}
			}
			UpdateNode(changesTreeView.Nodes[0]);
			changesTreeView.Invoke(new Action(() =>
				{
					progressLabel.Visible = false;
					changesTreeView.Visible = true;
				} ));
		}

		private void UpdateNode(TreeNode node)
		{
			if (node.Nodes.Count == 0)
			{
				node.Tag = 1;
				return;
			}
			int childrenSum = 0;
			var nodes = new List<TreeNode>();
			for (int i = 0; i < node.Nodes.Count; i++)
			{
				TreeNode childNode = node.Nodes[i];
				UpdateNode(childNode);
				childrenSum += (int)childNode.Tag;
				nodes.Add(childNode);
			}
			node.Text += " (" + childrenSum + ")";
			node.Tag = childrenSum;
			node.Nodes.Clear();
			nodes = nodes.OrderByDescending(i => (int) i.Tag).ToList();
			node.Nodes.AddRange(nodes.ToArray());
		}
	}
}
