using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Collections;
using System.Linq;


namespace Tabic.Views.TimelineTable;

public partial class TimelineTable : UserControl
{
    private ScrollViewer? _headerScrollViewer;
    private ScrollViewer? _timePointScrollViewer;
    private ScrollViewer? _contentScrollViewer;
    private ItemsControl? _headerItemsControl;
    private ItemsControl? _contentItemsControl;
    private ItemsControl? _timePointItemsControl;

    // 自适应尺寸属性
    public static readonly StyledProperty<double> CellWidthProperty =
        AvaloniaProperty.Register<TimelineTable, double>(nameof(CellWidth), 150);

    public static readonly StyledProperty<double> CellHeightProperty =
        AvaloniaProperty.Register<TimelineTable, double>(nameof(CellHeight), 80);

    public double CellWidth
    {
        get => GetValue(CellWidthProperty);
        set => SetValue(CellWidthProperty, value);
    }

    public double CellHeight
    {
        get => GetValue(CellHeightProperty);
        set => SetValue(CellHeightProperty, value);
    }

    public TimelineTable()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 获取滚动视图控件引用
        _headerScrollViewer = this.FindControl<ScrollViewer>("HeaderScrollViewer");
        _timePointScrollViewer = this.FindControl<ScrollViewer>("TimePointScrollViewer");
        _contentScrollViewer = this.FindControl<ScrollViewer>("ContentScrollViewer");
        _headerItemsControl = this.FindControl<ItemsControl>("HeaderItemsControl");
        _contentItemsControl = this.FindControl<ItemsControl>("RowItemsControl");
        _timePointItemsControl = this.FindControl<ItemsControl>("TimePointItemsControl");

        // 监听内容区域行渲染完成，同步行高
        _contentItemsControl?.LayoutUpdated += OnContentLayoutUpdated;

        // 设置滚动同步
        _contentScrollViewer?.ScrollChanged += OnContentScrollChanged;

        // 初始计算尺寸
        UpdateCellSizes();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        // 取消事件订阅
        _contentScrollViewer?.ScrollChanged -= OnContentScrollChanged;
        _contentItemsControl?.LayoutUpdated -= OnContentLayoutUpdated;
    }

    private void OnContentLayoutUpdated(object? sender, EventArgs e)
    {
        SyncRowHeights();
    }

    private void SyncRowHeights()
    {
        if (_contentItemsControl?.Items == null || _timePointItemsControl?.Items == null) return;

        var contentItems = _contentItemsControl.GetVisualDescendants().OfType<Control>().Where(c => c.Parent == _contentItemsControl).ToList();
        var timePointItems = _timePointItemsControl.GetVisualDescendants().OfType<Control>().Where(c => c.Parent == _timePointItemsControl).ToList();

        var minCount = Math.Min(contentItems.Count, timePointItems.Count);
        for (int i = 0; i < minCount; i++)
        {
            if (contentItems[i] is Control contentRow && timePointItems[i] is Control timePointRow)
            {
                var contentHeight = contentRow.Bounds.Height;
                if (contentHeight > 0 && timePointRow.Height != contentHeight)
                {
                    timePointRow.Height = contentHeight;
                }
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateCellSizes();
    }

    private void UpdateCellSizes()
    {
        if (_contentScrollViewer == null) return;

        var availableHeight = _contentScrollViewer.Bounds.Height;

        var rowsCount = (_contentItemsControl?.ItemsSource as IEnumerable)?.Cast<object>().Count() ?? 0;

        if (rowsCount > 0 && availableHeight > 0)
        {
            // 计算每个单元格的高度（均匀分配，但有最小值）
            var newCellHeight = Math.Max(60, availableHeight / Math.Min(rowsCount, 6));
            CellHeight = newCellHeight;
        }
    }

    private void OnContentScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // 同步横向滚动到表头
        if (_headerScrollViewer != null && e.OffsetDelta.X != 0)
        {
            _headerScrollViewer.Offset = new Vector(
                _contentScrollViewer?.Offset.X ?? 0,
                _headerScrollViewer.Offset.Y);
        }

        // 同步纵向滚动到时间点列
        if (_timePointScrollViewer != null && e.OffsetDelta.Y != 0)
        {
            _timePointScrollViewer.Offset = new Vector(
                _timePointScrollViewer.Offset.X,
                _contentScrollViewer?.Offset.Y ?? 0);
        }
    }
}
