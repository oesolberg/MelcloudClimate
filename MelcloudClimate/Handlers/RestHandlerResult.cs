using HSPI_MelcloudClimate.Libraries.Logs;
using Newtonsoft.Json;
using RestSharp;

namespace HSPI_MelcloudClimate.Handlers
{
	public class RestHandlerResult
	{
		private readonly ILog _log;
		public bool WrongUsernameOrPassword { get; set; }
		public bool Success { get; set; }
		public string Error { get; set; }
		public IRestResponse Response { get; set; }
		public string ResponseContent => Response?.Content;

		public RestHandlerResult(ILog log)
		{
			_log = log;
		}

		public bool ResponseIsOk()
		{
			if (Response != null && (int) Response.StatusCode == 200)
				return true;
			return false;
		}

		public string GetContextKey()
		{
			dynamic data = JsonConvert.DeserializeObject(Response.Content); //Convert data

			if (data.ContainsKey("ErrorId") && data.ErrorId == null)
			{
				_log.Debug("Seems like a login was successful");
				_log.Info("Successfully logged in to Melcloud");
				Success = true;
				return data.LoginData.ContextKey;
			}
			else if (data.ContainsKey("ErrorId") && data.ErrorId == 1)
			{

				_log.Debug("Wrong username and/or password");
				WrongUsernameOrPassword = true;
				Error = "Wrong username and/or password";
				Success = false;
			}
			else
			{
				_log.Debug("Something other than username and password failed during login to Melcloud");
				Success = false;
				
			}
			return string.Empty;
		}
	}
}