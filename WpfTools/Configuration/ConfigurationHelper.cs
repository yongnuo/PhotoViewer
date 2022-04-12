using System.Configuration;

namespace WpfTools.Configuration
{
	public static class ConfigurationHelper
	{
		public static int GetIntFromConfig(string key, int defaultValue)
		{
			var valueString = ConfigurationManager.AppSettings[key];
			int value;
			if (!int.TryParse(valueString, out value))
				value = defaultValue;
			return value;
		}

		public static bool GetBoolFromConfig(string key, bool defaultValue)
		{
			var valueString = ConfigurationManager.AppSettings[key];
			bool value;
			if (!bool.TryParse(valueString, out value))
				value = defaultValue;
			return value;
		}
		public static string GetStringFromConfig(string key, string defaultValue)
		{
			var valueString = ConfigurationManager.AppSettings[key];
			if (string.IsNullOrWhiteSpace(valueString))
				valueString = defaultValue;
			return valueString;
		}
	}
}