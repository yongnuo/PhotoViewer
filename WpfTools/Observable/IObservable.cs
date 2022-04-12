namespace WpfTools.Observable
{
	public interface IObservable<T> : IReadableObservable<T>
	{
		new T Value { get; set; }
	}
}