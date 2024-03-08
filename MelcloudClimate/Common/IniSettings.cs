using System;
using System.Collections.Generic;
using System.Linq;
using HomeSeerAPI;
using HSPI_MelcloudClimate.Common;


namespace HSPI_MelcloudClimate.Common
{
	public interface IIniSettings
	{
		void LoadSettingsFromIniFile();

		LogLevel LogLevel { get; set; }
		int CheckMelCloudTimerInterval { get; set; }

		string UserNameMelCloud { get; set; }
		string PasswordMelCloud { get; set; }
		double CheckMelCloudTimerIntervalInMilliseconds { get; }

		event IniSettingsChangedForUserNamePasswordEventHandler IniSettingsChangedForUserNamePassword;
		event IniSettingsChangedForCheckIntervalEventHandler IniSettingsChangedForCheckInterval;
		event IniSettingsChangedForLogLevelEventHandler IniSettingsChangedForLogLevel;

		bool PasswordAndUsernameOk();
	}

	public class IniSettings : IIniSettings, IDisposable
	{
		private readonly string UserSection = "User";
		public const string UserNameKey = "Username";
		public const string PasswordKey = "Password";

		private readonly string ConfigSection = "Config";
		public const string LogLevelKey = "LogLevel";
		public const string CheckMelCloudIntervalKey = "CheckMelcloudInterval";
		public const string TestFileLocationKey = "TestFile";
		public const string UseTestFileKey = "UseTestFile";
		private readonly IHSApplication _hs;

		private bool _disposed;
		private LogLevel _logLevel = LogLevel.Debug;

		private int _checkTriggerTimerInterval;
		private string _msAppId;
		private string _userName;
		private string _password;
		private const int DefaultTriggerTimeInterval = 120;


		public IniSettings(IHSApplication hs)
		{
			_hs = hs;
			LoadSettingsFromIniFile();
		}

		public string PasswordMelCloud
		{
			get => _password;
			set
			{
				_password = value;
				SavePasswordMelcloud();
				OnIniSettingsChangedForUserNameAndPassword();
			}
		}

		public string UserNameMelCloud
		{
			get => _userName;
			set
			{
				_userName = value;
				SaveUserNameMelcloud();
				OnIniSettingsChangedForUserNameAndPassword();
			}
		}

		private void SavePasswordMelcloud()
		{
			_hs.SaveINISetting(UserSection, PasswordKey, _password, Utility.IniFile);
		}

		private void SaveUserNameMelcloud()
		{

			_hs.SaveINISetting(UserSection, UserNameKey, _userName, Utility.IniFile);
		}

		public double CheckMelCloudTimerIntervalInMilliseconds => _checkTriggerTimerInterval * 1000;
		public event IniSettingsChangedForUserNamePasswordEventHandler IniSettingsChangedForUserNamePassword;
		public event IniSettingsChangedForCheckIntervalEventHandler IniSettingsChangedForCheckInterval;
		public event IniSettingsChangedForLogLevelEventHandler IniSettingsChangedForLogLevel;

		public bool PasswordAndUsernameOk()
		{
			if (!string.IsNullOrEmpty(_password) && !string.IsNullOrEmpty(_userName))
				return true;
			return false;
		}

		public int CheckMelCloudTimerInterval
		{
			get => _checkTriggerTimerInterval;
			set
			{
				_checkTriggerTimerInterval = value;
				SaveCheckTriggerTimerIntervalToIni();
				OnIniSettingsChangedForCheckInterval();
			}
		}

		protected virtual void OnIniSettingsChangedForCheckInterval()
		{
			IniSettingsChangedForCheckInterval?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void OnIniSettingsChangedForUserNameAndPassword()
		{
			IniSettingsChangedForUserNamePassword?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void OnIniSettingsChangedForLogLevel()
		{
			IniSettingsChangedForLogLevel?.Invoke(this, EventArgs.Empty);
		}

		public void LoadSettingsFromIniFile()
		{
			_logLevel = LoadLogLevel();
			_userName = LoadUserName();
			_password = LoadPassword();
			_checkTriggerTimerInterval = LoadCheckTriggerTimerInterval();
		}

		private int LoadCheckTriggerTimerInterval()
		{
			var checkTriggerTimerIntervalString = _hs.GetINISetting(ConfigSection, CheckMelCloudIntervalKey, "", Utility.IniFile);
			if (int.TryParse(checkTriggerTimerIntervalString, out var tempInterval))
			{
				if (tempInterval > 0)
					return tempInterval;
			}
			//return tempInterval;
			return DefaultTriggerTimeInterval;
		}

		private void SaveCheckTriggerTimerIntervalToIni()
		{
			_hs.SaveINISetting(ConfigSection, CheckMelCloudIntervalKey, _checkTriggerTimerInterval.ToString(), Utility.IniFile);
		}

		private string LoadUserName()
		{
			_userName = _hs.GetINISetting(UserSection, UserNameKey, "", Utility.IniFile);
			return _userName;
		}

		private string LoadPassword()
		{
			_password = _hs.GetINISetting(UserSection, PasswordKey, "", Utility.IniFile);
			return _password;
		}

		private LogLevel LoadLogLevel()
		{
			var debugLevelAsString = _hs.GetINISetting(ConfigSection, LogLevelKey, "Debug", Utility.IniFile);
			LogLevel logLevelToReturn;
			if (!Enum.TryParse(debugLevelAsString, true, out logLevelToReturn))
			{
				logLevelToReturn = LogLevel.Normal;
			}
			return logLevelToReturn;
		}

		public LogLevel LogLevel
		{
			get => _logLevel;
			set
			{
				_logLevel = value;
				SaveLogLevel();
				OnIniSettingsChangedForLogLevel();
			}
		}


		//public string TestfileLocation
		//{
		//	get { return _testfileLocation; }
		//	set
		//	{
		//		_testfileLocation = value;
		//		SaveTestfileLocation();
		//	}
		//}

		//private void SaveTestfileLocation()
		//{
		//	_hs.SaveINISetting(ConfigSection, TestfileLocationKey, _testfileLocation, Utility.IniFile);
		//	OnIniSettingsChanged();
		//}

		//public bool UseTestfile
		//{
		//	get
		//	{
		//		return _useTestfile;
		//	}
		//	set
		//	{
		//		_useTestfile = value;
		//		SaveUseTestfile();
		//	}
		//}

		//public bool LogRfLinkDataToFile
		//{
		//	get
		//	{
		//		return _logRfLinkDataToFile;
		//	}
		//	set
		//	{
		//		_logRfLinkDataToFile = value;
		//		SaveLogRfLinkDataToFile();
		//	}
		//}

		//private void SaveLogRfLinkDataToFile()
		//{
		//	_hs.SaveINISetting(ConfigSection, LogRfLinkDataToFileKey, _logRfLinkDataToFile.ToString(), Utility.IniFile);
		//}



		//private void SaveUseTestfile()
		//{
		//	_hs.SaveINISetting(ConfigSection, UseTestfileKey, _useTestfile.ToString(), Utility.IniFile);
		//	OnIniSettingsChanged();
		//}

		private void SaveLogLevel()
		{
			var logLevelToSave = Enum.GetName(typeof(LogLevel), _logLevel);

			_hs.SaveINISetting(ConfigSection, LogLevelKey, logLevelToSave, Utility.IniFile);
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
				// Indicate that the instance has been disposed.
				_disposed = true;
			}
		}
	}

}