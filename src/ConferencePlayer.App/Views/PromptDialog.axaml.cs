using Avalonia.Controls;
using ConferencePlayer.Services;

namespace ConferencePlayer.Views;

public partial class PromptDialog : Window
{
    public sealed class Model
    {
        public string Message { get; set; } = "";
        public string Details { get; set; } = "";
    }

    public PromptDialog()
    {
        InitializeComponent();
        DataContext = new Model();
    }

    public PromptDialog(string message, string? details)
    {
        InitializeComponent();
        DataContext = new Model
        {
            Message = message,
            Details = details ?? ""
        };
    }

    private void Retry_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(UserChoice.Retry);
    private void Skip_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(UserChoice.Skip);
    private void Stop_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(UserChoice.Stop);
}
