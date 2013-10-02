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
		TreeNodeCollection m_byPathNodes;
		TreeNodeCollection m_byFilesNodes;

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

			var byFilesDict = new Dictionary<string, int>();

			var byPathRootNode = new TreeNode();
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
					fileName = parts.Last();
					if (byFilesDict.ContainsKey(fileName))
						byFilesDict[fileName] = byFilesDict[fileName] + 1;
					else
						byFilesDict[fileName] = 1;

					var childNodes = byPathRootNode.Nodes;
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
			UpdateNode(byPathRootNode);
			m_byPathNodes = byPathRootNode.Nodes;

			var byCountDict = new Dictionary<int, List<string>>();
			foreach (var pair in byFilesDict)
			{
				if (byCountDict.ContainsKey(pair.Value))
					byCountDict[pair.Value].Add(pair.Key);
				else
					byCountDict.Add(pair.Value, new List<string>{pair.Key});
			}
			var countKeys = new List<int>(byCountDict.Keys);
			countKeys = countKeys.OrderByDescending(i => i).ToList();
			var byFilesRootNode = new TreeNode();
			foreach (int count in countKeys)
			{
				var node = new TreeNode(count.ToString()) { Name = count.ToString() };
				byFilesRootNode.Nodes.Add(node);
				var files = byCountDict[count];
				foreach (string file in files)
				{
					var childNode = new TreeNode(file) { Name = file };
					node.Nodes.Add(childNode);
				}
			}
			m_byFilesNodes = byFilesRootNode.Nodes;

			var nodeCollection = m_byFilesNodes;
			var rootNode = changesTreeView.Nodes[0];
			for (int i = 0; i < nodeCollection.Count; i++)
			{
				var item = nodeCollection[i];
				rootNode.Nodes.Add(item);
			}

			changesTreeView.Invoke(new Action(() =>
				{
					progressLabel.Visible = false;
					byFilesRadioButton.Visible = true;
					byPathRadioButton.Visible = true;
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

		private void ByPathRadioButtonCheckedChanged(object sender, EventArgs e)
		{
			var rootNode = changesTreeView.Nodes[0];
			rootNode.Nodes.Clear();
			for (int i = 0; i < m_byPathNodes.Count; i++)
			{
				var item = m_byPathNodes[i];
				rootNode.Nodes.Add(item);
			}
		}

		private void ByFilesRadioButtonCheckedChanged(object sender, EventArgs e)
		{
			var rootNode = changesTreeView.Nodes[0];
			rootNode.Nodes.Clear();
			for (int i = 0; i < m_byFilesNodes.Count; i++)
			{
				var item = m_byFilesNodes[i];
				rootNode.Nodes.Add(item);
			}
		}
	}
}
