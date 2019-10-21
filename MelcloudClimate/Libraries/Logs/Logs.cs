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
	
	public class Log : Library, ILog
	{
		public new IHSApplication _hs;

		public Log(IHSApplication HS, IIniSettings iniSettings)
		{
			_hs = HS;
			_iniSettings = iniSettings;
		}



		public void Debug(string msg)
		{
			if (_iniSettings.LogLevel == LogLevel.Debug)
			{
				if (msg == null)
					msg = "";
				_hs.WriteLog(GetName() + " Debug", msg);
				Console.WriteLine($"Debug: {msg}");
			}
		}

		public void Info(string msg)
		{
			if (_iniSettings.LogLevel == LogLevel.Debug || _iniSettings.LogLevel == LogLevel.Normal)
			{
				if (msg == null)
					msg = "";

				_hs.WriteLog(GetName() + " Info", msg);

				Console.WriteLine($"Info: {msg}");
			}
		}

		public void Error(string msg)
		{
			if (msg == null)
				msg = "";

			_hs.WriteLog(GetName() + " Error", msg);

			Console.WriteLine($"Error: {msg}");
		}
	}
}