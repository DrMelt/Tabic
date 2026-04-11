using System.Collections.Generic;

namespace Tabic.Models;

/// <summary>
/// 表格行模型，包含时间点信息和该行所有单元格内容
/// </summary>
public class TableRow
{
    public TimePoint TimePoint { get; set; } = new TimePoint();
    public List<CellContent> Cells { get; set; } = [];
}
