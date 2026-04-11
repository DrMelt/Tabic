using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Tabic.ViewModels;

/// <summary>
/// 单元格 ViewModel
/// </summary>
public class CellViewModel : ObservableObject
{
    private string _content = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                ContentChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 内容变更事件
    /// </summary>
    public event EventHandler? ContentChanged;
}
