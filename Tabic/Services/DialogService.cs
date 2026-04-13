using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;

namespace Tabic.Services;

/// <summary>
/// 确认对话框结果
/// </summary>
public enum ConfirmResult
{
    Yes,
    No,
    Cancel
}

/// <summary>
/// 对话框服务
/// </summary>
public class DialogService
{
    /// <summary>
    /// 显示错误对话框
    /// </summary>
    public async Task ShowErrorAsync(string title, string message)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
            && lifetime.MainWindow != null)
        {
            var msgBox = new Window
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
                HorizontalAlignment = HorizontalAlignment.Center,
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
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    okButton
                }
            };

            await msgBox.ShowDialog(lifetime.MainWindow);
        }
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public async Task<ConfirmResult> ShowConfirmAsync(
        string title,
        string message,
        string yesText,
        string noText,
        string cancelText)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime
            || lifetime.MainWindow == null)
        {
            return ConfirmResult.Cancel;
        }

        var tcs = new TaskCompletionSource<ConfirmResult>();

        var msgBox = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var yesButton = new Button { Content = yesText, Margin = new Thickness(5) };
        var noButton = new Button { Content = noText, Margin = new Thickness(5) };
        var cancelButton = new Button { Content = cancelText, Margin = new Thickness(5) };

        yesButton.Click += (s, e) => { tcs.SetResult(ConfirmResult.Yes); msgBox.Close(); };
        noButton.Click += (s, e) => { tcs.SetResult(ConfirmResult.No); msgBox.Close(); };
        cancelButton.Click += (s, e) => { tcs.SetResult(ConfirmResult.Cancel); msgBox.Close(); };

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
                    VerticalAlignment = VerticalAlignment.Center
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    [Grid.RowProperty] = 1,
                    Children = { yesButton, noButton, cancelButton }
                }
            }
        };

        msgBox.Closed += (s, e) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(ConfirmResult.Cancel);
        };

        await msgBox.ShowDialog(lifetime.MainWindow);
        return await tcs.Task;
    }
}
