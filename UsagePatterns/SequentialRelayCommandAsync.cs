using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AsyncWorkshop.UsagePatterns
{
    public class SequentialRelayCommandAsync : ICommand
    {
        private readonly Func<Task> _targetExecuteMethod;
        private readonly Func<bool> _targetCanExecuteMethod;

        private bool _currentlyExecuting;

        public SequentialRelayCommandAsync(Func<Task> executeMethod)
        {
            _targetExecuteMethod = executeMethod;
        }

        public SequentialRelayCommandAsync(Func<Task> executeMethod, Func<bool> canExecuteMethod)
        {
            _targetExecuteMethod = executeMethod;
            _targetCanExecuteMethod = canExecuteMethod;
        }

        bool ICommand.CanExecute(object parameter)
        {
            var couldExecute = _targetCanExecuteMethod?.Invoke() ?? _targetExecuteMethod != null;
            var canExecute = couldExecute && !_currentlyExecuting;

            return canExecute;
        }

        // Beware - should use weak references if command instance lifetime is longer than lifetime of UI objects that get hooked up to command
        // Prism commands solve this in their implementation
        public event EventHandler CanExecuteChanged = delegate { };

        async void ICommand.Execute(object parameter)
        {
            using (SequentialExecution())
            {
                await _targetExecuteMethod.Invoke();
            }
        }

        private IDisposable SequentialExecution()
        {
            _currentlyExecuting = true;
            CanExecuteChanged(this, EventArgs.Empty);

            return Disposable.Create(() =>
            {
                _currentlyExecuting = false;
                CanExecuteChanged(this, EventArgs.Empty);
            });
        }
    }
}