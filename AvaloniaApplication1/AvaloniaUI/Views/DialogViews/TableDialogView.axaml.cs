namespace AvaloniaUI.Views.DialogViews;

public partial class TableDialogView : UserControl
{
    public TableDialogView() => InitializeComponent();

    // Columns come from the view model; each row is a string array bound by index.
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not TableDialogViewModel vm) return;
        var grid = this.FindControl<DataGrid>("Table")!;
        grid.Columns.Clear();
        for (int i = 0; i < vm.Columns.Count; i++)
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = vm.Columns[i],
                Binding = new Avalonia.Data.Binding($"[{i}]"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Auto),
                MaxWidth = 480,
            });
    }
}
