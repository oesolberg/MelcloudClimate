using System;
using HomeSeerAPI;

namespace HSPI_MelcloudClimate.ConfigPage
{
	public class GeneralConfig
	{
		private readonly IHSApplication _hs;
		private IAppCallbackAPI _callback;
		private string _pluginName;

		public GeneralConfig(IHSApplication hs, IAppCallbackAPI callback,string pluginName)
		{
			_hs = hs;
			_callback = callback;
			_pluginName = pluginName;
		}

		public string GetPagePlugin(string page, string user, int userRights, string queryString)
		{
			Console.WriteLine($"got call for page {page} from user {user} with user rights {userRights} and querystring {queryString}");
			return string.Empty;
		}

		public string PostBackProc(string page, string data, string user, int userRights)
		{
			
			return "SOMETHING IS MISSING!!!";
		}

		public void Register()
		{
			Scheduler.PageBuilderAndMenu.clsPageBuilder pageToRegister;
			var pageName = "MelCloud_General_Config";
			_hs.RegisterPage(pageName, _pluginName, _pluginName);

			var linkText = pageName;
			linkText = linkText.Replace("MelCloud_", "").Replace("_", " ").Replace(_pluginName, "");
			var pageTitle = linkText;

			var webPageDescription = new WebPageDesc
			{
				plugInName = _pluginName,
				link = pageName ,
				linktext = pageTitle,
				page_title = pageTitle 
			};

			_callback.RegisterLink(webPageDescription);
		}

		//	var webPageDescription = new WebPageDesc
		//	{
		//		plugInName = Utility.PluginName,
		//		link = pageName + Utility.InstanceFriendlyName,
		//		linktext = pageTitle,
		//		page_title = pageTitle + Utility.InstanceFriendlyName
		//	};

		//	_callback.RegisterLink(webPageDescription);
		//
	}
}