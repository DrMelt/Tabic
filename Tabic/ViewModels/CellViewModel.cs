using CommunityToolkit.Mvvm.ComponentModel;

namespace Tabic.ViewModels;

/// <summary>
/// 单元格 ViewModel
/// </summary>
public class CellViewModel : ObservableObject
{
    private string _content = string.Empty;

    public string RoleId { get; set; } = string.Empty;
    public string TimePointId { get; set; } = string.Empty;

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }
}
