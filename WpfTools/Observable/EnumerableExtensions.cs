using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfTools.Observable
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T item)
		{
			foreach (var t in source) yield return t;
			yield return item;
		}

		public static IEnumerable<T> DefaultIfEmpty<T>(this IEnumerable<T> source, Func<T> factory)
		{
			return source.DefaultIfEmpty(() => new[] { factory() });
		}

		public static IEnumerable<T> DefaultIfEmpty<T>(this IEnumerable<T> source, IEnumerable<T> items)
		{
			return source.DefaultIfEmpty(() => items);
		}

		private static IEnumerable<T> DefaultIfEmpty<T>(this IEnumerable<T> source, Func<IEnumerable<T>> factory)
		{
			var breakOut = false;

			foreach (var item in source)
			{
				breakOut = true;
				yield return item;
			}

			if (breakOut)
				yield break;

			foreach (var item in factory())
				yield return item;
		}

		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
		{
			return source ?? Enumerable.Empty<T>();
		}

		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var item in source) action(item);
		}

		public static bool IsEmpty<T>(this IEnumerable<T> source)
		{
			return !source.Any();
		}

		public static IOrderedEnumerable<T> OrderBy<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector, Comparison<TValue> comparison)
		{
			return source.OrderBy(valueSelector, new Comparer<TValue>(comparison));
		}

		public static IOrderedEnumerable<T> OrderByDescending<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector, Comparison<TValue> comparison)
		{
			return source.OrderByDescending(valueSelector, new Comparer<TValue>(comparison));
		}

		public static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, T item)
		{
			yield return item;
			foreach (var t in source) yield return t;
		}

		public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
		{
			return source.SkipLast(1);
		}

		public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
		{
			var list = source.ToList();
			for (var i = 0; i < list.Count - count; i++)
				yield return list[i];
		}

		public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
		{
			var list = new List<T>(count + 1);
			foreach (var item in source)
			{
				list.Add(item);
				if (list.Count > count)
					list.RemoveAt(0);
			}
			return list;
		}

		public static IEnumerable<T> ToEnumerable<T>(this IEnumerable<T> source)
		{
			return source;
		}

		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
		{
			return new HashSet<T>(source);
		}

		public static IEnumerable<T> Visit<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var item in source)
			{
				action(item);
				yield return item;
			}
		}

		public static IEnumerable<IEnumerable<Guid>> Split(this IEnumerable<Guid> listToSplit, int count)
		{
			var numerOfIds = 0;
			var returnList = new List<Guid>();
			foreach (var item in listToSplit)
			{
				returnList.Add(item);
				numerOfIds++;
				if (numerOfIds != count) continue;
				yield return new List<Guid>(returnList);
				numerOfIds = 0;
				returnList = new List<Guid>();
			}
			if (numerOfIds > 0)
			{
				yield return new List<Guid>(returnList);
			}
		}

		private class Comparer<T> : IComparer<T>
		{
			private readonly Comparison<T> _comparison;

			public Comparer(Comparison<T> comparison)
			{
				_comparison = comparison;
			}

			public int Compare(T x, T y)
			{
				return _comparison(x, y);
			}
		}
	}
}