using HomeSeerAPI;
using System;
using HSPI_MelcloudClimate.Common;

namespace HSPI_MelcloudClimate.Libraries.Logs
{
	public interface ILog
	{
		void Debug(string msg);
		void Info(string msg);
		void Error(string msg);
		}
	
	public class Log : ILog
	{
		private IHSApplication _hs;
		private IIniSettings _iniSettings;

		public Log(IHSApplication hs, IIniSettings iniSettings)
		{
			_hs = hs;
			_iniSettings = iniSettings;
		}



		public void Debug(string msg)
		{
			if (_iniSettings.LogLevel == LogLevel.Debug)
			{
				if (msg == null)
					msg = "";
				_hs.WriteLog(Utility.PluginName + " Debug", msg);
				Console.WriteLine($"Debug: {msg}");
			}
		}

		public void Info(string msg)
		{
			if (_iniSettings.LogLevel == LogLevel.Debug || _iniSettings.LogLevel == LogLevel.Normal)
			{
				if (msg == null)
					msg = "";

				_hs.WriteLog(Utility.PluginName + " Info", msg);

				Console.WriteLine($"Info: {msg}");
			}
		}

		public void Error(string msg)
		{
			if (msg == null)
				msg = "";

			_hs.WriteLog(Utility.PluginName + " Error", msg);

			Console.WriteLine($"Error: {msg}");
		}
	}
}