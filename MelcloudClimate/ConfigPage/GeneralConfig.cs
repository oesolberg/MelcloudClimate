using System;
using System.Text;
using HomeSeerAPI;
using Scheduler;

namespace HSPI_MelcloudClimate.ConfigPage
{
	public class GeneralConfig : PageBuilderAndMenu.clsPageBuilder
	{
		private readonly IHSApplication _hs;
		private IAppCallbackAPI _callback;
		private string _pluginName;
		private const string _pageName = "MelCloud_General_Config";
		private const string _pageNameText = "General Config";


		public GeneralConfig(IHSApplication hs, IAppCallbackAPI callback,string pluginName) : base(_pageName)
		{
			_hs = hs;
			_callback = callback;
			_pluginName = pluginName;
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
			//returnString.Append(ShowMissingCredentialsErrorIfCredentialsMissing());
			returnString.Append(PageBuilderAndMenu.clsPageBuilder.DivEnd());

			//returnString.Append(BuildContent());

			returnString.Append(PageBuilderAndMenu.clsPageBuilder.DivEnd());
			this.AddFooter(_hs.GetPageFooter());
			this.suppressDefaultFooter = true;
			this.AddBody(returnString.ToString());



			return this.BuildPage();
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