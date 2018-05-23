using System;
using System.Reactive.Disposables;
using System.Windows.Input;

namespace AsyncWorkshop.UsagePatterns.Commands
{
    public class SequentialBoundRelayCommand : ICommand
    {
        private readonly Action _targetExecuteMethod;
        private readonly Func<bool> _targetCanExecuteMethod;

        private bool _currentlyExecuting;
        private bool _boundCommandExecuting;

        public SequentialBoundRelayCommand(ICommand boundCommand, Action executeMethod)
        {
            boundCommand.CanExecuteChanged += (sender, args) =>
            {
                _boundCommandExecuting = !_boundCommandExecuting;
                CanExecuteChanged(this, EventArgs.Empty);
            };

            _targetExecuteMethod = executeMethod;
        }

        bool ICommand.CanExecute(object parameter)
        {
            var couldExecute = _targetCanExecuteMethod?.Invoke() ?? _targetExecuteMethod != null;
            var canExecute = couldExecute && _boundCommandExecuting && !_currentlyExecuting;

            return canExecute;
        }

        // Beware - should use weak references if command instance lifetime is longer than lifetime of UI objects that get hooked up to command
        // Prism commands solve this in their implementation
        public event EventHandler CanExecuteChanged = delegate { };

        void ICommand.Execute(object parameter)
        {
            using (SequentialExecution())
            {
                _targetExecuteMethod.Invoke();
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