using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using Tabic.Models;

namespace Tabic.ViewModels;

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
    public ICommand ExportJsonCommand { get; }
    public ICommand ImportJsonCommand { get; }
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
        NewProjectCommand = new RelayCommand(NewProject);
        OpenProjectCommand = new RelayCommand(OpenProject);
        SaveProjectCommand = new RelayCommand(SaveProject);
        ExportJsonCommand = new RelayCommand(ExportJson);
        ImportJsonCommand = new RelayCommand(ImportJson);
        ExitCommand = new RelayCommand(Exit);
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
            ZoomLevel = System.Math.Min(ZoomLevel + ZoomStep, MaxZoom);
        }
    }

    /// <summary>
    /// 缩小
    /// </summary>
    private void ZoomOut()
    {
        if (ZoomLevel > MinZoom)
        {
            ZoomLevel = System.Math.Max(ZoomLevel - ZoomStep, MinZoom);
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
    private void NewProject()
    {
        Roles.Clear();
        TableRows.Clear();
    }

    /// <summary>
    /// 打开项目（从JSON文件）
    /// </summary>
    private void OpenProject()
    {
        // 简化实现，实际应该使用文件对话框
        ImportJson();
    }

    /// <summary>
    /// 保存项目（到JSON文件）
    /// </summary>
    private void SaveProject()
    {
        // 简化实现，实际应该使用文件对话框
        ExportJson();
    }

    /// <summary>
    /// 导出为JSON
    /// </summary>
    private void ExportJson()
    {
        var projectData = new ProjectData
        {
            Roles = Roles.ToList(),
            Rows = TableRows.Select(r => new RowData
            {
                TimePoint = r.TimePoint,
                Cells = r.Cells.Select(c => new CellContent
                {
                    RoleId = c.RoleId,
                    TimePointId = c.TimePointId,
                    Content = c.Content
                }).ToList()
            }).ToList()
        };

        var json = JsonSerializer.Serialize(projectData, new JsonSerializerOptions { WriteIndented = true });
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Tabic_Project.json");
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// 从JSON导入
    /// </summary>
    private void ImportJson()
    {
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Tabic_Project.json");
        if (!File.Exists(filePath)) return;

        var json = File.ReadAllText(filePath);
        var projectData = JsonSerializer.Deserialize<ProjectData>(json);
        if (projectData == null) return;

        Roles.Clear();
        TableRows.Clear();

        foreach (var role in projectData.Roles)
        {
            Roles.Add(role);
        }

        foreach (var rowData in projectData.Rows)
        {
            var row = new TableRowViewModel
            {
                TimePoint = rowData.TimePoint,
                Cells = []
            };

            foreach (var cell in rowData.Cells)
            {
                row.Cells.Add(new CellViewModel
                {
                    RoleId = cell.RoleId,
                    TimePointId = cell.TimePointId,
                    Content = cell.Content
                });
            }

            TableRows.Add(row);
        }
    }

    /// <summary>
    /// 退出应用
    /// </summary>
    private void Exit()
    {
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
            row.Cells.Add(new CellViewModel
            {
                RoleId = newRole.Id,
                TimePointId = row.TimePoint.Id,
                Content = ""
            });
        }
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
            row.Cells.Add(new CellViewModel
            {
                RoleId = role.Id,
                TimePointId = newTimePoint.Id,
                Content = ""
            });
        }

        TableRows.Add(row);
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
    }

    /// <summary>
    /// 删除选中时间点
    /// </summary>
    private void RemoveTimePoint()
    {
        if (SelectedRow == null) return;

        TableRows.Remove(SelectedRow);
        SelectedRow = null;
    }

    /// <summary>
    /// 显示关于对话框
    /// </summary>
    private void ShowAbout()
    {
        // 简化实现，实际应该显示对话框
        System.Console.WriteLine("Tabic - 角色时间线表格工具 v1.0");
    }

    private void InitializeData()
    {
        // 初始化角色（横向表头）
        var roles = new List<Role>
        {
            new() { Id = "role1", Name = "主角" },
            new() { Id = "role2", Name = "配角A" },
            new() { Id = "role3", Name = "配角B" },
            new() { Id = "role4", Name = "反派" },
        };

        foreach (var role in roles)
        {
            Roles.Add(role);
        }

        // 初始化时间点（纵向表头）
        var timePoints = new List<TimePoint>
        {
            new() { Id = "t1", Name = "第一章" },
            new() { Id = "t2", Name = "第二章" },
            new() { Id = "t3", Name = "第三章" },
            new() { Id = "t4", Name = "第四章" },
            new() { Id = "t5", Name = "第五章" },
        };

        // 初始化表格数据
        foreach (var timePoint in timePoints)
        {
            var row = new TableRowViewModel
            {
                TimePoint = timePoint,
                Cells = []
            };

            foreach (var role in roles)
            {
                row.Cells.Add(new CellViewModel
                {
                    RoleId = role.Id,
                    TimePointId = timePoint.Id,
                    Content = $"{role.Name}在{timePoint.Name}的内容"
                });
            }

            TableRows.Add(row);
        }
    }
}
