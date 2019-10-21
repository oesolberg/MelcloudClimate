using System;
using System.Collections.Generic;
using System.Text;
using HomeSeerAPI;
using HSPI_MelcloudClimate.Common;
using HSPI_MelcloudClimate.Libraries.Logs;
using Scheduler;

namespace HSPI_MelcloudClimate.ConfigPage
{
	public class GeneralConfig : PageBuilderAndMenu.clsPageBuilder
	{
		private readonly IHSApplication _hs;
		private IAppCallbackAPI _callback;
		private readonly IIniSettings _iniSettings;
		private string _pluginName;
		private ILog _log;
		private const string IdKey = "id";
		private const string _pageName = "MelCloud_General_Config";
		private const string _pageNameText = "General Config";

		private const string LogLevelKey = "LogLevelKey";
		private const string TriggerCheckIntervalKey = "TriggerCheckInterval";
		private const string UserNameKey = "UserNameKey";
		private const string PasswordKey = "PasswordKey";

		public GeneralConfig(IHSApplication hs, IAppCallbackAPI callback, 
			string pluginName, IIniSettings iniSettings, ILog log) : base(_pageName)
		{
			_hs = hs;
			_callback = callback;
			_pluginName = pluginName;
			_iniSettings = iniSettings;
			_log = log;
		}

		public void Register()
		{
			Scheduler.PageBuilderAndMenu.clsPageBuilder pageToRegister;

			_hs.RegisterPage(_pageName, _pluginName, _pluginName);

			var linkText = _pageName;
			linkText = linkText.Replace("MelCloud_", "").Replace("_", " ").Replace(_pluginName, "");
			var pageTitle = linkText;

			var wpd = new WebPageDesc
			{
				link = _pageName,
				plugInName = _pluginName
			};
			_callback.RegisterConfigLink(wpd);

			var webPageDescription = new WebPageDesc
			{
				plugInName = _pluginName,
				link = _pageName,
				linktext = pageTitle,
				page_title = pageTitle
			};
			_hs.RegisterPage(_pageName, _pluginName, string.Empty);
			_callback.RegisterLink(webPageDescription);
		}

		public string GetPagePlugin(string page, string user, int userRights, string queryString)
		{
			_log.Debug($"got call for page {page} from user {user} with user rights {userRights} and querystring {queryString}");
			this.reset();
			var returnString = new StringBuilder();

			returnString.Append("<title>" + _pageNameText + "</title>");
			returnString.Append(_hs.GetPageHeader(_pageName, _pluginName, "", "", false, false));
			//' a message area for error messages from jquery ajax post back (optional, only needed if using AJAX calls to get data)
			returnString.Append(PageBuilderAndMenu.clsPageBuilder.DivStart("pluginpage", ""));
			returnString.Append(PageBuilderAndMenu.clsPageBuilder.DivStart("errormessage", "class='errormessage'"));
			returnString.Append(PageBuilderAndMenu.clsPageBuilder.DivEnd());

			returnString.Append(BuildContent());

			returnString.Append(PageBuilderAndMenu.clsPageBuilder.DivEnd());
			this.AddFooter(_hs.GetPageFooter());
			this.suppressDefaultFooter = true;
			this.AddBody(returnString.ToString());

			return this.BuildPage();
		}

		private string BuildContent()
		{
			var returnString = new StringBuilder();

			returnString.Append("<strong><div id=\'message\'>&nbsp;</div></strong><br/>");
			returnString.Append(" <table border='0' cellpadding='0' cellspacing='0' width='1000'>");
			returnString.Append("  <tr class='tableheader'><td width='250'>" + _pageNameText + "</td><td width='750'>" +
								$"General settings for {_pluginName}" + "</td></tr>");
			
			//Set user name
			returnString.Append("  <tr class='tablerowodd'><td>User name:</td><td>" +
								SetUserName() + "</td></tr>");
			
			//set password
			returnString.Append("  <tr class='tableroweven'><td>Password:</td><td>" +
								SetPassword() + "</td></tr>");

			//time between Melcloud checks
			//returnString.Append("  <tr class='tablerowodd'><td>Time between check of Melcloud (minutes:seconds):</td><td>" +
			//					SetMelCloudTimeCheck() + "</td></tr>");

			////Set log level
			//returnString.Append("  <tr class='tablerowodd'><td>Log level:</td><td>" + SetLogLevelUserInterface() +
			//					"</td></tr>");


			returnString.Append("</td></tr>");
			returnString.Append(" </table>");

			returnString.Append("<br/><br/>");

			return returnString.ToString();

		}

		private string SetPassword()
		{
			var passwordTextBox = new clsJQuery.jqTextBox(PasswordKey, "text", "", _pageName, 40, false);
			passwordTextBox.defaultText = _iniSettings.PasswordMelCloud;
			return passwordTextBox.Build();
		}

		private string SetUserName()
		{
			var userNameTextBox = new clsJQuery.jqTextBox(UserNameKey, "text", "", _pageName, 40, false);
			userNameTextBox.defaultText = _iniSettings.UserNameMelCloud;
			return userNameTextBox.Build();
		}


		private string SetMelCloudTimeCheck()
		{
			var checkTriggerTimePicker = new clsJQuery.jqTimePicker(TriggerCheckIntervalKey, "", _pageName, false);
			var currentCheckTriggerTimerInterval = _iniSettings.CheckMelCloudTimerInterval;
			var currentCheckTriggerTimeIntervalTimeSpan = new TimeSpan(0, 0, 0, currentCheckTriggerTimerInterval);
			checkTriggerTimePicker.minutesSeconds = true;
			checkTriggerTimePicker.defaultValue =
				$"{currentCheckTriggerTimeIntervalTimeSpan.Minutes.ToString("00")}:{currentCheckTriggerTimeIntervalTimeSpan.Seconds.ToString("00")}";
			return checkTriggerTimePicker.Build();
		}

		private string SetLogLevelUserInterface()
		{
			var logLevelDropdown = new clsJQuery.jqDropList(LogLevelKey, _pageName, false);
			logLevelDropdown.items = new List<Pair>()
			{
				new Pair() {Name = "None", Value = "0",},
				new Pair() {Name = "Normal", Value = "1"},
				new Pair() {Name = "Debug", Value = "2"},
				new Pair() {Name = "Debug to file", Value = "3"}
			};
			var iniSettingsLogLevel = _iniSettings.LogLevel;
			logLevelDropdown.selectedItemIndex = iniSettingsLogLevel.ToInt();
			return logLevelDropdown.Build();
		}

		public string PostBackProc(string page, string data, string user, int userRights)
		{

			Dictionary<string, string> dicQueryString = SplitDataString(data);

			if (dicQueryString.ContainsKey(IdKey))
			{
				var configUnit = dicQueryString[IdKey];
				switch (configUnit)
				{
					case LogLevelKey:
						HandleLogLevelDropDown(configUnit, dicQueryString[configUnit]);
						break;
					default:
						PostError("Unknown post back");
						break;
				}
			}

			else if (dicQueryString.ContainsKey(UserNameKey))
			{
				HandleUserNameChange(dicQueryString);
			}
			else if (dicQueryString.ContainsKey(PasswordKey))
			{
				HandlePasswordChange(dicQueryString);
			}
			else if (dicQueryString.ContainsKey(TriggerCheckIntervalKey))
			{
				HandleTriggerCheckIntervalChange(dicQueryString);
			}

			return base.postBackProc(page, data, user, userRights);
		}

		private void HandleLogLevelDropDown(string configUnit, string chosenNumber)
		{
			var logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), chosenNumber);
			_iniSettings.LogLevel = logLevel;
		}

		private void HandleTriggerCheckIntervalChange(Dictionary<string, string> dicQueryString)
		{
			var timeString = dicQueryString[TriggerCheckIntervalKey];
			var timespan = GetTimespanFromTimeString(timeString);
			if (timespan > new TimeSpan(0, 0, 0, 9))
			{
				_iniSettings.CheckMelCloudTimerInterval = (int)timespan.TotalSeconds;
			}
			else
			{
				PostError("Timespan is to little, minimum is 10 seconds. Change will not be stored.");
			}
		}

		private TimeSpan GetTimespanFromTimeString(string timeString)
		{
			var resultingTimeSpan = new TimeSpan(0, 0, 1, 0);
			//Only minutes and seconds
			if (!string.IsNullOrWhiteSpace(timeString) && timeString.Length >= 3)
			{
				var splitOnColon = timeString.Split(':');

				var numberOfMinutes = int.Parse(splitOnColon[0]);
				var numberOfSeconds = int.Parse(splitOnColon[1]);

				resultingTimeSpan = new TimeSpan(0, 0, numberOfMinutes, numberOfSeconds);
				if (resultingTimeSpan.TotalSeconds >= 3600)
				{
					resultingTimeSpan = new TimeSpan(0, 0, 59, 59);
				}
			}

			return resultingTimeSpan;
		}

		private void HandlePasswordChange(Dictionary<string, string> dicQueryString)
		{
			var password = dicQueryString[PasswordKey];
			_iniSettings.PasswordMelCloud = password;
		}

		private void HandleUserNameChange(Dictionary<string, string> dicQueryString)
		{
			var userName = dicQueryString[UserNameKey];
			_iniSettings.UserNameMelCloud = userName;
		}

		private Dictionary<string, string> SplitDataString(string data)
		{
			var returnDictionary = new Dictionary<string, string>();
			var splitByAmpersand = data.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var nameValuePair in splitByAmpersand)
			{
				var splitByEqual = nameValuePair.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
				if (splitByEqual.Length > 1)
				{
					returnDictionary.Add(splitByEqual[0], splitByEqual[1]);
				}

				if (splitByEqual.Length == 1)
				{
					returnDictionary.Add(splitByEqual[0], string.Empty);
				}
			}

			return returnDictionary;
		}

		private void PostError(string message)
		{
			this.divToUpdate.Add("errormessage", message);
		}
	}
}