using CargoTransport.Desktop.ViewModels;
using System.Windows;

namespace CargoTransport.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
