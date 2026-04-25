using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using SukiUI.Controls;
using Tabic.Core.Models;
using Tabic.Core.Services;

namespace Tabic.Services;

/// <summary>
/// 项目文件操作服务，封装新建、打开、保存等文档状态管理
/// </summary>
public class ProjectService
{
    private string? _currentDocumentPath;
    private bool _hasUnsavedChanges;

    /// <summary>
    /// 当前文档路径
    /// </summary>
    public string? CurrentDocumentPath => _currentDocumentPath;

    /// <summary>
    /// 是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges => _hasUnsavedChanges;

    /// <summary>
    /// 未保存状态变更事件
    /// </summary>
    public event EventHandler? UnsavedStateChanged;

    /// <summary>
    /// 文档路径变更事件
    /// </summary>
    public event EventHandler? DocumentPathChanged;

    /// <summary>
    /// 获取窗口标题
    /// </summary>
    public string GetWindowTitle()
    {
        var fileName = string.IsNullOrEmpty(_currentDocumentPath)
            ? "未命名项目"
            : Path.GetFileNameWithoutExtension(_currentDocumentPath);
        var unsavedMarker = _hasUnsavedChanges ? "* " : "";
        return $"{unsavedMarker}{fileName} - Tabic";
    }

    /// <summary>
    /// 标记有未保存的更改
    /// </summary>
    public void MarkAsUnsaved()
    {
        if (!_hasUnsavedChanges)
        {
            _hasUnsavedChanges = true;
            UnsavedStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 新建项目
    /// </summary>
    public void NewProject()
    {
        _currentDocumentPath = null;
        _hasUnsavedChanges = false;
        DocumentPathChanged?.Invoke(this, EventArgs.Empty);
        UnsavedStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 打开项目
    /// </summary>
    public async Task<DocumentData?> OpenProjectAsync()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "打开项目文件",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Tabic 项目文件")
                {
                    Patterns = ["*.tabic"]
                }
            ]
        };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
            && lifetime.MainWindow != null)
        {
            var result = await lifetime.MainWindow.StorageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0)
            {
                var filePath = result[0].Path.LocalPath;
                try
                {
                    var document = await DocumentSaveService.LoadDocumentAsync(filePath);
                    _currentDocumentPath = filePath;
                    _hasUnsavedChanges = false;
                    DocumentPathChanged?.Invoke(this, EventArgs.Empty);
                    UnsavedStateChanged?.Invoke(this, EventArgs.Empty);
                    return document;
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync("打开项目失败", ex.Message);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 保存项目（首次保存会弹出对话框）
    /// </summary>
    public async Task<bool> SaveProjectAsync(DocumentData document)
    {
        var documentPath = _currentDocumentPath;

        if (string.IsNullOrEmpty(documentPath))
        {
            documentPath = await PickSavePathAsync();
            if (string.IsNullOrEmpty(documentPath))
            {
                return false;
            }
        }

        await DocumentSaveService.SaveDocumentAsync(documentPath, document);
        _currentDocumentPath = documentPath;
        _hasUnsavedChanges = false;
        DocumentPathChanged?.Invoke(this, EventArgs.Empty);
        UnsavedStateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>
    /// 选择保存路径
    /// </summary>
    public async Task<string?> PickSavePathAsync()
    {
        var options = new FilePickerSaveOptions
        {
            Title = "保存项目",
            DefaultExtension = DocumentSaveService.Extension,
            SuggestedFileName = "未命名项目",
            FileTypeChoices =
            [
                new FilePickerFileType("Tabic 项目文件")
                {
                    Patterns = ["*.tabic"]
                }
            ]
        };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
            && lifetime.MainWindow != null)
        {
            var result = await lifetime.MainWindow.StorageProvider.SaveFilePickerAsync(options);
            return result?.Path.LocalPath;
        }

        return null;
    }

    /// <summary>
    /// 从 TimelineData 构建 DocumentData
    /// </summary>
    public DocumentData BuildDocumentData(TimelineData timelineData)
    {
        var documentPath = _currentDocumentPath ?? "未命名项目";
        return timelineData.BuildDocumentData(Path.GetFileNameWithoutExtension(documentPath));
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    private static async Task ShowErrorDialogAsync(string title, string message)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
            && lifetime.MainWindow != null)
        {
            var msgBox = new SukiWindow
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var okButton = new Button
            {
                Content = "确定",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                [Grid.RowProperty] = 1
            };
            okButton.Click += (s, e) => msgBox.Close();

            msgBox.Content = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
                Margin = new Thickness(20),
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    },
                    okButton
                }
            };

            await msgBox.ShowDialog(lifetime.MainWindow);
        }
    }
}
