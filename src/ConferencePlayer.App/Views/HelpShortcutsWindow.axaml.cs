using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ConferencePlayer.Views;

public partial class HelpShortcutsWindow : Window
{
    public HelpShortcutsWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
