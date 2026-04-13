using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Tabic.Core.Models;

namespace Tabic.ViewModels;

/// <summary>
/// 表格行 ViewModel
/// </summary>
public class TableRowViewModel : ObservableObject
{
    public TimePoint TimePoint { get; set; } = new TimePoint();
    public ObservableCollection<CellViewModel> Cells { get; set; } = [];
}
