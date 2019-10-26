using System.IO;

namespace HSPI_MelcloudClimate.Common
{
	public class Utility
	{

		public const string IniFile = "MelcloudClimate.ini";
		public static string PluginName => "MelcloudClimate";
		public static string InstanceFriendlyName => string.Empty;
		public static string ExePath = Directory.GetCurrentDirectory();
	}
}