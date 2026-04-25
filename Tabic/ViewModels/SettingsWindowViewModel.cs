using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Tabic.Models;
using Tabic.Services;

namespace Tabic.ViewModels;

/// <summary>
/// 请求头条目视图模型
/// </summary>
public class HeaderItemViewModel : ViewModelBase
{
    private string _key = string.Empty;
    private string _value = string.Empty;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

/// <summary>
/// 设置窗口视图模型
/// </summary>
public class SettingsWindowViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private AppSettings _workingSettings = new();

    private string? _apiUrl;
    private string? _apiKey;
    private string? _model;

    /// <summary>
    /// API 接口地址
    /// </summary>
    public string? ApiUrl
    {
        get => _apiUrl;
        set
        {
            if (SetProperty(ref _apiUrl, value))
            {
                _workingSettings.ApiUrl = value;
            }
        }
    }

    /// <summary>
    /// API 密钥
    /// </summary>
    public string? ApiKey
    {
        get => _apiKey;
        set
        {
            if (SetProperty(ref _apiKey, value))
            {
                _workingSettings.ApiKey = value;
            }
        }
    }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string? Model
    {
        get => _model;
        set
        {
            if (SetProperty(ref _model, value))
            {
                _workingSettings.Model = value;
            }
        }
    }

    /// <summary>
    /// 自定义请求头列表
    /// </summary>
    public ObservableCollection<HeaderItemViewModel> Headers { get; } = new();

    /// <summary>
    /// 添加请求头命令
    /// </summary>
    public ICommand AddHeaderCommand { get; }

    /// <summary>
    /// 删除请求头命令
    /// </summary>
    public ICommand RemoveHeaderCommand { get; }

    /// <summary>
    /// 保存命令
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// 取消命令
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// 保存完成事件
    /// </summary>
    public event EventHandler? SaveCompleted;

    /// <summary>
    /// 取消事件
    /// </summary>
    public event EventHandler? Cancelled;

    public SettingsWindowViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;

        // 创建工作副本
        _workingSettings = settingsService.Settings.Clone();

        _apiUrl = _workingSettings.ApiUrl;
        _apiKey = _workingSettings.ApiKey;
        _model = _workingSettings.Model;

        foreach (var header in _workingSettings.Headers)
        {
            Headers.Add(new HeaderItemViewModel { Key = header.Key, Value = header.Value });
        }

        AddHeaderCommand = new RelayCommand(() => Headers.Add(new HeaderItemViewModel()));
        RemoveHeaderCommand = new RelayCommand<HeaderItemViewModel>(item =>
        {
            if (item != null) Headers.Remove(item);
        });

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
    }

    private async System.Threading.Tasks.Task SaveAsync()
    {
        _workingSettings.Headers = Headers
            .Where(h => !string.IsNullOrWhiteSpace(h.Key))
            .ToDictionary(h => h.Key, h => h.Value);

        await _settingsService.SaveSettingsAsync(_workingSettings);
        SaveCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
