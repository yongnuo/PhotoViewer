using System;
using System.Diagnostics;
using System.Windows.Input;
using WpfTools.Observable;

namespace WpfTools.Command
{
	/// <summary>
	/// A command whose sole purpose is to 
	/// relay its functionality to other
	/// objects by invoking delegates. The
	/// default return value for the CanExecute
	/// method is 'true'.
	/// </summary>
	public class RelayCommand : RelayCommand<object>
	{
		public RelayCommand(Action execute, Func<bool> canExecute = null)
			: this(_ => execute(), canExecute == null ? default(Predicate<object>) : _ => canExecute())
		{
		}

		public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
			: base(execute, canExecute)
		{
		}

		public RelayCommand(Action execute, IReadableObservable<bool> observable)
			: base(_ => execute(), observable)
		{
		}
	}

	public class RelayCommand<T> : ICommand
	{
		readonly Action<T> _execute;
		readonly Predicate<T> _canExecute;

		/// <summary>
		/// Creates a new command.
		/// </summary>
		/// <param name="execute">The execution logic.</param>
		/// <param name="canExecute">The execution status logic.</param>
		public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
		{
			if (execute == null)
				throw new ArgumentNullException("execute");

			_execute = execute;
			_canExecute = canExecute;
			CommandManager = WpfCommandManager.Instance;
		}

		public RelayCommand(Action<T> execute, IReadableObservable<bool> observable)
			: this(execute, _ => observable.Value)
		{
			observable.PropertyChanged += delegate { CommandManager.InvalidateRequerySuggested(); };
		}

		[DebuggerStepThrough]
		public bool CanExecute(object parameter)
		{
			return _canExecute == null || _canExecute((T)parameter);
		}

		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public void Execute(object parameter)
		{
			_execute((T)parameter);
		}

		public ICommandManager CommandManager { get; set; }

		private class WpfCommandManager : ICommandManager
		{
			private WpfCommandManager() { }

			static WpfCommandManager()
			{
				Instance = new WpfCommandManager();
			}

			// ReSharper disable once StaticFieldInGenericType
			public static readonly WpfCommandManager Instance;

			public event EventHandler RequerySuggested
			{
				add { System.Windows.Input.CommandManager.RequerySuggested += value; }
				remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
			}

			public void InvalidateRequerySuggested()
			{
				System.Windows.Input.CommandManager.InvalidateRequerySuggested();
			}
		}
	}
}
