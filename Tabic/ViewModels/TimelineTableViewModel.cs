using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Tabic.Core.Models;
using Tabic.Core.Services;

namespace Tabic.ViewModels;

public partial class TimelineTableViewModel : ViewModelBase
{
    /// <summary>
    /// 纯数据模型
    /// </summary>
    public TimelineData Data { get; }

    /// <summary>
    /// 角色列表（横向表头）
    /// </summary>
    public ObservableCollection<Role> Roles { get; } = [];

    /// <summary>
    /// 表格行数据（包含时间点和单元格内容）
    /// </summary>
    public ObservableCollection<TableRowViewModel> TableRows { get; } = [];

    /// <summary>
    /// 数据变更事件（用于通知外部标记未保存）
    /// </summary>
    public event EventHandler? DataChanged;

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

    public ICommand AddRoleCommand { get; }
    public ICommand AddTimePointCommand { get; }
    public ICommand RemoveRoleCommand { get; }
    public ICommand RemoveTimePointCommand { get; }
    public ICommand InsertTimePointBelowCommand { get; }
    public ICommand InsertTimePointAboveCommand { get; }

    public TimelineTableViewModel(TimelineData data)
    {
        Data = data;
        Data.Changed += (s, e) => DataChanged?.Invoke(this, EventArgs.Empty);

        AddRoleCommand = new RelayCommand(AddRole);
        AddTimePointCommand = new RelayCommand(AddTimePoint);
        RemoveRoleCommand = new RelayCommand(RemoveRole);
        RemoveTimePointCommand = new RelayCommand(RemoveTimePoint);
        InsertTimePointAboveCommand = new RelayCommand(InsertTimePointAbove);
        InsertTimePointBelowCommand = new RelayCommand(InsertTimePointBelow);
    }

    private void AddRole()
    {
        var role = Data.AddRole();

        foreach (var row in TableRows)
        {
            var cellVm = new CellViewModel
            {
                RoleId = role.Id,
                Content = ""
            };
            AttachCellEvents(cellVm, row.TimePoint.Id);
            row.Cells.Add(cellVm);
        }

        Roles.Add(role);
    }

    private void AddTimePoint()
    {
        var timePoint = Data.AddTimePoint();

        var row = new TableRowViewModel
        {
            TimePoint = timePoint,
            Cells = []
        };

        foreach (var role in Roles)
        {
            var content = Data.GetCellContent(timePoint.Id, role.Id);
            var cellVm = new CellViewModel
            {
                RoleId = role.Id,
                Content = content
            };
            AttachCellEvents(cellVm, timePoint.Id);
            row.Cells.Add(cellVm);
        }

        TableRows.Add(row);
    }

    private void RemoveRole()
    {
        if (SelectedRole == null) return;
        var role = SelectedRole;

        Data.RemoveRole(role);

        foreach (var row in TableRows)
        {
            var cellToRemove = row.Cells.FirstOrDefault(c => c.RoleId == role.Id);
            if (cellToRemove != null)
            {
                row.Cells.Remove(cellToRemove);
            }
        }

        Roles.Remove(role);

        if (SelectedRole == role)
        {
            SelectedRole = null;
        }
    }

    private void RemoveTimePoint()
    {
        if (SelectedRow == null) return;
        var row = SelectedRow;

        Data.RemoveTimePoint(row.TimePoint);
        TableRows.Remove(row);

        if (SelectedRow == row)
        {
            SelectedRow = null;
        }
    }

    private void InsertTimePointAbove()
    {
        if (SelectedRow == null) return;
        var referenceRow = SelectedRow;

        var newTimePoint = Data.InsertTimePointAbove(referenceRow.TimePoint);
        var newRow = CreateRowViewModel(newTimePoint);

        var index = TableRows.IndexOf(referenceRow);
        if (index < 0) index = 0;
        TableRows.Insert(index, newRow);
    }

    private void InsertTimePointBelow()
    {
        if (SelectedRow == null) return;
        var referenceRow = SelectedRow;

        var newTimePoint = Data.InsertTimePointBelow(referenceRow.TimePoint);
        var newRow = CreateRowViewModel(newTimePoint);

        var index = TableRows.IndexOf(referenceRow);
        if (index < 0) index = TableRows.Count;
        else index++;
        TableRows.Insert(index, newRow);
    }

    private TableRowViewModel CreateRowViewModel(TimePoint timePoint)
    {
        var row = new TableRowViewModel
        {
            TimePoint = timePoint,
            Cells = []
        };

        foreach (var role in Roles)
        {
            var content = Data.GetCellContent(timePoint.Id, role.Id);
            var cellVm = new CellViewModel
            {
                RoleId = role.Id,
                Content = content
            };
            AttachCellEvents(cellVm, timePoint.Id);
            row.Cells.Add(cellVm);
        }

        return row;
    }

    public void Clear()
    {
        Data.Clear();
        Roles.Clear();
        TableRows.Clear();
        SelectedRole = null;
        SelectedRow = null;
    }

    public void LoadFromDocument(DocumentData document)
    {
        Data.LoadFromDocument(document);

        Roles.Clear();
        foreach (var role in Data.Roles)
        {
            Roles.Add(role);
        }

        TableRows.Clear();
        foreach (var timePoint in Data.TimePoints)
        {
            var row = new TableRowViewModel
            {
                TimePoint = timePoint,
                Cells = []
            };

            foreach (var role in Data.Roles)
            {
                var content = Data.GetCellContent(timePoint.Id, role.Id);
                var cellVm = new CellViewModel
                {
                    RoleId = role.Id,
                    Content = content
                };
                AttachCellEvents(cellVm, timePoint.Id);
                row.Cells.Add(cellVm);
            }

            TableRows.Add(row);
        }

        SelectedRole = null;
        SelectedRow = null;
    }

    private void AttachCellEvents(CellViewModel cellVm, string timePointId)
    {
        cellVm.ContentChanged += (s, e) =>
        {
            Data.SetCellContent(timePointId, cellVm.RoleId, cellVm.Content);
        };
    }
}
