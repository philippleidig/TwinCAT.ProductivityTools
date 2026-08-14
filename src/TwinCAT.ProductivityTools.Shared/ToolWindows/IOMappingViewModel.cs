using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Io;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.ToolWindows
{
	public class TreeNode
	{
		public string Name { get; set; }

		public ObservableCollection<TreeNode> Children { get; set; } =
			new ObservableCollection<TreeNode>();

		public override string ToString() => Name;
	}

	internal class IOMappingViewModel : ObservableObject
	{
		public IOMappingViewModel()
		{
			ReloadCommand = new RelayCommand(Reload);
			CopyCommand = new RelayCommand<TreeNode>(Copy);
			ExportCommand = new RelayCommand(Export);
		}

		private ITcSysManager3 _systemManager;

		public ICommand ReloadCommand { get; }

		public ICommand CopyCommand { get; }

		public ICommand ExportCommand { get; }

		private ObservableCollection<TreeNode> _treeData = new ObservableCollection<TreeNode>();

		public ObservableCollection<TreeNode> TreeData
		{
			get => _treeData;
			private set => SetProperty(ref _treeData, value);
		}

		private ObservableCollection<TreeNode> _filteredTreeData =
			new ObservableCollection<TreeNode>();

		public ObservableCollection<TreeNode> FilteredTreeData
		{
			get => _filteredTreeData;
			private set => SetProperty(ref _filteredTreeData, value);
		}

		private IDictionary<string, List<Variable>> _variables =
			new Dictionary<string, List<Variable>>();

		public IDictionary<string, List<Variable>> Variables
		{
			get => _variables;
			private set => SetProperty(ref _variables, value);
		}

		private string _searchQuery = string.Empty;

		public string SearchQuery
		{
			get => _searchQuery;
			set
			{
				if (_searchQuery != value)
				{
					_searchQuery = value;
					OnPropertyChanged();
					ApplyFilter();
				}
			}
		}

		private string _status = string.Empty;

		public string Status
		{
			get => _status;
			private set => SetProperty(ref _status, value);
		}

		public void Reload()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				string xmlMappingInfo = _systemManager?.ProduceMappingInfo();

				Variables = new IoMappingParser().Parse(xmlMappingInfo);
				TreeData = BuildTree(Variables);

				// ApplyFilter is called explicitly instead of relying on the SearchQuery setter.
				// The setter only raises a change notification when the value actually changes,
				// so a reload with an unchanged filter would otherwise keep showing stale data.
				ApplyFilter();

				int links = Variables.Sum(entry => entry.Value?.Count ?? 0);
				Status = $"{links} link(s) in {Variables.Count} mapping(s).";
			}
			catch (Exception ex)
			{
				TreeData = new ObservableCollection<TreeNode>();
				FilteredTreeData = new ObservableCollection<TreeNode>();
				Status = "The mapping information could not be read.";

				ThreadHelper
					.JoinableTaskFactory.RunAsync(
						() => Report.FailureAsync("Failed to read the I/O mapping.", ex)
					)
					.FireAndForget();
			}
		}

		/// <summary>
		/// Copies the selected entry, or the whole tree when nothing is selected, to the
		/// clipboard.
		/// </summary>
		private void Copy(TreeNode node)
		{
			string text =
				node == null ? IoMappingCsv.Build(Variables) : Flatten(node, string.Empty);

			try
			{
				Clipboard.SetText(text);
				Status = "Copied to the clipboard.";
			}
			catch (Exception)
			{
				// Another process may hold the clipboard open. Losing a copy is not worth an
				// error dialog.
				Status = "The clipboard is not available.";
			}
		}

		private void Export()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				var dialog = new Microsoft.Win32.SaveFileDialog
				{
					Title = "Export I/O mappings",
					Filter = "CSV file (*.csv)|*.csv",
					DefaultExt = ".csv",
					FileName = "IoMappings.csv"
				};

				if (dialog.ShowDialog() != true)
				{
					return;
				}

				File.WriteAllText(dialog.FileName, IoMappingCsv.Build(Variables));

				Status = $"Exported to {dialog.FileName}.";
			}
			catch (Exception ex)
			{
				Status = "The export failed.";

				ThreadHelper
					.JoinableTaskFactory.RunAsync(
						() => Report.FailureAsync("Failed to export the I/O mapping.", ex)
					)
					.FireAndForget();
			}
		}

		private static string Flatten(TreeNode node, string prefix)
		{
			if (node == null)
			{
				return string.Empty;
			}

			string line = string.IsNullOrEmpty(prefix) ? node.Name : $"{prefix} / {node.Name}";

			if (node.Children.Count == 0)
			{
				return line;
			}

			return string.Join(
				Environment.NewLine,
				node.Children.Select(child => Flatten(child, line))
			);
		}

		public async Task InitializeAsync(ITcSysManager2 systemManager)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			_systemManager = systemManager as ITcSysManager3;

			Reload();
		}

		private void ApplyFilter()
		{
			if (string.IsNullOrEmpty(SearchQuery))
			{
				FilteredTreeData = new ObservableCollection<TreeNode>(TreeData);
				return;
			}

			FilteredTreeData = new ObservableCollection<TreeNode>(
				TreeData.Select(node => FilterNode(node, SearchQuery)).Where(node => node != null)
			);
		}

		private TreeNode FilterNode(TreeNode node, string query)
		{
			if (node == null)
			{
				return null;
			}

			if (node.Name?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return node;
			}

			var filteredChildren = node.Children.Select(child => FilterNode(child, query))
				.Where(child => child != null)
				.ToList();

			return filteredChildren.Any()
				? new TreeNode
				{
					Name = node.Name,
					Children = new ObservableCollection<TreeNode>(filteredChildren)
				}
				: null;
		}

		public static IDictionary<string, List<Variable>> ExtractVariables(string mappings) =>
			new IoMappingParser().Parse(mappings);

		internal static ObservableCollection<TreeNode> BuildTree(
			IDictionary<string, List<Variable>> dictionary
		)
		{
			var rootNodes = new ObservableCollection<TreeNode>();

			if (dictionary == null)
			{
				return rootNodes;
			}

			foreach (var kvp in dictionary)
			{
				string[] keyParts = (kvp.Key ?? string.Empty).Split('^');

				AddToTree(rootNodes, keyParts, 0, kvp.Value ?? new List<Variable>());
			}

			return rootNodes;
		}

		private static void AddToTree(
			ObservableCollection<TreeNode> nodes,
			string[] pathParts,
			int index,
			List<Variable> variables
		)
		{
			if (index >= pathParts.Length)
			{
				return;
			}

			string currentPart = pathParts[index];
			TreeNode existingNode = nodes.FirstOrDefault(
				node => string.Equals(node.Name, currentPart, StringComparison.Ordinal)
			);

			if (existingNode == null)
			{
				existingNode = new TreeNode { Name = currentPart };
				nodes.Add(existingNode);
			}

			if (index == pathParts.Length - 1)
			{
				foreach (Variable variable in variables.Where(variable => variable != null))
				{
					existingNode.Children.Add(
						new TreeNode
						{
							Name =
								$"{variable.Name} | {variable.Path} | size {variable.Size} | offset {variable.Offset}"
						}
					);
				}

				return;
			}

			AddToTree(existingNode.Children, pathParts, index + 1, variables);
		}
	}
}
