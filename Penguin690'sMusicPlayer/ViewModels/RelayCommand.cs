using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Penguin690_sMusicPlayer.ViewModels;

internal class RelayCommand : ICommand
{
    private readonly Action _action;
    private readonly Func<bool> _canExecute;

    public RelayCommand(Action action)
    {
        _action = action;
        _canExecute = () => true;
    }
    public RelayCommand(Action action, Func<bool> canExecute)
    {
        _action = action;
        _canExecute = canExecute;
    }
    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        return _canExecute();
    }

    public void Execute(object parameter)
    {
        _action();
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

internal class RelayCommand<T> : ICommand where T : class 
{
    private readonly Action<T> _action;
    private readonly Func<T, bool> _canExecute;

    public RelayCommand(Action<T> action)
    {
        _action = action;
    }
    public RelayCommand(Action<T> action, Func<T, bool> canExecute)
    {
        _action = action;
        _canExecute = canExecute;
    }
    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        return  _canExecute == null || _canExecute(parameter as T);
    }

    public void Execute(object parameter)
    {
        _action(parameter as T);
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}