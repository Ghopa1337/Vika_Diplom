using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace CargoTransport.Desktop.ViewModels;

public delegate void ExecuteHandler(object? parameter);
public delegate bool CanExecuteHandler(object? parameter);
public delegate void ExecuteHandler<T>(T parameter);
public delegate bool CanExecuteHandler<T>(T parameter);

public class RelayCommand : ICommand
{
    private readonly CanExecuteHandler? _canExecute;
    private readonly ExecuteHandler _execute;
    private readonly EventHandler _requerySuggested;
    private readonly Dispatcher _dispatcher;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(ExecuteHandler execute, CanExecuteHandler? canExecute = null)
        : this()
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;

        _requerySuggested = (_, _) => Invalidate();
        CommandManager.RequerySuggested += _requerySuggested;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(
            _ => execute(),
            _ => canExecute?.Invoke() ?? true)
    {
    }

    private RelayCommand()
    {
        _execute = _ => { };
        _requerySuggested = (_, _) => { };
        _dispatcher = Application.Current.Dispatcher;
    }

    public virtual bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public virtual void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged()
    {
        if (_dispatcher.CheckAccess())
        {
            Invalidate();
            return;
        }

        _dispatcher.BeginInvoke((Action)Invalidate);
    }

    private void Invalidate() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class RelayCommand<T> : RelayCommand
{
    public RelayCommand(ExecuteHandler<T> execute, CanExecuteHandler<T>? canExecute = null)
        : base(
            parameter =>
            {
                if (TryGetParameter(parameter, out T value))
                {
                    execute(value);
                }
            },
            parameter => TryGetParameter(parameter, out T value) && (canExecute?.Invoke(value) ?? true))
    {
    }

    private static bool TryGetParameter(object? parameter, out T value)
    {
        if (parameter is T typedParameter)
        {
            value = typedParameter;
            return true;
        }

        if (parameter is null && default(T) is null)
        {
            value = default!;
            return true;
        }

        value = default!;
        return false;
    }
}
