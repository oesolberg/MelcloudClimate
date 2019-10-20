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
		string PasswordMelCloud{ get; set; }

		event IniSettingsChangedEventHandler IniSettingsChanged;

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
		private LogLevel _logLevel;

		private int _checkTriggerTimerInterval;
		private string _msAppId;
		private string _userName;
		private string _password;
		private const int DefaultTriggerTimeInterval = 20;


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
				OnIniSettingsChanged();
			}
		}

		public string UserNameMelCloud
		{
			get => _userName;
			set
			{
				_userName = value;
				SaveUserNameMelcloud();
				OnIniSettingsChanged();
			}
		}

		private void SavePasswordMelcloud()
		{
			_hs.SaveINISetting(UserSection, PasswordKey, _password, Utility.IniFile);
			OnIniSettingsChanged();
		}

		private void SaveUserNameMelcloud()
		{

			_hs.SaveINISetting(UserSection, UserNameKey, _userName, Utility.IniFile);
				OnIniSettingsChanged();
		}

		public event IniSettingsChangedEventHandler IniSettingsChanged;

		public int CheckMelCloudTimerInterval
		{
			get => _checkTriggerTimerInterval;
			set
			{
				_checkTriggerTimerInterval = value;
				SaveCheckTriggerTimerIntervalToIni();
				OnIniSettingsChanged();
			}
		}

		protected virtual void OnIniSettingsChanged()
		{
			IniSettingsChanged?.Invoke(this, EventArgs.Empty);
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
				if(tempInterval>0)
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
			_password= _hs.GetINISetting(UserSection, PasswordKey, "", Utility.IniFile);
			return _password;
		}

		private LogLevel LoadLogLevel()
		{
			var debugLevelAsString = _hs.GetINISetting(ConfigSection, LogLevelKey, "NONE", Utility.IniFile);
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
				OnIniSettingsChanged();
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
			OnIniSettingsChanged();
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