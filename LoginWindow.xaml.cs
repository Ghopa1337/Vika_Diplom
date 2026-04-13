using CargoTransport.Desktop.ViewModels;
using System.Windows;

namespace CargoTransport.Desktop;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
