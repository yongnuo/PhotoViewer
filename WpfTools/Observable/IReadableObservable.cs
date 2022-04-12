using System;
using System.ComponentModel;

namespace WpfTools.Observable
{
	public interface IReadableObservable : INotifyPropertyChanged, IDataErrorInfo, IDisposable
	{
		bool IsValueChanging { get; }
		event EventHandler ValueChanging;
		event EventHandler ValueChanged;
	}

	public interface IReadableObservable<T> : IReadableObservable
	{
		T Value { get; }
	}
}