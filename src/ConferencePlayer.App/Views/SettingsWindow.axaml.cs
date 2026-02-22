using Avalonia.Controls;
using ConferencePlayer.ViewModels;

namespace ConferencePlayer.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.RequestClose += (_, __) => Close();
            }
        };
    }
}
