using System.Reflection;
using System.Text;
using HomeSeerAPI;
using HSPI_MelcloudClimate.Common;
using Scheduler;

namespace HSPI_MelcloudClimate.ConfigPage
{
	public class AboutPage : PageBuilderAndMenu.clsPageBuilder, IConfigPage
	{
		private readonly string _pageName;
		private readonly IHSApplication _hs;

		public AboutPage(string pageName,IHSApplication hs) : base(pageName)
		{
			_pageName = pageName;
			_hs = hs;
		}

		public new string PageName => _pageName;
		public string GetPagePlugin(string page, string user, int userRights, string queryString)
		{

			var returnString = new StringBuilder();
			reset();
			UsesJqAll = true;

			returnString.Append("<title>About RfLinkSeer plugin</title>");
			returnString.Append(_hs.GetPageHeader(_pageName, "RfLinkSeer", "", "", false, false));
			returnString.Append(DivStart("pluginpage", ""));

			//returnString.Append(BuildContent());
			returnString.Append("<br/>");
			//returnString.Append("<br/>Big thank you to the RFLink Stuntteam! <a href=\"http://www.rflink.nl\" target=\"_blank\">www.rflink.nl</a>");
			returnString.Append("<br/>");
			returnString.Append("<br/>Aleks at <a href=\"https://www.hjemmeautomasjon.no\" target=\"_blank\">www.hjemmeautomasjon.no</a>");
			returnString.Append("<br/>Moskus at <a href=\"https://www.hjemmeautomasjon.no\" target=\"_blank\">www.hjemmeautomasjon.no</a>");
			returnString.Append("<br/>");

			returnString.Append("<br/>Testers: ");
			returnString.Append("<br/>fjaeran at <a href=\"https://www.hjemmeautomasjon.no\" target=\"_blank\">www.hjemmeautomasjon.no</a>");
			returnString.Append("<br/>");

			//returnString.Append("<br/>Icons from ");
			//returnString.Append("<br/>Randolph Novino - Noun Project (Barometric pressure icon)");
			//returnString.Append("<br/>amanda - Noun Project (Ruler/distance icon)");
			//returnString.Append("<br/>Arthur Shlain - Noun Project (weather icons)");
			//returnString.Append("<br/>JohnnyZi - Noun Project (Question mark for weather)");
			//returnString.Append("<br/>iconsmind.com - Noun Project (temperature meter)");
			//returnString.Append("<br/>hunotika- Noun Project (wind directions)");
			//returnString.Append("<br/>B.Agustín Amenábar Larraín - Noun Project (wind)");
			//returnString.Append("<br/>David - Noun Project (volt and ampere icon)");
			//returnString.Append("<br/>Romualdas Jurgaitis - Noun Project (note icon)");
			//returnString.Append("<br/>Pham Duy Phuong Hung - Noun Project (meter icon)");
			//returnString.Append("<br/>Demak Daksina S - Noun Project (sound icon)");

			returnString.Append("<br/>");
			returnString.Append($"<br/>Guahtdim 2019 - {Utility.PluginName} version: " + Assembly.GetExecutingAssembly().GetName().Version);

			returnString.Append(DivEnd());
			AddFooter(_hs.GetPageFooter());
			suppressDefaultFooter = true;
			AddBody(returnString.ToString());
			return BuildPage();
		}

		public string PostBackProc(string page, string data, string user, int userRights)
		{
			throw new System.NotImplementedException("This page should not handle postbacks");
		}
	}
}