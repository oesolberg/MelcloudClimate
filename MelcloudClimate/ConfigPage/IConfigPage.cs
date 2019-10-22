namespace HSPI_MelcloudClimate.ConfigPage
{
	public interface IConfigPage
	{
		string PageName { get; }
		//string GetPagePlugin(string page, string user, int userRights, string queryString, UnitController unitController);
		string GetPagePlugin(string page, string user, int userRights, string queryString);
		string PostBackProc(string page, string data, string user, int userRights);
	}
}