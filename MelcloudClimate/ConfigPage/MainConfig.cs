//using System;
//using System.Collections.Generic;
//using System.Linq;
//using HomeSeerAPI;


//namespace HSPI_MelcloudClimate.ConfigPage
//{


//		public interface IMainConfig
//		{
//			void RegisterConfigs();
//			string PostBackProc(string page, string data, string user, int userRights);
//			string GetPagePlugin(string page, string user, int userRights, string queryString);
//		}
//		public class MainConfig : IMainConfig
//		{
//			private readonly ILogging _log;
//			private readonly IHSApplication _hs;
//			private readonly IAppCallbackAPI _callback;
//			private List<IConfigPage> _configPages;
//			private readonly IIniSettings _iniSettings;
//			private readonly ICalendarConnectionGoogle _calendarConnectionGoogle;
//			private readonly ICalendarConnectionOutlook _calendarConnectionOutlook;
//			private IGCalPlugin _mainPlugin;
	

//			public MainConfig(ILogging log, IHSApplication hs, IAppCallbackAPI callback, IIniSettings iniSettings,
//				ICalendarConnectionGoogle calendarConnectionGoogle, ICalendarConnectionOutlook calendarConnectionOutlook, IGCalPlugin mainPlugin)
//			{
//				_log = log;
//				_hs = hs;
//				_callback = callback;
//				_iniSettings = iniSettings;
//				_calendarConnectionGoogle = calendarConnectionGoogle;
//				_calendarConnectionOutlook = calendarConnectionOutlook;
//				_mainPlugin = mainPlugin;
//			}

//			public void RegisterConfigs()
//			{
//				var wpd = new WebPageDesc
//				{
//					link = GCalSeerGeneralConfig,
//					plugInName = Utility.PluginName
//				};
//				_callback.RegisterConfigLink(wpd);

//				if (_configPages == null) _configPages = new List<IConfigPage>();
//				_configPages.Add(CreateConfigPage(GCalSeerGeneralConfig));
//				_configPages.Add(CreateConfigPage(GCalSeerCalendarAuth));
//				_configPages.Add(CreateConfigPage(GCalSeerCalendarMsAuth));
//				//_configPages.Add(CreateConfigPage(GCalSeerCalendarDevices));
//				//_configPages.Add(CreateConfigPage(GCalSeerRegexTester));
//				//_configPages.Add(CreateConfigPage(GCalSeerHelpConfig));
//				_configPages.Add(CreateConfigPage(GCalSeerAboutPage));
//			}

//			public string PostBackProc(string page, string data, string user, int userRights)
//			{
//				var selectedPage = FindSelectedPage(page);
//				if (selectedPage != null)
//					return selectedPage.PostBackProc(page, data, user, userRights);
//				return "SOMETHING IS MISSING!!!";
//			}

//			private IConfigPage FindSelectedPage(string page)
//			{
//				IConfigPage toBeReturned = _configPages.FirstOrDefault(x => x.PageName == page);
//				if (toBeReturned == null) Console.WriteLine($"Got a page name that does not exist '{page}'");

//				return toBeReturned;
//			}

//			public string GetPagePlugin(string page, string user, int userRights, string queryString)
//			{
//				Console.WriteLine($"got call for page {page} from user {user} with user rights {userRights} and querystring {queryString}");
//				var webPage = FindSelectedPage(page);
//				return webPage.GetPagePlugin(page, user, userRights, queryString);
//			}

//			private IConfigPage CreateConfigPage(string pageName)
//			{
//				Scheduler.PageBuilderAndMenu.clsPageBuilder pageToRegister;
//				switch (pageName)
//				{
//					case GCalSeerGeneralConfig:
//						pageToRegister = new ConfigGeneral(pageName, _hs, _iniSettings, _log, _calendarConnectionGoogle, null, _mainPlugin);
//						break;
//					case GCalSeerAboutPage:
//						pageToRegister = new ConfigAbout(pageName, _hs, _iniSettings, _log);
//						break;
//					case GCalSeerHelpConfig:
//						pageToRegister = new ConfigHelp(pageName, _hs, _iniSettings, _log);
//						break;
//					case GCalSeerCalendarAuth:
//						pageToRegister = new ConfigGoogleCalendar(pageName, _hs, _iniSettings, _log, _calendarConnectionGoogle);
//						break;
//					case GCalSeerCalendarMsAuth:
//						pageToRegister = new ConfigMsCalendar(pageName, _hs, _iniSettings, _log, _calendarConnectionOutlook);
//						break;
//					case GCalSeerCalendarDevices:
//						pageToRegister = new ConfigCalendarDevices(pageName, _hs, _iniSettings, _log, _calendarConnectionOutlook, _calendarConnectionGoogle);
//						break;
//					case GCalSeerRegexTester:
//						pageToRegister = new ConfigRegExTester(pageName, _hs, _iniSettings, _log);
//						break;
//					default: throw new NotImplementedException($"Page {pageName} is not implemented");
//				}

//				_hs.RegisterPage(pageName, Utility.PluginName, Utility.InstanceFriendlyName);

//				var linkText = pageName;
//				linkText = linkText.Replace("GCalSeer_", "").Replace("_", " ").Replace(Utility.PluginName, "");
//				var pageTitle = linkText;

//				var webPageDescription = new WebPageDesc
//				{
//					plugInName = Utility.PluginName,
//					link = pageName + Utility.InstanceFriendlyName,
//					linktext = pageTitle,
//					page_title = pageTitle + Utility.InstanceFriendlyName
//				};

//				_callback.RegisterLink(webPageDescription);
//				return (IConfigPage)pageToRegister;
//			}
//		}
//	}

//}