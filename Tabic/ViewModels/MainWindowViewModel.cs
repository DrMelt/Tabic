using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using Tabic.Core.Models;
using Tabic.Services;

namespace Tabic.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// 表格视图模型
    /// </summary>
    public TimelineTableViewModel TimelineTable { get; }

    /// <summary>
    /// 角色列表（横向表头）
    /// </summary>
    public ObservableCollection<Role> Roles => TimelineTable.Roles;

    /// <summary>
    /// 表格行数据（包含时间点和单元格内容）
    /// </summary>
    public ObservableCollection<TableRowViewModel> TableRows => TimelineTable.TableRows;

    /// <summary>
    /// 缩放级别
    /// </summary>
    public double ZoomLevel => TimelineTable.ZoomLevel;

    /// <summary>
    /// 窗口标题
    /// </summary>
    public string WindowTitle => GetWindowTitle();

    /// <summary>
    /// 项目服务
    /// </summary>
    private readonly ProjectService _projectService;

    /// <summary>
    /// 对话框服务
    /// </summary>
    private readonly DialogService _dialogService;

    // 选中项
    public Role? SelectedRole
    {
        get => TimelineTable.SelectedRole;
        set => TimelineTable.SelectedRole = value;
    }

    public TableRowViewModel? SelectedRow
    {
        get => TimelineTable.SelectedRow;
        set => TimelineTable.SelectedRow = value;
    }

    // 命令
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ResetZoomCommand { get; }
    public ICommand NewProjectCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand SaveProjectCommand { get; }

    public ICommand ExitCommand { get; }
    public ICommand AddRoleCommand => TimelineTable.AddRoleCommand;
    public ICommand AddTimePointCommand => TimelineTable.AddTimePointCommand;
    public ICommand RemoveRoleCommand => TimelineTable.RemoveRoleCommand;
    public ICommand RemoveTimePointCommand => TimelineTable.RemoveTimePointCommand;
    public ICommand AboutCommand { get; }

    public MainWindowViewModel(DialogService dialogService, ProjectService projectService, TimelineTableViewModel timelineTable)
    {
        _dialogService = dialogService;
        _projectService = projectService;
        TimelineTable = timelineTable;

        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        ResetZoomCommand = new RelayCommand(ResetZoom);
        NewProjectCommand = new AsyncRelayCommand(NewProject);
        OpenProjectCommand = new AsyncRelayCommand(OpenProject);
        SaveProjectCommand = new AsyncRelayCommand(SaveProject);

        ExitCommand = new AsyncRelayCommand(Exit);
        AboutCommand = new RelayCommand(ShowAbout);

        _projectService.UnsavedStateChanged += (s, e) => OnPropertyChanged(nameof(WindowTitle));
        _projectService.DocumentPathChanged += (s, e) => OnPropertyChanged(nameof(WindowTitle));

        TimelineTable.DataChanged += (s, e) => _projectService.MarkAsUnsaved();
    }

    /// <summary>
    /// 放大
    /// </summary>
    private void ZoomIn()
    {
        TimelineTable.ZoomIn();
    }

    /// <summary>
    /// 缩小
    /// </summary>
    private void ZoomOut()
    {
        TimelineTable.ZoomOut();
    }

    /// <summary>
    /// 重置缩放
    /// </summary>
    private void ResetZoom()
    {
        TimelineTable.ResetZoom();
    }

    /// <summary>
    /// 新建项目
    /// </summary>
    private async Task NewProject()
    {
        // 检查是否有未保存的更改
        if (_projectService.HasUnsavedChanges)
        {
            var result = await _dialogService.ShowConfirmAsync(
                "未保存的更改",
                "当前项目有未保存的更改，是否保存？",
                "保存",
                "不保存",
                "取消");

            if (result == ConfirmResult.Cancel) return;
            if (result == ConfirmResult.Yes)
            {
                await SaveProject();
                if (_projectService.HasUnsavedChanges) return;
            }
        }

        // 清空数据，创建空白项目
        TimelineTable.Clear();
        _projectService.NewProject();
    }

    /// <summary>
    /// 打开项目
    /// </summary>
    private async Task OpenProject()
    {
        if (_projectService.HasUnsavedChanges)
        {
            var result = await _dialogService.ShowConfirmAsync(
                "未保存的更改",
                "当前项目有未保存的更改，是否保存？",
                "保存",
                "不保存",
                "取消");

            if (result == ConfirmResult.Cancel) return;
            if (result == ConfirmResult.Yes)
            {
                await SaveProject();
                if (_projectService.HasUnsavedChanges) return;
            }
        }

        var document = await _projectService.OpenProjectAsync();
        if (document == null) return;

        TimelineTable.LoadFromDocument(document);
    }

    /// <summary>
    /// 保存项目
    /// </summary>
    private async Task SaveProject()
    {
        var document = _projectService.BuildDocumentData(TimelineTable.Data);
        await _projectService.SaveProjectAsync(document);
    }

    /// <summary>
    /// 获取窗口标题
    /// </summary>
    private string GetWindowTitle()
    {
        return _projectService.GetWindowTitle();
    }

    /// <summary>
    /// 退出应用
    /// </summary>
    private async Task Exit()
    {
        if (_projectService.HasUnsavedChanges)
        {
            var result = await _dialogService.ShowConfirmAsync(
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
                if (_projectService.HasUnsavedChanges) return;
            }
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }

    /// <summary>
    /// 显示关于对话框
    /// </summary>
    private void ShowAbout()
    {
        // 简化实现，实际应该显示对话框
        Console.WriteLine("Tabic - v1.0");
    }

}
