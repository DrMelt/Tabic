using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using Tabic.Models;
using Tabic.Services;

namespace Tabic.ViewModels;

/// <summary>
/// 确认对话框结果
/// </summary>
public enum ConfirmResult
{
    Yes,
    No,
    Cancel
}

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// 角色列表（横向表头）
    /// </summary>
    public ObservableCollection<Role> Roles { get; } = [];

    /// <summary>
    /// 表格行数据（包含时间点和单元格内容）
    /// </summary>
    public ObservableCollection<TableRowViewModel> TableRows { get; } = [];

    private double _zoomLevel = 1.0;

    /// <summary>
    /// 缩放级别
    /// </summary>
    public double ZoomLevel
    {
        get => _zoomLevel;
        set => SetProperty(ref _zoomLevel, value);
    }

    /// <summary>
    /// 最小缩放级别
    /// </summary>
    public double MinZoom { get; } = 0.5;

    /// <summary>
    /// 最大缩放级别
    /// </summary>
    public double MaxZoom { get; } = 2.0;

    /// <summary>
    /// 缩放步长
    /// </summary>
    public double ZoomStep { get; } = 0.1;

    /// <summary>
    /// 窗口标题
    /// </summary>
    public string WindowTitle => GetWindowTitle();

    /// <summary>
    /// 当前文档路径
    /// </summary>
    private string? _currentDocumentPath;

    /// <summary>
    /// 是否有未保存的更改
    /// </summary>
    private bool _hasUnsavedChanges = false;

    /// <summary>
    /// 文档服务
    /// </summary>
    private readonly DocumentService _documentService = new();

    // 选中项
    private Role? _selectedRole;
    public Role? SelectedRole
    {
        get => _selectedRole;
        set => SetProperty(ref _selectedRole, value);
    }

    private TableRowViewModel? _selectedRow;
    public TableRowViewModel? SelectedRow
    {
        get => _selectedRow;
        set => SetProperty(ref _selectedRow, value);
    }

    // 命令
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ResetZoomCommand { get; }
    public ICommand NewProjectCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand SaveProjectCommand { get; }

    public ICommand ExitCommand { get; }
    public ICommand AddRoleCommand { get; }
    public ICommand AddTimePointCommand { get; }
    public ICommand RemoveRoleCommand { get; }
    public ICommand RemoveTimePointCommand { get; }
    public ICommand AboutCommand { get; }

    public MainWindowViewModel()
    {
        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        ResetZoomCommand = new RelayCommand(ResetZoom);
        NewProjectCommand = new AsyncRelayCommand(NewProject);
        OpenProjectCommand = new AsyncRelayCommand(OpenProject);
        SaveProjectCommand = new AsyncRelayCommand(SaveProject);

        ExitCommand = new AsyncRelayCommand(Exit);
        AddRoleCommand = new RelayCommand(AddRole);
        AddTimePointCommand = new RelayCommand(AddTimePoint);
        RemoveRoleCommand = new RelayCommand(RemoveRole);
        RemoveTimePointCommand = new RelayCommand(RemoveTimePoint);
        AboutCommand = new RelayCommand(ShowAbout);
        InitializeData();
    }

    /// <summary>
    /// 放大
    /// </summary>
    private void ZoomIn()
    {
        if (ZoomLevel < MaxZoom)
        {
            ZoomLevel = Math.Min(ZoomLevel + ZoomStep, MaxZoom);
        }
    }

    /// <summary>
    /// 缩小
    /// </summary>
    private void ZoomOut()
    {
        if (ZoomLevel > MinZoom)
        {
            ZoomLevel = Math.Max(ZoomLevel - ZoomStep, MinZoom);
        }
    }

    /// <summary>
    /// 重置缩放
    /// </summary>
    private void ResetZoom()
    {
        ZoomLevel = 1.0;
    }

    /// <summary>
    /// 新建项目
    /// </summary>
    private async Task NewProject()
    {
        // 检查是否有未保存的更改
        if (_hasUnsavedChanges)
        {
            var result = await ShowConfirmDialogAsync(
                "未保存的更改",
                "当前项目有未保存的更改，是否保存？",
                "保存",
                "不保存",
                "取消");

            if (result == ConfirmResult.Cancel) return;
            if (result == ConfirmResult.Yes)
            {
                await SaveProject();
                if (_hasUnsavedChanges) return;
            }
        }

        // 清空数据，创建空白项目
        Roles.Clear();
        TableRows.Clear();
        _currentDocumentPath = null;
        _hasUnsavedChanges = false;
        OnPropertyChanged(nameof(WindowTitle));
    }

    /// <summary>
    /// 打开项目
    /// </summary>
    private async Task OpenProject()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "打开项目文件",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Tabic 项目文件")
                {
                    Patterns = ["*.tabic"]
                }
            ]
        };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
            && lifetime.MainWindow != null)
        {
            var result = await lifetime.MainWindow.StorageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0)
            {
                var filePath = result[0].Path.LocalPath;
                try
                {
                    var document = await DocumentService.LoadDocumentAsync(filePath);

                    // 加载角色
                    Roles.Clear();
                    foreach (var role in document.Roles)
                    {
                        Roles.Add(role);
                    }

                    // 加载时间点并重建行数据
                    TableRows.Clear();
                    foreach (var timePoint in document.TimePoints)
                    {
                        var row = new TableRowViewModel
                        {
                            TimePoint = timePoint,
                            Cells = []
                        };

                        // 为该时间点的每个角色创建单元格
                        foreach (var role in Roles)
                        {
                            // 查找对应的单元格内容
                            var cellContent = document.Cells.FirstOrDefault(
                                c => c.TimePointId == timePoint.Id && c.RoleId == role.Id);

                            var cellVm = new CellViewModel
                            {
                                RoleId = role.Id,
                                Content = cellContent?.Content ?? string.Empty
                            };
                            cellVm.ContentChanged += (s, e) => MarkAsUnsaved();
                            row.Cells.Add(cellVm);
                        }

                        TableRows.Add(row);
                    }

                    _currentDocumentPath = filePath;
                    _hasUnsavedChanges = false;
                    OnPropertyChanged(nameof(WindowTitle));
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync("打开项目失败", ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// 保存项目
    /// </summary>
    private async Task SaveProject()
    {
        var documentPath = _currentDocumentPath;

        if (string.IsNullOrEmpty(documentPath))
        {
            // 首次保存，选择位置
            var options = new FilePickerSaveOptions
            {
                Title = "保存项目",
                DefaultExtension = DocumentService.Extension,
                SuggestedFileName = "未命名项目",
                FileTypeChoices =
                [
                    new FilePickerFileType("Tabic 项目文件")
                    {
                        Patterns = ["*.tabic"]
                    }
                ]
            };

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
                && lifetime.MainWindow != null)
            {
                var result = await lifetime.MainWindow.StorageProvider.SaveFilePickerAsync(options);
                if (result != null)
                {
                    documentPath = result.Path.LocalPath;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        await SaveProjectInternalAsync(documentPath);
    }

    /// <summary>
    /// 内部保存方法
    /// </summary>
    private async Task SaveProjectInternalAsync(string? documentPath = null)
    {
        documentPath ??= _currentDocumentPath;
        if (string.IsNullOrEmpty(documentPath)) return;

        // 收集所有非空单元格（离散存储）
        var cells = new List<CellContent>();
        foreach (var row in TableRows)
        {
            foreach (var cell in row.Cells)
            {
                if (!string.IsNullOrWhiteSpace(cell.Content))
                {
                    cells.Add(new CellContent
                    {
                        TimePointId = row.TimePoint.Id,
                        RoleId = cell.RoleId,
                        Content = cell.Content
                    });
                }
            }
        }

        var document = new DocumentData
        {
            Title = Path.GetFileNameWithoutExtension(documentPath),
            Roles = [.. Roles],
            TimePoints = [.. TableRows.Select(r => r.TimePoint)],
            Cells = cells
        };

        await DocumentService.SaveDocumentAsync(documentPath, document);
        _currentDocumentPath = documentPath;
        _hasUnsavedChanges = false;
        OnPropertyChanged(nameof(WindowTitle));
    }

    /// <summary>
    /// 标记有未保存的更改
    /// </summary>
    private void MarkAsUnsaved()
    {
        _hasUnsavedChanges = true;
        OnPropertyChanged(nameof(WindowTitle));
    }

    /// <summary>
    /// 获取窗口标题
    /// </summary>
    private string GetWindowTitle()
    {
        var fileName = string.IsNullOrEmpty(_currentDocumentPath)
            ? "未命名项目"
            : Path.GetFileNameWithoutExtension(_currentDocumentPath);
        var unsavedMarker = _hasUnsavedChanges ? "* " : "";
        return $"{unsavedMarker}{fileName} - Tabic";
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    private static async Task ShowErrorDialogAsync(string title, string message)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
            && lifetime.MainWindow != null)
        {
            var msgBox = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var okButton = new Button
            {
                Content = "确定",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                [Grid.RowProperty] = 1
            };
            okButton.Click += (s, e) => msgBox.Close();

            msgBox.Content = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
                Margin = new Thickness(20),
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    },
                    okButton
                }
            };

            await msgBox.ShowDialog(lifetime.MainWindow);
        }
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    private async Task<ConfirmResult> ShowConfirmDialogAsync(
        string title,
        string message,
        string yesText,
        string noText,
        string cancelText)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime
            || lifetime.MainWindow == null)
        {
            return ConfirmResult.Cancel;
        }

        var tcs = new TaskCompletionSource<ConfirmResult>();

        var msgBox = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var yesButton = new Button { Content = yesText, Margin = new Thickness(5) };
        var noButton = new Button { Content = noText, Margin = new Thickness(5) };
        var cancelButton = new Button { Content = cancelText, Margin = new Thickness(5) };

        yesButton.Click += (s, e) => { tcs.SetResult(ConfirmResult.Yes); msgBox.Close(); };
        noButton.Click += (s, e) => { tcs.SetResult(ConfirmResult.No); msgBox.Close(); };
        cancelButton.Click += (s, e) => { tcs.SetResult(ConfirmResult.Cancel); msgBox.Close(); };

        msgBox.Content = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Margin = new Thickness(20),
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    [Grid.RowProperty] = 1,
                    Children = { yesButton, noButton, cancelButton }
                }
            }
        };

        msgBox.Closed += (s, e) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(ConfirmResult.Cancel);
        };

        await msgBox.ShowDialog(lifetime.MainWindow);
        return await tcs.Task;
    }

    /// <summary>
    /// 退出应用
    /// </summary>
    private async Task Exit()
    {
        if (_hasUnsavedChanges)
        {
            var result = await ShowConfirmDialogAsync(
                "未保存的更改",
                "您有未保存的更改，是否保存？",
                "保存",
                "不保存",
                "取消");

            if (result == ConfirmResult.Cancel)
            {
                return;
            }
            else if (result == ConfirmResult.Yes)
            {
                await SaveProject();
                // 如果保存失败（如用户取消对话框），不退出
                if (_hasUnsavedChanges) return;
            }
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }

    /// <summary>
    /// 添加角色
    /// </summary>
    private void AddRole()
    {
        var newId = $"role{Roles.Count + 1}";
        var newRole = new Role { Id = newId, Name = $"新角色{Roles.Count + 1}" };
        Roles.Add(newRole);

        // 为新角色在每个时间点添加单元格
        foreach (var row in TableRows)
        {
            var cellVm = new CellViewModel
            {
                RoleId = newRole.Id,
                Content = ""
            };
            cellVm.ContentChanged += (s, e) => MarkAsUnsaved();
            row.Cells.Add(cellVm);
        }

        MarkAsUnsaved();
    }

    /// <summary>
    /// 添加时间点
    /// </summary>
    private void AddTimePoint()
    {
        var newId = $"t{TableRows.Count + 1}";
        var newTimePoint = new TimePoint { Id = newId, Name = $"新时间点{TableRows.Count + 1}" };

        var row = new TableRowViewModel
        {
            TimePoint = newTimePoint,
            Cells = []
        };

        foreach (var role in Roles)
        {
            var cellVm = new CellViewModel
            {
                RoleId = role.Id,
                Content = ""
            };
            cellVm.ContentChanged += (s, e) => MarkAsUnsaved();
            row.Cells.Add(cellVm);
        }

        TableRows.Add(row);
        MarkAsUnsaved();
    }

    /// <summary>
    /// 删除选中角色
    /// </summary>
    private void RemoveRole()
    {
        if (SelectedRole == null) return;

        // 删除角色
        Roles.Remove(SelectedRole);

        // 删除该角色的所有单元格
        foreach (var row in TableRows)
        {
            var cellToRemove = row.Cells.FirstOrDefault(c => c.RoleId == SelectedRole.Id);
            if (cellToRemove != null)
            {
                row.Cells.Remove(cellToRemove);
            }
        }

        SelectedRole = null;
        MarkAsUnsaved();
    }

    /// <summary>
    /// 删除选中时间点
    /// </summary>
    private void RemoveTimePoint()
    {
        if (SelectedRow == null) return;

        TableRows.Remove(SelectedRow);
        SelectedRow = null;
        MarkAsUnsaved();
    }

    /// <summary>
    /// 显示关于对话框
    /// </summary>
    private void ShowAbout()
    {
        // 简化实现，实际应该显示对话框
        Console.WriteLine("Tabic - 角色时间线表格工具 v1.0");
    }

    private void InitializeData()
    {
        // 应用启动时显示空项目，不填充默认数据
        // 用户可以通过菜单添加角色和时间点
    }
}
