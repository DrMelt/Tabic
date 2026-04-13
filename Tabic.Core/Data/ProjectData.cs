using Tabic.Core.Models;

namespace Tabic.Core.Data;

/// <summary>
/// 项目数据（用于序列化）
/// </summary>
public class ProjectData
{
    public List<Role> Roles { get; set; } = [];
    public List<RowData> Rows { get; set; } = [];
}

/// <summary>
/// 行数据（用于序列化）
/// </summary>
public class RowData
{
    public TimePoint TimePoint { get; set; } = new TimePoint();
    public List<CellContent> Cells { get; set; } = [];
}
