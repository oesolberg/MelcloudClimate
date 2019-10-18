using System;
using System.Collections.Generic;
using System.Text;
using HomeSeerAPI;
using HSPI_MelcloudClimate.Common;
using Scheduler;

namespace HSPI_MelcloudClimate.ConfigPage
{
	public class GeneralConfig : PageBuilderAndMenu.clsPageBuilder
	{
		private readonly IHSApplication _hs;
		private IAppCallbackAPI _callback;
		private readonly IIniSettings _iniSettings;
		private string _pluginName;
		private const string _pageName = "MelCloud_General_Config";
		private const string _pageNameText = "General Config";

		private const string LogLevelKey = "LogLevelKey";
		private const string TriggerCheckIntervalKey = "TriggerCheckInterval";
		private const string UsernameKey = "UsernameKey";
		private const string PasswordKey = "PasswordKey";

		public GeneralConfig(IHSApplication hs, IAppCallbackAPI callback,string pluginName, IIniSettings iniSettings) : base(_pageName)
		{
			_hs = hs;
			_callback = callback;
			_pluginName = pluginName;
			_iniSettings = iniSettings;
		}

		public string GetPagePlugin(string page, string user, int userRights, string queryString)
		{
			Console.WriteLine($"got call for page {page} from user {user} with user rights {userRights} and querystring {queryString}");
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
			//time between calendar checks
			returnString.Append("  <tr class='tablerowodd'><td>User name:</td><td>" +
			                    SetUserName() + "</td></tr>");
			//time between calendar checks
			returnString.Append("  <tr class='tableroweven'><td>Password:</td><td>" +
			                    SetPassword() + "</td></tr>");
			//time between trigger checks
			returnString.Append("  <tr class='tablerowodd'><td>Time between check of Melcloud:</td><td>" +
			                    SetMelCloudTimeCheck() + "</td></tr>");

		

			//Set log level
			returnString.Append("  <tr class='tablerowodd'><td>Log level:</td><td>" + SetLogLevelUserInterface() +
			                    "</td></tr>");


			returnString.Append("</td></tr>");
			returnString.Append(" </table>");

			returnString.Append("<br/><br/>");

			return returnString.ToString();

		}

		private string SetPassword()
		{
			return string.Empty;
		}

		private string SetUserName()
		{
			return string.Empty;
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
			
			return "SOMETHING IS MISSING!!!";
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
				link = _pageName ,
				linktext = pageTitle,
				page_title = pageTitle 
			};
			_hs.RegisterPage(_pageName, _pluginName, string.Empty);
			_callback.RegisterLink(webPageDescription);
		}

		
	}
}