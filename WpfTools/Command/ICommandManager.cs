using System;

namespace WpfTools.Command
{
	public interface ICommandManager
	{
		event EventHandler RequerySuggested;
		void InvalidateRequerySuggested();
	}
}