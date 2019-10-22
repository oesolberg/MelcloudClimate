using System;
using System.Runtime.Remoting.Messaging;
using HSPI_MelcloudClimate.Libraries.Logs;
using Newtonsoft.Json.Linq;
using RestSharp;
using Scheduler;

namespace HSPI_MelcloudClimate.Handlers
{
	public interface IRestHandler
	{
		bool NoContext { get; }
		RestHandlerResult Login(string melcloudEmail, string melcloudPassword);
		RestHandlerResult DoDeviceListing();
		RestHandlerResult UpdateDevice(object jsonObject);
		RestHandlerResult DoDeviceGet(JToken deviceId, JToken buildingId);
	}

	public class RestHandler : IRestHandler
	{
		private readonly ILog _log;
		private RestClient _client = new RestClient("https://app.melcloud.com/Mitsubishi.Wifi.Client/");
		private string _contextKey;
		private string DeviceGet= "Device/Get";
		private string ClientLogin= "Login/ClientLogin";
		private string UserListDevices = "User/ListDevices";
		private string DeviceSetAta = "Device/SetAta";

		public RestHandler(ILog log)
		{
			_log = log;
		}

		private RestRequest RequestBasis(string loginType,Method method=Method.POST)
		{
			var request = new RestRequest(loginType,method);
			request.AddHeader("Accept", "application/json");
			if (!string.IsNullOrEmpty(_contextKey))
			{
				request.AddHeader("X-MitsContextKey", _contextKey);
			}
			request.RequestFormat = DataFormat.Json;

			return request;
		}

		public RestHandlerResult Login(string melcloudEmail, string melcloudPassword)
		{
			var result = new RestHandlerResult(_log);
			_contextKey = null; //Reset context key

			var request = RequestBasis(ClientLogin);
			request.Parameters.Clear();
			request.AddJsonBody(new { Email = melcloudEmail, Password = melcloudPassword, Language = 0, AppVersion = "1.16.1.2", Persist = "false", CaptchaResponse = "" });

			try
			{
				IRestResponse response = _client.Execute(request);
				_log.Debug(response.Content);
				result.Response = response;
			}
			catch (Exception ex)
			{
				_log.Info("Could not login to Melcloud" + ex);
				result.Success = false;
				result.Error = ex.Message;
			}

			if (result.ResponseIsOk())
			{
				_log.Debug("Got a successful response from Melcloud");

				_contextKey = result.GetContextKey();
			}
			else
			{
				_log.Debug("Other Error");
				result.Success = false;
			}

			return result;
		}

		public RestHandlerResult DoDeviceListing()
		{
			var result = new RestHandlerResult(_log);
			var request = RequestBasis(UserListDevices,Method.GET);

			try
			{
				result.Response=_client.Execute(request);
			}
			catch (Exception e)
			{
				_log.Error($"Could not fetch devices from Melcloud: {e.Message}");
				result.Error = e.Message;
			}

			if (result.ResponseIsOk())
			{
				result.Success = true;
			}
			else
			{
				if (!string.IsNullOrEmpty(result.Response.ErrorMessage))
				{
					result.Error=result.Response.ErrorMessage;
				}
			}
			return result;
		}

		

		public RestHandlerResult UpdateDevice(object jsonObject)
		{
			var result=new RestHandlerResult(_log);
			var request = RequestBasis(DeviceSetAta);
			request.AddJsonBody(jsonObject);

			try
			{
				result.Response = _client.Execute(request);
				
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				result.Error = $"Tried to save device to Melcloud but got an error: {e.Message}";
			}
			if(result.ResponseIsOk())
				result.Success = true;
			return result;
		}

		public RestHandlerResult DoDeviceGet(JToken deviceId, JToken buildingId)
		{
			var result=new RestHandlerResult(_log);
			var request = RequestBasis(DeviceGet, Method.GET);
			request.AddParameter("id", deviceId);
			request.AddParameter("buildingID", buildingId);

			try
			{
				result.Response = _client.Execute(request);
			}
			catch (Exception e)
			{
				_log.Error($"Could not fetch data for updating devices from Melcloud: {e.Message}");
				result.Error = e.Message;
			}

			if (result.ResponseIsOk())
			{
				result.Success = true;
			}
			return result;
		}

		public bool NoContext
		{
			get
			{
				if (string.IsNullOrEmpty(_contextKey))
					return true;
				return false;
			}
		}
	}
}