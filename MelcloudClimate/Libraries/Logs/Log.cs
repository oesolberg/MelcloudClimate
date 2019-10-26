using System;
using System.IO;
using HomeSeerAPI;
using HSPI_MelcloudClimate.Common;
using Scheduler;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace HSPI_MelcloudClimate.Libraries.Logs
{
	public interface ILog
	{
		void Debug(string msg);
		void Info(string msg);
		void Error(string msg);
		void Warn(string msg);
		void Exception(Exception ex);
		void ToFileIfLogToFileEnabled(string logMessage,LogType logType=LogType.Debug);
		void Dispose();
	}

	public class Log : ILog, IDisposable
	{
		private const string OrangeColor = "#FFA500";
		private const string RedColor = "#FF0000";
		private static object _lockObject = new object();
		private readonly IHSApplication _hs;
		private readonly IIniSettings _iniSettings;
		private bool _disposed;
		private Logger _seriLogger = null;

		public Log(IHSApplication hs, IIniSettings iniSettings)
		{
			_hs = hs;
			_iniSettings = iniSettings;
			_iniSettings.IniSettingsChangedForLogLevel += OnIniSettingsChangedForLogLevel;

			if (_iniSettings.LogLevel == LogLevel.DebugToFile || _iniSettings.LogLevel == LogLevel.DebugToFileAndLog)
			{
				CreateLogFile();
			}
		}

		private void CreateLogFile()
		{
			if (_seriLogger != null) return;
			var logPath = Path.Combine(Utility.ExePath, "Logs");
			if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
			var logFile = Path.Combine(logPath, "GCalSeerDebug.log");
			lock (_lockObject)
			{
				_seriLogger = new LoggerConfiguration()
					.MinimumLevel.Debug()
					.WriteTo.File(path: logFile, rollingInterval: RollingInterval.Day, shared: true)
					.CreateLogger();
			}
		}

		private void CloseLogFile()
		{
			if (_seriLogger != null)
			{
				lock (_lockObject)
				{
					_seriLogger.Dispose();
					_seriLogger = null;
				}
			}
		}

		private void OnIniSettingsChangedForLogLevel(object sender, EventArgs eventArgs)
		{
			if (_iniSettings.LogLevel != LogLevel.DebugToFileAndLog && _iniSettings.LogLevel != LogLevel.DebugToFile)
			{
				CloseLogFile();
			}

			if (_iniSettings.LogLevel == LogLevel.DebugToFileAndLog || _iniSettings.LogLevel == LogLevel.DebugToFile)
			{
				CreateLogFile();
			}
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
			ToFileIfLogToFileEnabled($"Debug: {msg}");
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
			ToFileIfLogToFileEnabled($"Info: {msg}",LogType.Info);
		}

		public void Exception(Exception ex)
		{
			if (ex != null)
			{
				Error("Exception: " + ex.Message);
				Error(ex.StackTrace);
			}

			ToFileIfLogToFileEnabled("Exception: " + ex.Message,LogType.Error);
			ToFileIfLogToFileEnabled(ex.StackTrace, LogType.Error);
		}

		public void ToFileIfLogToFileEnabled(string msg, LogType logType = LogType.Debug)
		{
			if (_seriLogger != null &&
				(_iniSettings.LogLevel == LogLevel.DebugToFile || _iniSettings.LogLevel == LogLevel.DebugToFileAndLog))
			{
				lock (_lockObject)
				{
					switch (logType)
					{
						case LogType.Debug:
							_seriLogger.Debug($"{msg}");
							break;
						case LogType.Info:
							_seriLogger.Information($"{msg}");
							break;
						case LogType.Warn:
							_seriLogger.Warning($"{msg}");
							break;
						case LogType.Error:
							_seriLogger.Error($"{msg}");
							break;
						case LogType.Fatal:
							_seriLogger.Fatal($"{msg}");
							break;
						default: _seriLogger.Debug(msg); break;
					}
					Serilog.Log.CloseAndFlush();
				}
			}
		}

		public void Error(string msg)
		{
			if (msg == null)
				msg = "";

			_hs.WriteLogEx(Utility.PluginName + "-Error", msg, RedColor);

			ToFileIfLogToFileEnabled(msg,LogType.Error);
			

			Console.WriteLine($"Error: {msg}");
		}

		public void Warn(string msg)
		{
			_hs.WriteLogEx(Utility.PluginName + "-Warn", msg, OrangeColor);
			ToFileIfLogToFileEnabled(msg, LogType.Warn);
		}

		public void Dispose()
		{
			Dispose(true);
			// Use SupressFinalize in case a subclass 
			// of this type implements a finalizer.
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				_iniSettings.IniSettingsChangedForLogLevel -= OnIniSettingsChangedForLogLevel;
				// Indicate that the instance has been disposed.
				_disposed = true;
			}
		}
	}

	public enum LogType
	{
		Debug,
		Info,
		Warn,
		Error,
		Fatal
	}
}