using System.Collections.Generic;

namespace Tabic.Models;

/// <summary>
/// 应用设置模型
/// </summary>
public class AppSettings
{
    /// <summary>
    /// OpenAI API 兼容的接口地址
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// API 密钥
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 自定义请求头（键值对）
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// 创建默认设置的副本
    /// </summary>
    public AppSettings Clone()
    {
        return new AppSettings
        {
            ApiUrl = ApiUrl,
            ApiKey = ApiKey,
            Model = Model,
            Headers = new Dictionary<string, string>(Headers)
        };
    }
}
