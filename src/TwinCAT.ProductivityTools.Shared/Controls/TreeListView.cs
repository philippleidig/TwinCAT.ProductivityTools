using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TwinCAT.ProductivityTools.Controls
{
	public class TreeListView : TreeView
	{
		public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
			"Columns",
			typeof(GridViewColumnCollection),
			typeof(TreeListView),
			new UIPropertyMetadata(null)
		);

		public static readonly DependencyProperty AllowsColumnReorderProperty =
			DependencyProperty.Register(
				"AllowsColumnReorder",
				typeof(bool),
				typeof(TreeListView),
				new UIPropertyMetadata(null)
			);

		public event EventHandler<ItemDoubleClickedEventArgs> ItemDoubleClicked;

		static TreeListView()
		{
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(TreeListView),
				new FrameworkPropertyMetadata(typeof(TreeListView))
			);
		}

		public TreeListView()
		{
			base.SelectedItemChanged += new RoutedPropertyChangedEventHandler<Object>(
				TreeListView_SelectedItemChanged
			);
			Columns = new GridViewColumnCollection();

			this.MouseDoubleClick += OnMouseDoubleClick;
		}

		public GridViewColumnCollection Columns
		{
			get { return (GridViewColumnCollection)GetValue(ColumnsProperty); }
			set { SetValue(ColumnsProperty, value); }
		}

		public bool AllowsColumnReorder
		{
			get { return (bool)GetValue(AllowsColumnReorderProperty); }
			set { SetValue(AllowsColumnReorderProperty, value); }
		}

		public static readonly DependencyProperty SelectedItemsProperty =
			DependencyProperty.Register(
				"SelectedItem",
				typeof(Object),
				typeof(TreeListView),
				new PropertyMetadata(null)
			);
		public new Object SelectedItem
		{
			get { return (Object)GetValue(SelectedItemProperty); }
			set
			{
				SetValue(SelectedItemsProperty, value);
				NotifyPropertyChanged("SelectedItem");
			}
		}

		private void TreeListView_SelectedItemChanged(
			Object sender,
			RoutedPropertyChangedEventArgs<Object> e
		)
		{
			this.SelectedItem = base.SelectedItem;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String aPropertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(aPropertyName));
		}

		private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = GetClickedItem(e);

			if (item != null)
			{
				OnItemDoubleClicked(new ItemDoubleClickedEventArgs(item));
			}
		}

		private object GetClickedItem(MouseButtonEventArgs e)
		{
			var element = e.OriginalSource as FrameworkElement;
			if (element != null)
			{
				return element.DataContext;
			}
			return null;
		}

		protected virtual void OnItemDoubleClicked(ItemDoubleClickedEventArgs e)
		{
			ItemDoubleClicked?.Invoke(this, e);
		}
	}

	public class ItemDoubleClickedEventArgs : EventArgs
	{
		public object ClickedItem { get; private set; }

		public ItemDoubleClickedEventArgs(object clickedItem)
		{
			this.ClickedItem = clickedItem;
		}
	}
}
