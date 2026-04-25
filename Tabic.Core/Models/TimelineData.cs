using Tabic.Core.Services;

namespace Tabic.Core.Models;

/// <summary>
/// 时间线纯数据模型，与 UI 无关
/// </summary>
public class TimelineData
{
    /// <summary>
    /// 角色列表
    /// </summary>
    public List<Role> Roles { get; } = [];

    /// <summary>
    /// 时间点列表
    /// </summary>
    public List<TimePoint> TimePoints { get; } = [];

    /// <summary>
    /// 单元格内容列表
    /// </summary>
    public List<CellContent> Cells { get; } = [];

    /// <summary>
    /// 数据变更事件
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// 添加角色
    /// </summary>
    public Role AddRole()
    {
        var newId = $"role{Roles.Count + 1}";
        var newRole = new Role { Id = newId, Name = $"新角色{Roles.Count + 1}" };
        Roles.Add(newRole);
        OnChanged();
        return newRole;
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    public void RemoveRole(Role role)
    {
        if (role == null) return;

        Roles.Remove(role);
        Cells.RemoveAll(c => c.RoleId == role.Id);
        OnChanged();
    }

    /// <summary>
    /// 添加时间点
    /// </summary>
    public TimePoint AddTimePoint()
    {
        var newId = $"t{TimePoints.Count + 1}";
        var newTimePoint = new TimePoint { Id = newId, Name = $"新时间点{TimePoints.Count + 1}" };
        TimePoints.Add(newTimePoint);
        OnChanged();
        return newTimePoint;
    }

    /// <summary>
    /// 删除时间点
    /// </summary>
    public void RemoveTimePoint(TimePoint timePoint)
    {
        if (timePoint == null) return;

        TimePoints.Remove(timePoint);
        Cells.RemoveAll(c => c.TimePointId == timePoint.Id);
        OnChanged();
    }

    /// <summary>
    /// 在指定时间点上方插入新时间点
    /// </summary>
    public TimePoint InsertTimePointAbove(TimePoint timePoint)
    {
        var index = TimePoints.IndexOf(timePoint);
        if (index < 0) index = 0;

        var newId = Guid.NewGuid().ToString("N")[..8];
        var newTimePoint = new TimePoint { Id = newId, Name = $"新时间点{TimePoints.Count + 1}" };
        TimePoints.Insert(index, newTimePoint);
        OnChanged();
        return newTimePoint;
    }

    /// <summary>
    /// 在指定时间点下方插入新时间点
    /// </summary>
    public TimePoint InsertTimePointBelow(TimePoint timePoint)
    {
        var index = TimePoints.IndexOf(timePoint);
        if (index < 0) index = TimePoints.Count;
        else index++;

        var newId = Guid.NewGuid().ToString("N")[..8];
        var newTimePoint = new TimePoint { Id = newId, Name = $"新时间点{TimePoints.Count + 1}" };
        TimePoints.Insert(index, newTimePoint);
        OnChanged();
        return newTimePoint;
    }

    /// <summary>
    /// 获取指定角色和时间点的单元格内容
    /// </summary>
    public string GetCellContent(string timePointId, string roleId)
    {
        var cell = Cells.FirstOrDefault(c => c.TimePointId == timePointId && c.RoleId == roleId);
        return cell?.Content ?? string.Empty;
    }

    /// <summary>
    /// 设置指定角色和时间点的单元格内容
    /// </summary>
    public void SetCellContent(string timePointId, string roleId, string content)
    {
        var cell = Cells.FirstOrDefault(c => c.TimePointId == timePointId && c.RoleId == roleId);
        if (cell != null)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                Cells.Remove(cell);
            }
            else
            {
                cell.Content = content;
            }
        }
        else if (!string.IsNullOrWhiteSpace(content))
        {
            Cells.Add(new CellContent
            {
                TimePointId = timePointId,
                RoleId = roleId,
                Content = content
            });
        }
        OnChanged();
    }

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        Roles.Clear();
        TimePoints.Clear();
        Cells.Clear();
        OnChanged();
    }

    /// <summary>
    /// 从文档数据加载
    /// </summary>
    public void LoadFromDocument(DocumentData document)
    {
        Roles.Clear();
        Roles.AddRange(document.Roles);

        TimePoints.Clear();
        TimePoints.AddRange(document.TimePoints);

        Cells.Clear();
        Cells.AddRange(document.Cells);

        OnChanged();
    }

    /// <summary>
    /// 构建文档数据
    /// </summary>
    public DocumentData BuildDocumentData(string title)
    {
        return new DocumentData
        {
            Title = title,
            Roles = [.. Roles],
            TimePoints = [.. TimePoints],
            Cells = [.. Cells.Where(c => !string.IsNullOrWhiteSpace(c.Content))]
        };
    }

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
