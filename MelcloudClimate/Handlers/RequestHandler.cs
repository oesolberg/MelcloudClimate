using RestSharp;

namespace HSPI_MelcloudClimate.Handlers
{
	public interface IRequestHandler
	{

	}

	public class RequestHandler:IRequestHandler
	{
		public IRestResponse SendJsonRequest()
		{
			//var request = new RestRequest("User/ListDevices", Method.GET);
			//request.AddHeader("Accept", "application/json");
			//request.AddHeader("X-MitsContextKey", ContextKey.ToString());
			//request.RequestFormat = DataFormat.Json;

			//IRestResponse response = client.Execute(request);
			return null;
		}
	}
}