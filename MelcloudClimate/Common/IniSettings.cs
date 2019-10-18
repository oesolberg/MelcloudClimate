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
		private const string GCalendarItemsSection = "GCALENDARS";
		private const string GCalendarIdItemsSection = "GCALENDARIDS";
		private const string MsCalendarItemsSection = "MSCALENDARS";
		private const string MsCalendarIdItemsSection = "MSCALENDARIDS";

		private readonly string ConfigSection = "CONFIG";

		public const string LogLevelKey = "LOGLEVEL";
		public const string MsAppIdKey = "MICROSOFT_APP_ID";
		public const string MsRedirectUriKey = "MICROSOFT_REDIRECT_URI";
		public const string MsAppPasswordKey = "MICROSOFT_APP_PASSWORD";
		public const string CheckCalendarIntervalKey = "CHECKCALENDARINTERVAL";
		public const string TestfileLocationKey = "TESTFILE";
		public const string UseTestfileKey = "USETESTFILE";
		private const string LogRfLinkDataToFileKey = "LOGRFLINKDATATOFILE";
		private readonly IHSApplication _hs;
		//private Dictionary<int, TcpIpAddress> _tcpIpAddressList = new Dictionary<int, TcpIpAddress>();

		private Dictionary<int, string> _comportsUsedList = new Dictionary<int, string>();
		private bool _disposed;
		private LogLevel _logLevel;
		//private string _testfileLocation;
		//private bool _useTestfile;
		//private bool _logRfLinkDataToFile;
		private TimeSpan _calendarCheckInterval = new TimeSpan(2, 0, 0, 0);
		private Dictionary<int, string> _gcalendarHashItems;
		private Dictionary<int, string> _mscalendarHashItems;
		private Dictionary<int, string> _gcalendarIdItems;
		private Dictionary<int, string> _msCalendarIdItems;
		private int _checkTriggerTimerInterval;
		private string _msAppId;
		private const string CheckTriggerTimerIntervalKey = "TRIGGERTIMERINTERVAL";
		private const int DefaultTriggerTimeInterval = 20;
		private readonly TimeSpan _defaultCheckCalendarInterval = new TimeSpan(0, 0, 1, 0);
		private string _msRedirectUri;
		private string _msAppPassword;

		public Dictionary<int, string> ComportsList => _comportsUsedList;
		//public Dictionary<int, TcpIpAddress> TcpIpAddressList => _tcpIpAddressList;

		public IniSettings(IHSApplication hs)
		{
			_hs = hs;
		}


		public string MsAppId
		{
			get => _msAppId;
			set
			{
				_msAppId = value;
				SaveMsAppIdToIni();
				OnIniSettingsChanged();
			}
		}

		public int NumberOfDaysToFetchFromThePast { get; set; }
		public int NumberOfDaysToFetchFromTheFuture { get; set; }
		public string MsRedirectUri
		{
			get => _msRedirectUri;
			set
			{
				_msRedirectUri = value;
				SaveMsRedirectUriToIni();
				OnIniSettingsChanged();
			}
		}

		public string MsAppPassword
		{
			get => _msAppPassword;
			set
			{
				_msAppPassword = value;
				SaveMsAppPasswordToIni();
				OnIniSettingsChanged();
			}
		}

		private void SaveMsAppIdToIni()
		{
			_hs.SaveINISetting(ConfigSection, MsAppIdKey, _msAppId, Utility.IniFile);
		}
		private void SaveMsRedirectUriToIni()
		{
			_hs.SaveINISetting(ConfigSection, MsRedirectUriKey, _msRedirectUri, Utility.IniFile);
		}
		private void SaveMsAppPasswordToIni()
		{
			_hs.SaveINISetting(ConfigSection, MsAppPasswordKey, _msAppPassword, Utility.IniFile);
		}

		public string PasswordMelCloud { get; set; }
		event IniSettingsChangedEventHandler IIniSettings.IniSettingsChanged
		{
			add => throw new NotImplementedException();
			remove => throw new NotImplementedException();
		}

		public event IniSettingsChangedEventHandler IniSettingsChanged;

		public Dictionary<int, string> GCalendarHashItems => _gcalendarHashItems;
		public Dictionary<int, string> MsCalendarHashItems => _mscalendarHashItems;



		public int CheckTriggerTimerInterval
		{
			get => _checkTriggerTimerInterval;
			set
			{
				_checkTriggerTimerInterval = value;
				SaveCheckTriggerTimerIntervalToIni();
				OnIniSettingsChanged();
			}
		}

		public List<string> GetMsCalendarIds
		{
			get
			{
				if (_msCalendarIdItems != null && _msCalendarIdItems.Count > 0)
					return _msCalendarIdItems.Select(x => x.Value).ToList();
				else
				{
					return new List<string>();
				}
			}
		}

		public List<string> GetCalendarIds
		{
			get
			{
				if (_gcalendarIdItems != null && _gcalendarIdItems.Count > 0)
					return _gcalendarIdItems.Select(x => x.Value).ToList();
				else
				{
					return new List<string>();
				}
			}
		}

		public TimeSpan CalendarCheckInterval
		{
			get
			{
				return _calendarCheckInterval;
			}
			set
			{
				_calendarCheckInterval = value;
				SaveCalendarCheckIntervalToIni();
				OnIniSettingsChanged();
			}
		}

		protected virtual void OnIniSettingsChanged()
		{
			IniSettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		public void LoadSettingsFromIniFile()
		{
			_gcalendarHashItems = new Dictionary<int, string>();
			var tempGcalendarItems = _hs.GetINISectionEx(GCalendarItemsSection, Utility.IniFile).ToList();
			CreateCalendarItemHashList(tempGcalendarItems, _gcalendarHashItems);

			_gcalendarIdItems = new Dictionary<int, string>();
			tempGcalendarItems = _hs.GetINISectionEx(GCalendarIdItemsSection, Utility.IniFile).ToList();
			CreateCalendarItemIdDictionary(tempGcalendarItems, _gcalendarIdItems);

			_mscalendarHashItems = new Dictionary<int, string>();
			var tempMsCalendarItems = _hs.GetINISectionEx(MsCalendarItemsSection, Utility.IniFile).ToList();
			CreateCalendarItemHashList(tempMsCalendarItems, _mscalendarHashItems);

			_msCalendarIdItems = new Dictionary<int, string>();
			tempMsCalendarItems = _hs.GetINISectionEx(MsCalendarIdItemsSection, Utility.IniFile).ToList();
			CreateCalendarItemIdDictionary(tempMsCalendarItems, _msCalendarIdItems);

			_logLevel = GetLogLevel();

			_msAppId = GetMsAppId();
			_msAppPassword = GetMsAppPassword();
			_msRedirectUri = GetMsRedirectUri();
			_calendarCheckInterval = GetCalendarCheckInterval();
			_checkTriggerTimerInterval = GetCheckTriggerTimerInterval();
		}

		private int GetCheckTriggerTimerInterval()
		{
			var checkTriggerTimerIntervalString = _hs.GetINISetting(ConfigSection, CheckTriggerTimerIntervalKey, "", Utility.IniFile);
			if (int.TryParse(checkTriggerTimerIntervalString, out var tempInterval))
			{
				return tempInterval;
			}
			//return tempInterval;
			return DefaultTriggerTimeInterval;
		}

		private void SaveCheckTriggerTimerIntervalToIni()
		{
			_hs.SaveINISetting(ConfigSection, CheckTriggerTimerIntervalKey, _checkTriggerTimerInterval.ToString(), Utility.IniFile);
		}

		private void SaveCalendarCheckIntervalToIni()
		{
			var timespanString = _calendarCheckInterval.ToString("G");
			_hs.SaveINISetting(ConfigSection, CheckCalendarIntervalKey, timespanString, Utility.IniFile);
		}
		private string GetMsAppId()
		{
			_msAppId = _hs.GetINISetting(ConfigSection, MsAppIdKey, "", Utility.IniFile);
			return _msAppId;
		}

		private string GetMsAppPassword()
		{
			_msAppPassword = _hs.GetINISetting(ConfigSection, MsAppPasswordKey, "", Utility.IniFile);
			return _msAppPassword;
		}

		private string GetMsRedirectUri()
		{
			_msRedirectUri = _hs.GetINISetting(ConfigSection, MsRedirectUriKey, "", Utility.IniFile);
			return _msRedirectUri;
		}


		private TimeSpan GetCalendarCheckInterval()
		{
			var timespanString = _hs.GetINISetting(ConfigSection, CheckCalendarIntervalKey, "", Utility.IniFile);

			var timespan = _defaultCheckCalendarInterval;
			if (timespanString.Length > 3 && TimeSpan.TryParse(timespanString, out timespan))
			{
			}
			return timespan;
		}

		private void CreateCalendarItemHashList(List<string> calendarItems, Dictionary<int, string> dictionary)
		{
			foreach (var calendarItem in calendarItems)
			{
				var splitArray = calendarItem.Split(new char[] { '=' }, 2);
				if (splitArray.Length > 1 && !string.IsNullOrWhiteSpace(splitArray[splitArray.Length - 1]))
				{
					var indexKey = int.Parse(splitArray[splitArray.Length - 2]);
					var calendarName = splitArray[splitArray.Length - 1];
					dictionary.Add(indexKey, calendarName);
				}
			}
		}


		private void CreateCalendarItemIdDictionary(List<string> calendarItems, Dictionary<int, string> dictionary)
		{
			foreach (var calendarItem in calendarItems)
			{
				var splitArray = calendarItem.Split(new char[] { '=' }, 2);
				if (splitArray.Length > 1 && !string.IsNullOrWhiteSpace(splitArray[splitArray.Length - 1]))
				{
					var indexKey = int.Parse(splitArray[splitArray.Length - 2]);
					var calendarId = splitArray[splitArray.Length - 1];
					dictionary.Add(indexKey, calendarId);
				}
			}
		}

		private LogLevel GetLogLevel()
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
				SaveLoglevel();
				OnIniSettingsChanged();
			}
		}

		public int CheckMelCloudTimerInterval { get; set; }
		public string UserNameMelCloud { get; set; }

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

		private void SaveLoglevel()
		{

			var loglevelToSave = Enum.GetName(typeof(LogLevel), _logLevel);

			_hs.SaveINISetting(ConfigSection, LogLevelKey, loglevelToSave, Utility.IniFile);
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