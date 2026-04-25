using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Tabic.Views.TimelineTable;

public partial class TableCell : UserControl
{
    private static readonly IBrush InnerBorderBackground = new SolidColorBrush(Color.Parse("#D0D5E0"));
    private static readonly BoxShadows InnerBorderBoxShadow = new(new BoxShadow
    {
        OffsetX = 0,
        OffsetY = 1,
        Blur = 3,
        Spread = 0,
        Color = Color.Parse("#10000000")
    });

    private Border? _innerBorder;
    private TextBox? _cellTextBox;

    public TableCell()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _innerBorder = this.FindControl<Border>("InnerBorder");
        _cellTextBox = this.FindControl<TextBox>("CellTextBox");

        if (_cellTextBox != null)
        {
            _cellTextBox.TextChanged += (_, _) => UpdateInnerBorder();
            _cellTextBox.GotFocus += (_, _) => UpdateInnerBorder();
            _cellTextBox.LostFocus += (_, _) => UpdateInnerBorder();
        }

        UpdateInnerBorder();
    }

    private void UpdateInnerBorder()
    {
        if (_innerBorder == null || _cellTextBox == null) return;

        bool show = !string.IsNullOrEmpty(_cellTextBox.Text) || _cellTextBox.IsFocused;
        _innerBorder.Background = show ? InnerBorderBackground : Brushes.Transparent;
        _innerBorder.BoxShadow = show ? InnerBorderBoxShadow : default;
    }
}
