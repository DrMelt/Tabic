using System.Text.Json;
using ModelContextProtocol.Server;
using Tabic.Core.Models;
using Tabic.Core.Services;

namespace Tabic.Mcp;

/// <summary>
/// Tabic MCP 工具集，供 LLM 客户端调用。
/// </summary>
[McpServerToolType]
public sealed class TabTools
{
    private readonly TimelineData _data;

    public TabTools(TimelineData data)
    {
        _data = data;
    }

    // === 角色管理 ===

    [McpServerTool(Name = "list_roles")]
    public string ListRoles()
    {
        var roles = _data.Roles.Select(r => new { r.Id, r.Name });
        return JsonSerializer.Serialize(roles);
    }

    [McpServerTool(Name = "add_role")]
    public string AddRole()
    {
        var role = _data.AddRole();
        return JsonSerializer.Serialize(new { role.Id, role.Name });
    }

    [McpServerTool(Name = "remove_role")]
    public string RemoveRole(string roleId)
    {
        var role = _data.Roles.FirstOrDefault(r => r.Id == roleId);
        if (role == null)
            return $"未找到角色: {roleId}";
        _data.RemoveRole(role);
        return $"已删除角色: {role.Name} ({roleId})";
    }

    // === 时间点管理 ===

    [McpServerTool(Name = "list_timepoints")]
    public string ListTimePoints()
    {
        var tps = _data.TimePoints.Select(t => new { t.Id, t.Name });
        return JsonSerializer.Serialize(tps);
    }

    [McpServerTool(Name = "add_timepoint")]
    public string AddTimePoint()
    {
        var tp = _data.AddTimePoint();
        return JsonSerializer.Serialize(new { tp.Id, tp.Name });
    }

    [McpServerTool(Name = "remove_timepoint")]
    public string RemoveTimePoint(string timePointId)
    {
        var tp = _data.TimePoints.FirstOrDefault(t => t.Id == timePointId);
        if (tp == null)
            return $"未找到时间点: {timePointId}";
        _data.RemoveTimePoint(tp);
        return $"已删除时间点: {tp.Name} ({timePointId})";
    }

    [McpServerTool(Name = "insert_timepoint_above")]
    public string InsertTimePointAbove(string timePointId)
    {
        var tp = _data.TimePoints.FirstOrDefault(t => t.Id == timePointId);
        if (tp == null)
            return $"未找到时间点: {timePointId}";
        var newTp = _data.InsertTimePointAbove(tp);
        return JsonSerializer.Serialize(new { newTp.Id, newTp.Name });
    }

    [McpServerTool(Name = "insert_timepoint_below")]
    public string InsertTimePointBelow(string timePointId)
    {
        var tp = _data.TimePoints.FirstOrDefault(t => t.Id == timePointId);
        if (tp == null)
            return $"未找到时间点: {timePointId}";
        var newTp = _data.InsertTimePointBelow(tp);
        return JsonSerializer.Serialize(new { newTp.Id, newTp.Name });
    }

    // === 单元格内容 ===

    [McpServerTool(Name = "get_cell_content")]
    public string GetCellContent(string timePointId, string roleId)
    {
        return _data.GetCellContent(timePointId, roleId);
    }

    [McpServerTool(Name = "set_cell_content")]
    public string SetCellContent(string timePointId, string roleId, string content)
    {
        _data.SetCellContent(timePointId, roleId, content);
        return $"已设置单元格内容: timePoint={timePointId}, role={roleId}";
    }

    [McpServerTool(Name = "list_cells")]
    public string ListCells()
    {
        var cells = _data.Cells
            .Where(c => !string.IsNullOrWhiteSpace(c.Content))
            .Select(c => new { c.TimePointId, c.RoleId, c.Content });
        return JsonSerializer.Serialize(cells);
    }

    // === 文档操作 ===

    [McpServerTool(Name = "load_document")]
    public async Task<string> LoadDocument(string filePath)
    {
        if (!File.Exists(filePath))
            return $"文件不存在: {filePath}";
        var document = await DocumentSaveService.LoadDocumentAsync(filePath);
        _data.LoadFromDocument(document);
        return $"已从 {filePath} 加载文档: {document.Title}";
    }

    [McpServerTool(Name = "save_document")]
    public async Task<string> SaveDocument(string filePath, string? title = null)
    {
        var docTitle = title ?? $"未命名_{DateTime.Now:yyyyMMdd_HHmmss}";
        var doc = _data.BuildDocumentData(docTitle);
        await DocumentSaveService.SaveDocumentAsync(filePath, doc);
        return $"已保存文档到: {filePath}";
    }

    [McpServerTool(Name = "clear_data")]
    public string ClearData()
    {
        _data.Clear();
        return "已清空所有数据";
    }

    [McpServerTool(Name = "get_summary")]
    public string GetSummary()
    {
        var summary = new
        {
            RoleCount = _data.Roles.Count,
            Roles = _data.Roles.Select(r => new { r.Id, r.Name }),
            TimePointCount = _data.TimePoints.Count,
            TimePoints = _data.TimePoints.Select(t => new { t.Id, t.Name }),
            CellCount = _data.Cells.Count(c => !string.IsNullOrWhiteSpace(c.Content)),
            TotalCells = _data.Cells.Count
        };
        return JsonSerializer.Serialize(summary);
    }
}