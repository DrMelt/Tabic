using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Tabic.Models;

namespace Tabic.Services;

/// <summary>
/// 设置服务，负责读取和保存应用设置到执行文件相对路径
/// </summary>
public class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings _settings = new();

    /// <summary>
    /// 当前应用设置
    /// </summary>
    public AppSettings Settings => _settings;

    /// <summary>
    /// 设置变更事件
    /// </summary>
    public event EventHandler? SettingsChanged;

    public SettingsService()
    {
        var baseDir = AppContext.BaseDirectory;
        _settingsFilePath = Path.Combine(baseDir, "settings.json");
    }

    /// <summary>
    /// 加载设置（同步版本，用于启动时调用避免死锁）
    /// </summary>
    public void LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            _settings = new AppSettings();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, GetJsonOptions());
            _settings = loaded ?? new AppSettings();
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _settings = settings;
        var json = JsonSerializer.Serialize(settings, GetJsonOptions());
        await File.WriteAllTextAsync(_settingsFilePath, json);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
