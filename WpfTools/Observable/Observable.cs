using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WpfTools.Observable
{
	public class Observable<T> : ObservableBase<T>, IObservable<T>
	{
		public Observable()
			: this(default(T))
		{
		}

		public Observable(T value)
		{
			SetValue(value, ValueOrigin.Internal);
		}

		public T Value
		{
			get { return GetValue(); }
			set { SetValue(value, ValueOrigin.External); }
		}

		public static Observable<TItem, T> FromItems<TItem>(IEnumerable<TItem> items, Func<IEnumerable<TItem>, T> getValue, Action<IEnumerable<TItem>, T> setValue)
			where TItem : INotifyPropertyChanged
		{
			return new Observable<TItem, T>(items, x => new INotifyPropertyChanged[] { x }, getValue, setValue);
		}

		public static Observable<TItem, T> FromItems<TItem>(IEnumerable<TItem> items, Func<TItem, IEnumerable<INotifyPropertyChanged>> valueTriggerProvider, Func<IEnumerable<TItem>, T> getValue, Action<IEnumerable<TItem>, T> setValue)
		{
			return new Observable<TItem, T>(items, valueTriggerProvider, getValue, setValue);
		}
	}

	public class Observable<TItem, TValue> : ObservableBase<TItem, TValue>, IObservable<TValue>
	{
		private Action<IEnumerable<TItem>, TValue> _setValue;

		public Observable(IEnumerable<TItem> items, Func<TItem, IEnumerable<INotifyPropertyChanged>> observableProvider, Func<IEnumerable<TItem>, TValue> valueProvider, Action<IEnumerable<TItem>, TValue> setValue)
			: base(items, observableProvider, valueProvider)
		{
			_setValue = setValue;
		}

		public TValue Value
		{
			get { return GetValue(); }
			set { SetValue(value, ValueOrigin.External); }
		}

		protected override void OnValueAssigned(ValueOrigin valueOrigin)
		{
			base.OnValueAssigned(valueOrigin);
			if (valueOrigin == ValueOrigin.External) _setValue(Items, Value);
		}

		public void Configure(IEnumerable<TItem> items, Func<IEnumerable<TItem>, TValue> valueProvider,
									 Action<IEnumerable<TItem>, TValue> setValue)
		{
			ConfigureWithoutEvaluatingValue(items);
			ConfigureWithoutEvaluatingValue(valueProvider);
			_setValue = setValue;
			EvaluateValue();
		}
	}
}