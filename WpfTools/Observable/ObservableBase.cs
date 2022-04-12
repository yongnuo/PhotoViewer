using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace WpfTools.Observable
{
	public abstract class ObservableBase<T> : IReadableObservable<T>
	{
		private T _value;
		private readonly Validator _validator;

		public event EventHandler ValueChanging = delegate { };
		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		public event EventHandler ValueChanged = delegate { };

		protected ObservableBase()
		{
			_validator = new Validator(this);
		}

		public bool IsValueChanging { get; private set; }

		T IReadableObservable<T>.Value
		{
			get { return GetValue(); }
		}

		public string this[string columnName]
		{
			get { return columnName != "Value" ? null : _validator.ErrorMessage; }
		}

		public string Error
		{
			get { return _validator.ErrorMessage; }
		}

		public bool HasErrors
		{
			get { return _validator.ErrorMessage != string.Empty; }
		}

		protected T GetValue()
		{
			return _value;
		}

		protected void SetValue(T value, ValueOrigin valueOrigin)
		{
			if (EqualityComparer<T>.Default.Equals(_value, value))
				return;

			try
			{
				IsValueChanging = true;
				ValueChanging(this, EventArgs.Empty);
				_value = value;
				OnValueAssigned(valueOrigin);
			}
			finally
			{
				IsValueChanging = false;
			}

			ValueChanged(this, EventArgs.Empty);
		}

		private void RaisePropertyChanged(string property)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		protected virtual void OnValueAssigned(ValueOrigin valueOrigin)
		{
			_validator.Validate();
			RaisePropertyChanged("Value");
		}

		public void AddValidation(Func<T, bool> predicate, string errorMessage)
		{
			_validator.AddRule(predicate, errorMessage);
		}

		public void AddValidationTrigger(INotifyPropertyChanged trigger)
		{
			_validator.AddTrigger(trigger);
		}

		public void RemoveValidationTrigger(INotifyPropertyChanged trigger)
		{
			_validator.RemoveTrigger(trigger);
		}

		public virtual void Dispose()
		{
			_validator.Dispose();
		}

		public static implicit operator T(ObservableBase<T> observable)
		{
			return observable.GetValue();
		}

		protected enum ValueOrigin
		{
			Internal,
			External
		}

		private class Validator
		{
			private ObservableBase<T> _observable;
			private readonly List<ValidationRule> _rules;

			public Validator(ObservableBase<T> observable)
			{
				_observable = observable;
				_rules = new List<ValidationRule>();
				ErrorMessage = string.Empty;
			}

			public string ErrorMessage { get; private set; }

			private void TriggerPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				Validate();
			}

			private void TriggerValueChanged(object sender, EventArgs eventArgs)
			{
				Validate();
			}

			public void Validate()
			{
				if (_observable == null) return;
				var errorMessages = _rules.Where(x => !x.Predicate.Invoke(_observable.GetValue())).Select(x => x.ErrorMessage);
				var errorMessage = string.Join(Environment.NewLine, errorMessages);
				if (errorMessage == ErrorMessage) return;
				ErrorMessage = errorMessage;
				_observable.RaisePropertyChanged("HasErrors");
				_observable.RaisePropertyChanged("Value");
			}

			public void AddTrigger(INotifyPropertyChanged trigger)
			{
				var observable = trigger as IReadableObservable;
				if (observable != null)
					observable.ValueChanged += TriggerValueChanged;
				else
					trigger.PropertyChanged += TriggerPropertyChanged;
			}

			public void RemoveTrigger(INotifyPropertyChanged trigger)
			{
				var observable = trigger as IReadableObservable;
				if (observable != null)
					observable.ValueChanged -= TriggerValueChanged;
				else
					trigger.PropertyChanged -= TriggerPropertyChanged;
			}

			public void Dispose()
			{
				_observable = null;
				_rules.Clear();
			}

			public void AddRule(Func<T, bool> predicate, string errorMessage)
			{
				if (predicate == null) throw new ArgumentNullException();
				if (string.IsNullOrWhiteSpace(errorMessage)) throw new ArgumentException();
				_rules.Add(new ValidationRule { Predicate = predicate, ErrorMessage = errorMessage });
				Validate();
			}

			private class ValidationRule
			{
				public Func<T, bool> Predicate { get; set; }
				public string ErrorMessage { get; set; }
			}
		}
	}

	public abstract class ObservableBase<TItem, TValue> : ObservableBase<TValue>
	{
		private readonly List<TItem> _items;
		private readonly Func<TItem, IEnumerable<INotifyPropertyChanged>> _observableProvider;
		private Func<IEnumerable<TItem>, TValue> _valueProvider;

		protected ObservableBase(IEnumerable<TItem> items, Func<TItem, IEnumerable<INotifyPropertyChanged>> observableProvider, Func<IEnumerable<TItem>, TValue> valueProvider)
		{
			_items = items.ToList();
			_observableProvider = observableProvider;
			_valueProvider = valueProvider;

			foreach (var observable in _items.SelectMany(x => _observableProvider(x)))
				observable.PropertyChanged += ItemOnPropertyChanged;

			EvaluateValue();
		}

		protected IEnumerable<TItem> Items
		{
			get { return _items; }
		}

		public override void Dispose()
		{
			RemoveItemsWithoutEvaluatingValue(Items.ToList());
			base.Dispose();
		}

		protected void EvaluateValue()
		{
			var value = _valueProvider(_items);
			SetValue(value, ValueOrigin.Internal);
		}

		private void ItemOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (IsValueChanging) return;
			EvaluateValue();
		}

		public void AddItem(TItem item)
		{
			AddItems(new[] { item });
		}

		public void AddItems(IEnumerable<TItem> items)
		{
			AddItemsWithoutEvaluatingValue(items);
			EvaluateValue();
		}

		public void RemoveItem(TItem item)
		{
			RemoveItems(new[] { item });
		}

		public void RemoveItems(IEnumerable<TItem> items)
		{
			RemoveItemsWithoutEvaluatingValue(items);
			EvaluateValue();
		}

		private void RemoveItemsWithoutEvaluatingValue(IEnumerable<TItem> items)
		{
			items.Where(x => _items.Remove(x))
				.SelectMany(x => _observableProvider(x))
				.ForEach(x => x.PropertyChanged -= ItemOnPropertyChanged);
		}

		protected void AddItemsWithoutEvaluatingValue(IEnumerable<TItem> items)
		{
			items = items.ToList();
			_items.AddRange(items);
			items.SelectMany(x => _observableProvider(x)).ForEach(x => x.PropertyChanged += ItemOnPropertyChanged);
		}

		public void RemoveAllItems()
		{
			RemoveItemsWithoutEvaluatingValue(Items.ToList());
			EvaluateValue();
		}

		public void Configure(TItem item)
		{
			Configure(new[] { item });
		}

		public void Configure(IEnumerable<TItem> items)
		{
			ConfigureWithoutEvaluatingValue(items);
			EvaluateValue();
		}

		protected void ConfigureWithoutEvaluatingValue(IEnumerable<TItem> items)
		{
			RemoveItemsWithoutEvaluatingValue(Items.ToList());
			AddItemsWithoutEvaluatingValue(items);
		}

		protected void ConfigureWithoutEvaluatingValue(Func<IEnumerable<TItem>, TValue> valueProvider)
		{
			_valueProvider = valueProvider;
		}

		public void AddValidation(Func<TValue, IEnumerable<TItem>, bool> predicate, string errorMessage)
		{
			AddValidation(v => predicate(v, Items), errorMessage);
		}
	}
}