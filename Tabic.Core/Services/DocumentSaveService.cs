using System.Text.Json;
using System.Text.Json.Serialization;
using Tabic.Core.Models;

namespace Tabic.Core.Services;

/// <summary>
/// 文档存储服务
/// </summary>
public class DocumentSaveService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 文档扩展名
    /// </summary>
    public const string Extension = ".tabic";

    /// <summary>
    /// 保存文档为单一文件
    /// </summary>
    public static async Task SaveDocumentAsync(string documentPath, DocumentData data)
    {
        if (!documentPath.EndsWith(Extension))
            documentPath += Extension;

        // 确保目录存在
        var directory = Path.GetDirectoryName(documentPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // 创建完整的文档对象
        var document = new TabicDocument
        {
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Title = data.Title,
            Roles = data.Roles,
            TimePoints = data.TimePoints,
            Cells = data.Cells
        };

        var json = JsonSerializer.Serialize(document, JsonOptions);
        await File.WriteAllTextAsync(documentPath, json);
    }

    /// <summary>
    /// 加载单一文件文档
    /// </summary>
    public static async Task<DocumentData> LoadDocumentAsync(string documentPath)
    {
        if (!File.Exists(documentPath))
            throw new FileNotFoundException($"文档不存在: {documentPath}");

        var json = await File.ReadAllTextAsync(documentPath);
        var document = JsonSerializer.Deserialize<TabicDocument>(json, JsonOptions) ?? throw new InvalidOperationException("无法解析文档内容");

        return new DocumentData
        {
            Title = document.Title,
            Roles = document.Roles,
            TimePoints = document.TimePoints,
            Cells = document.Cells
        };
    }
}

/// <summary>
/// Tabic 单一文件文档格式
/// </summary>
public class TabicDocument
{
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<Role> Roles { get; set; } = [];
    public List<TimePoint> TimePoints { get; set; } = [];
    public List<CellContent> Cells { get; set; } = [];
}

/// <summary>
/// 文档数据
/// </summary>
public class DocumentData
{
    public string Title { get; set; } = string.Empty;
    public List<Role> Roles { get; set; } = [];
    public List<TimePoint> TimePoints { get; set; } = [];
    public List<CellContent> Cells { get; set; } = [];
}
