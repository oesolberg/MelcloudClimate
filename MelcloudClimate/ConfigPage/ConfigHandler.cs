using System;
using System.Collections.Generic;
using System.Linq;
using HomeSeerAPI;
using HSPI_MelcloudClimate.Common;
using HSPI_MelcloudClimate.Libraries.Logs;

namespace HSPI_MelcloudClimate.ConfigPage
{
	public class ConfigHandler
	{
		private readonly IHSApplication _hs;
		private readonly IAppCallbackAPI _callbackApi;
		private List<IConfigPage> _configPages;
		private readonly IIniSettings _iniSettings;
		private readonly ILog _log;
		private const string MelcloudAbout = "MelCloud_About";
		private const string MelcloudGeneralConfig = "MelCloud_General_Config";

		public ConfigHandler(IHSApplication hs,IAppCallbackAPI callbackApi,IIniSettings iniSettings,ILog log)
		{
			_hs = hs;
			_callbackApi = callbackApi;
			_iniSettings = iniSettings;
			_log = log;
		}

		private IConfigPage FindSelectedPage(string page)
		{
			IConfigPage toBeReturned = _configPages.FirstOrDefault(x => x.PageName == page);
			if (toBeReturned == null) Console.WriteLine($"Got a pagename that does not exist '{page}'");

			return toBeReturned;
		}

		public  string GetPagePlugin(string page, string user, int userRights, string queryString)
		{
			//Handle callbacks to plugin pages (normally config pages, but also any other page that this plugin controls
			Console.WriteLine($"got call for page {page} from user {user} with userrights {userRights} and querystring {queryString}");
			var webPage = FindSelectedPage(page);

			return webPage.GetPagePlugin(page, user, userRights, queryString);
		}

		public  string PostBackProc(string page, string data, string user, int userRights)
		{
			//Console.WriteLine($"postback firing {DateTime.Now.ToString("HH:mm:ss")}");
			var selectedPage = FindSelectedPage(page);
			if (selectedPage != null)
				return selectedPage.PostBackProc(page, data, user, userRights);
			return "something is missing";
		}


		private void RegisterPlugin()
		{
			var wpd = new WebPageDesc
			{
				link = MelcloudGeneralConfig,
				plugInName = Utility.PluginName
			};
			_callbackApi.RegisterConfigLink(wpd);
		}

		private void CreateConfigPages()
		{
			//Should create configpage in initio
			if (_configPages == null) _configPages = new List<IConfigPage>();
			_configPages.Add(CreateConfigPage(MelcloudGeneralConfig));
			_configPages.Add(CreateConfigPage(MelcloudAbout));
		}
		private IConfigPage CreateConfigPage(string pageName)
		{
			Scheduler.PageBuilderAndMenu.clsPageBuilder pageToRegister;
			switch (pageName)
			{
				case MelcloudGeneralConfig:
					pageToRegister = new GeneralConfig(pageName, _hs,_callbackApi, _iniSettings, _log);
					break;
				case MelcloudAbout:
					pageToRegister = new AboutPage(pageName, _hs);
					break;
				default: throw new NotImplementedException($"Page {pageName} is not implemented");
			}

			_hs.RegisterPage(pageName, Utility.PluginName, Utility.InstanceFriendlyName);

			var linkText = pageName;
			linkText = linkText.Replace("RFLink_", "").Replace("_", " ").Replace(Utility.PluginName, "");
			var pageTitle = linkText;

			var webPageDescription = new WebPageDesc
			{
				plugInName = Utility.PluginName,
				link = pageName + Utility.InstanceFriendlyName,
				linktext = pageTitle,
				page_title = pageTitle + Utility.InstanceFriendlyName
			};

			_callbackApi.RegisterLink(webPageDescription);
			return (IConfigPage)pageToRegister;
		}

		public void Register()
		{
			RegisterPlugin();
			CreateConfigPages();
		}
	}
}