using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Tabic.ViewModels;

namespace Tabic.Views.TimelineTable;

public partial class TimePointHeader : UserControl
{
    public TimePointHeader()
    {
        InitializeComponent();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not TableRowViewModel rowVm) return;

        var itemsControl = this.FindAncestorOfType<ItemsControl>();
        if (itemsControl?.DataContext is not TimelineTableViewModel vm) return;

        vm.SelectedRow = rowVm;
    }
}
