using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Io;

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
			_reloadCommand = new RelayCommand(Reload);

			_selectCommand = new RelayCommand<TreeNode>(OnTreeItemSelected);
			_doubleClickCommand = new RelayCommand<TreeNode>(OnTreeItemDoubleClicked);
			_goToDestinationCommand = new RelayCommand(GoToDestination);
			_goToSourceCommand = new RelayCommand(GoToSource);
		}

		private ITcSysManager3 _systemManager;

		private RelayCommand _reloadCommand;
		public ICommand ReloadCommand => _reloadCommand;

		private RelayCommand<TreeNode> _selectCommand;
		public ICommand SelectCommand => _selectCommand;

		private RelayCommand<TreeNode> _doubleClickCommand;
		public ICommand DoubleClickCommand => _doubleClickCommand;

		private RelayCommand _goToDestinationCommand;
		public ICommand GoToDestinationCommand => _goToDestinationCommand;

		private RelayCommand _goToSourceCommand;
		public ICommand GoToSourceCommand => _goToSourceCommand;

		public ObservableCollection<TreeNode> TreeData { get; set; }

		public ObservableCollection<TreeNode> FilteredTreeData { get; set; }

		private Dictionary<string, List<Variable>> _variables;

		public Dictionary<string, List<Variable>> Variables
		{
			get => _variables;
			set => SetProperty(ref _variables, value);
		}

		private string _searchQuery;

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

		public void Reload()
		{
			string xmlMappingInfo = _systemManager?.ProduceMappingInfo();
			Variables =
				new IoMappingParser().Parse(xmlMappingInfo) as Dictionary<string, List<Variable>>;

			TreeData = new ObservableCollection<TreeNode>(BuildTree(Variables));
			FilteredTreeData = new ObservableCollection<TreeNode>(TreeData);

			SearchQuery = string.Empty;
		}

		private void OnTreeItemSelected(TreeNode node)
		{
			if (node != null)
			{
				Console.WriteLine($"Selected: {node.Name}");
			}
		}

		private void OnTreeItemDoubleClicked(TreeNode node)
		{
			if (node != null)
			{
				Console.WriteLine($"Double-clicked: {node.Name}");
			}
		}

		private void GoToDestination()
		{
			Console.WriteLine("Go To Destination executed");
		}

		private void GoToSource()
		{
			Console.WriteLine("Go To Source executed");
		}

		public Task InitializeAsync(ITcSysManager2 systemManager)
		{
			_systemManager = systemManager as ITcSysManager3;

			Reload();

			return Task.CompletedTask;
		}

		private void ApplyFilter()
		{
			if (string.IsNullOrEmpty(SearchQuery))
			{
				FilteredTreeData = new ObservableCollection<TreeNode>(TreeData);
			}
			else
			{
				var filtered = TreeData
					.Select(node => FilterNode(node, SearchQuery))
					.Where(node => node != null)
					.ToList();

				FilteredTreeData = new ObservableCollection<TreeNode>(filtered);
			}

			OnPropertyChanged(nameof(FilteredTreeData));
		}

		private TreeNode FilterNode(TreeNode node, string query)
		{
			if (node.Name.Contains(query))
			{
				return node;
			}

			var filteredChildren = node.Children.Select(child => FilterNode(child, query))
				.Where(child => child != null)
				.ToList();

			if (filteredChildren.Any())
			{
				return new TreeNode
				{
					Name = node.Name,
					Children = new ObservableCollection<TreeNode>(filteredChildren)
				};
			}

			return null;
		}

		public static Dictionary<string, List<Variable>> ExtractVariables(string mappings) =>
			(Dictionary<string, List<Variable>>)new IoMappingParser().Parse(mappings);

		private ObservableCollection<TreeNode> BuildTree(
			Dictionary<string, List<Variable>> dictionary
		)
		{
			var rootNodes = new ObservableCollection<TreeNode>();

			foreach (var kvp in dictionary)
			{
				string[] keyParts = kvp.Key.Split('^');
				AddToTree(rootNodes, keyParts, 0, kvp.Value);
			}

			return rootNodes;
		}

		private void AddToTree(
			ObservableCollection<TreeNode> nodes,
			string[] pathParts,
			int index,
			List<Variable> variables
		)
		{
			if (index >= pathParts.Length)
				return;

			var currentPart = pathParts[index];
			var existingNode = FindNode(nodes, currentPart);

			if (existingNode == null)
			{
				existingNode = new TreeNode { Name = currentPart };
				nodes.Add(existingNode);
			}

			if (index == pathParts.Length - 1)
			{
				foreach (var variable in variables)
				{
					existingNode.Children.Add(
						new TreeNode
						{
							Name =
								$"Name: {variable.Name}, Path: {variable.Path}, Size: {variable.Size}, Offset: {variable.Offset}"
						}
					);
				}
			}
			else
			{
				AddToTree(existingNode.Children, pathParts, index + 1, variables);
			}
		}

		private TreeNode FindNode(ObservableCollection<TreeNode> nodes, string name)
		{
			foreach (var node in nodes)
			{
				if (node.Name == name)
					return node;
			}

			return null;
		}
	}
}
