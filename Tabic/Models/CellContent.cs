namespace Tabic.Models;

/// <summary>
/// 单元格内容模型（离散存储）
/// </summary>
public class CellContent
{
    /// <summary>
    /// 时间点ID
    /// </summary>
    public string TimePointId { get; set; } = string.Empty;

    /// <summary>
    /// 角色ID
    /// </summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
