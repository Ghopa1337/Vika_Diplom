using System.Windows;

namespace CargoTransport.Desktop.Services;

public interface IWindowService
{
    void ShowErrorMessage(string message, string caption);
    void ShowInfoMessage(string message, string caption);
    void Close(object viewModel, bool? dialogResult = null);
}

public class WindowService : IWindowService
{
    public void ShowErrorMessage(string message, string caption)
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowInfoMessage(string message, string caption)
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void Close(object viewModel, bool? dialogResult = null)
    {
        Window? window = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(x => ReferenceEquals(x.DataContext, viewModel));

        if (window is null)
        {
            return;
        }

        if (dialogResult.HasValue)
        {
            window.DialogResult = dialogResult.Value;
        }
        else
        {
            window.Close();
        }
    }
}
