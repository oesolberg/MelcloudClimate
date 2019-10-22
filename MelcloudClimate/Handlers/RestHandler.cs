using System;
using System.Runtime.Remoting.Messaging;
using HSPI_MelcloudClimate.Libraries.Logs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Scheduler;

namespace HSPI_MelcloudClimate.Handlers
{
	public interface IRestHandler
	{
		bool NoContext { get; }
		RestHandlerResult DoLogin(string melcloudEmail, string melcloudPassword);
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

		private void GetNewClient()
		{
			_client = new RestClient("https://app.melcloud.com/Mitsubishi.Wifi.Client/");
		}

		private RestRequest RequestBasis(string loginType,Method method=Method.POST)
		{
			//var method = Method.POST;
			//if (loginType == DeviceGet)
			//	method = Method.GET;

			var request = new RestRequest(loginType,method);
			request.AddHeader("Accept", "application/json");
			if (!string.IsNullOrEmpty(_contextKey))
			{
				request.AddHeader("X-MitsContextKey", _contextKey);
			}
			request.RequestFormat = DataFormat.Json;

			return request;
		}

		public RestHandlerResult DoLogin(string melcloudEmail, string melcloudPassword)
		{
			var result = new RestHandlerResult(_log);
			_contextKey = null; //Reset context key

			var request = RequestBasis(ClientLogin);
			request.Parameters.Clear();
			request.AddJsonBody(new { Email = melcloudEmail, Password = melcloudPassword, Language = 0, AppVersion = "1.16.1.2", Persist = "false", CaptchaResponse = "" });

			try
			{
				//GetNewClient();
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


				//dynamic data = JsonConvert.DeserializeObject(result.ResponseContent); //Convert data
				
				//if (data.ContainsKey("ErrorId") && data.ErrorId == null)
				//{
				//	_log.Debug("Seems like a login was successful");
				//	_contextKey = data.LoginData.ContextKey;
				//	_log.Info("Successfully logged in to Melcloud");
				//	result.Success = true;
				//}
				//else
				//{
				//	_log.Debug("Username or password invalid");
				//	result.Success = false;
				//}
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
				//GetNewClient();
				result.Response=_client.Execute(request);
			}
			catch (Exception e)
			{
				_log.Error($"Could not fetch devices from Melcloud: {e.Message}");
				//Console.WriteLine(e);
				result.Error = e.Message;
			}

			if (result.ResponseIsOk())
			{
				result.Success = true;
			}
			//var request = new RestRequest(DoDeviceListing(), Method.GET);
			//request.AddHeader("Accept", "application/json");
			//request.AddHeader("X-MitsContextKey", ContextKey.ToString());
			//request.RequestFormat = DataFormat.Json;

			//var request = new RestRequest(DeviceGet, Method.GET);
			//request.AddHeader("Accept", "application/json");
			//request.AddHeader("X-MitsContextKey", _contextKey.ToString());
			//request.AddHeader("Content-Type", "application/json");
			//request.RequestFormat = DataFormat.Json;
			return result;
		}

		

		public RestHandlerResult UpdateDevice(object jsonObject)
		{
			//_log.Debug("Sending changes to cloud");



			//JsonCommand[deviceId.ToString()].HasPendingCommand = true;
			//JsonCommand[deviceId.ToString()].EffectiveFlags = 0x1F;

			//var result = _restHandler.UpdateDevice()
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
			//_client = new RestClient("https://app.melcloud.com/Mitsubishi.Wifi.Client/");
			if(result.ResponseIsOk())
				result.Success = true;
			//var request = new RestRequest("Device/SetAta", Method.POST);
			//request.AddHeader("Accept", "application/json");
			//request.AddHeader("X-MitsContextKey", ContextKey.ToString());
			//request.RequestFormat = DataFormat.Json;
			//request.AddJsonBody(JsonCommand[deviceId.ToString()].ToString());

			//IRestResponse response = _client.Execute(request);
			//_log.Debug("Tried to save: " + JsonCommand[deviceId.ToString()].ToString());
			//_log.Debug(response.Content);

			//JsonCommand[deviceId.ToString()].HasPendingCommand = false; //Reset pending changes

			return result;
		}

		public RestHandlerResult DoDeviceGet(JToken deviceId, JToken buildingId)
		{
			var result=new RestHandlerResult(_log);
			var request = RequestBasis(DeviceGet, Method.GET);

			//_client = new RestClient("https://app.melcloud.com/Mitsubishi.Wifi.Client/");
			//_log.Debug("Updating devices");
			//req = requests.get("https://app.melcloud.com/Mitsubishi.Wifi.Client/Device/Get", headers = { 'X-MitsContextKey': self._authentication.getContextKey()}, data = { 'id': self._deviceid, 'buildingID': self._buildingid})
			//{ "id": "112833", "buildingID": "57359"}
			//var request = new RestRequest("Device/Get", Method.GET);
			//request.AddHeader("Accept", "application/json");
			//request.AddHeader("X-MitsContextKey", ContextKey.ToString());
			//request.AddHeader("Content-Type", "application/json");
			//request.RequestFormat = DataFormat.Json;
			//request.Parameters.Clear();
			//dynamic RequestBody = new JObject();
			//RequestBody.id = deviceId;
			//RequestBody.buildingID = buildingId;

			request.AddParameter("id", deviceId);
			request.AddParameter("buildingID", buildingId);

			try
			{
				//GetNewClient();
				result.Response = _client.Execute(request);
			}
			catch (Exception e)
			{
				_log.Error($"Could not fetch data for updating devices from Melcloud: {e.Message}");
				//Console.WriteLine(e);
				result.Error = e.Message;
			}

			if (result.ResponseIsOk())
			{
				result.Success = true;
			}
			//IRestResponse response = _client.Execute(request);

			//_log.Debug(response.Content);
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

	public class RestHandlerResult
	{
		private ILog _log;
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
			else
			{
				_log.Debug("Username or password invalid");
				Success = false;
				return string.Empty;
			}
		}
	}
}