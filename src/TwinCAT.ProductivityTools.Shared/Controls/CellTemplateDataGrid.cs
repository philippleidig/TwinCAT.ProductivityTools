using System.Windows;
using System.Windows.Controls;

namespace TwinCAT.ProductivityTools.Controls
{
	public class CellTemplateDataGrid : DataGrid
	{
		public static readonly DependencyProperty CellTemplateSelectorProperty =
			DependencyProperty.Register(
				"Selector",
				typeof(DataTemplateSelector),
				typeof(CellTemplateDataGrid),
				new FrameworkPropertyMetadata(null)
			);
		public DataTemplateSelector CellTemplateSelector
		{
			get { return (DataTemplateSelector)GetValue(CellTemplateSelectorProperty); }
			set { SetValue(CellTemplateSelectorProperty, value); }
		}

		protected override void OnAutoGeneratingColumn(DataGridAutoGeneratingColumnEventArgs e)
		{
			e.Cancel = true;
			Columns.Add(
				new DataGridTemplateColumn
				{
					Header = e.Column.Header,
					CellTemplateSelector = CellTemplateSelector,
				}
			);
		}
	}
}
