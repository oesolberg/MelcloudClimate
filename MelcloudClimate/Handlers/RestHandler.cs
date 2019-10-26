using System;
using System.Net;
using HSPI_MelcloudClimate.Libraries.Logs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace HSPI_MelcloudClimate.Handlers
{
	public interface IRestHandler
	{
		bool NoContext { get; }
		string ErrorMessage { get; }
		int ErrorId { get; }
		bool IsConnected { get; }
		bool IsLoggedIn { get; }
		bool WrongUsernamePassword { get; }
		RestHandlerResult Login(string melcloudEmail, string melcloudPassword);
		RestHandlerResult DoDeviceListing();
		RestHandlerResult UpdateDevice(object jsonObject);
		RestHandlerResult DoDeviceGet(JToken deviceId, JToken buildingId);
		void RemoveContext();
	}

	public class RestHandler : IRestHandler
	{
		private readonly RestClient _client = new RestClient("https://app.melcloud.com/Mitsubishi.Wifi.Client/");
		private readonly ILog _log;
		private readonly string ClientLogin = "Login/ClientLogin";
		private readonly string DeviceGet = "Device/Get";
		private readonly string DeviceSetAta = "Device/SetAta";
		private readonly string UserListDevices = "User/ListDevices";
		private string _contextKey;
		private int _httpStatusCode;
		private bool _isLoggedIn;

		public RestHandler(ILog log)
		{
			_log = log;
		}

		public bool IsLoggedIn => _isLoggedIn;

		public RestHandlerResult Login(string melcloudEmail, string melcloudPassword)
		{
			var result = new RestHandlerResult(_log);
			_contextKey = null; //Reset context key
			_httpStatusCode = 0;
			var request = RequestBasis(ClientLogin);
			request.Parameters.Clear();
			request.AddJsonBody(new
			{
				Email = melcloudEmail,
				Password = melcloudPassword,
				Language = 0,
				AppVersion = "1.16.1.2",
				Persist = "false",
				CaptchaResponse = ""
			});
			IRestResponse response = null;
			try
			{
				response = _client.Execute(request);
				_log.Debug(response.Content);
				result.Response = response;
				IsConnected = response.IsSuccessful;
			}
			catch (Exception ex)
			{
				_log.Info("Could not login to Melcloud" + ex);
				result.Success = false;
				IsConnected = false;
				result.Error = ex.Message;
			}

			if (ResponseIsOk(response))
			{
				_log.Debug("Got a successful response from Melcloud");

				CheckForLoginErrorAndGetContext(response);
				result.Success = true;
				result.WrongUsernameOrPassword = WrongUsernamePassword;
			}

			return result;
		}

		public RestHandlerResult DoDeviceListing()
		{
			var result = new RestHandlerResult(_log);
			var request = RequestBasis(UserListDevices, Method.GET);

			try
			{
				result.Response = _client.Execute(request);
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
				if (!string.IsNullOrEmpty(result.Response.ErrorMessage)) result.Error = result.Response.ErrorMessage;
			}

			return result;
		}

		public RestHandlerResult UpdateDevice(object jsonObject)
		{
			var result = new RestHandlerResult(_log);
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

			if (result.ResponseIsOk())
				result.Success = true;
			return result;
		}

		public RestHandlerResult DoDeviceGet(JToken deviceId, JToken buildingId)
		{
			var result = new RestHandlerResult(_log);
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

			if (result.ResponseIsOk()) result.Success = true;
			return result;
		}

		public void RemoveContext()
		{
			_contextKey = null;
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

		public bool IsConnected { get; private set; }

		public bool WrongUsernamePassword { get; private set; }

		public string ErrorMessage { get; private set; }

		public int ErrorId { get; private set; }

		private RestRequest RequestBasis(string loginType, Method method = Method.POST)
		{
			var request = new RestRequest(loginType, method);
			request.AddHeader("Accept", "application/json");
			if (!string.IsNullOrEmpty(_contextKey)) request.AddHeader("X-MitsContextKey", _contextKey);
			request.RequestFormat = DataFormat.Json;

			return request;
		}

		private void CheckForLoginErrorAndGetContext(IRestResponse response)
		{
			dynamic data = JsonConvert.DeserializeObject(response.Content); //Convert data
			_isLoggedIn = false;

			if (data.ContainsKey("ErrorId") && data.ErrorId == null)
			{
				WrongUsernamePassword = false;
				_log.Debug("Seems like a login was successful");
				_log.Info("Successfully logged in to Melcloud");
				if (data.ContainsKey("LoginData") && data.LoginData.ContainsKey("ContextKey"))
				{
					_isLoggedIn = true;
					_contextKey = data.LoginData.ContextKey;
				}
			}
			else if (data.ContainsKey("ErrorId") && data.ErrorId == 1)
			{
				var reason = "Wrong username and/or password";
				_log.Debug(reason);
				WrongUsernamePassword = true;
				SetErrorIdAndErrorMessageIfFound(data, reason);
			}
			else
			{
				_log.Debug("Something other than username and password failed during login to Melcloud");
				SetErrorIdAndErrorMessageIfFound(data);
			}
		}

		private void SetErrorIdAndErrorMessageIfFound(dynamic data, string reason = null)
		{
			SetErrorMessageIfFound(data, reason);
			SetErrorIdIfFound(data);
		}

		private void SetErrorMessageIfFound(dynamic data, string reason = null)
		{
			if (data.ContainsKey("ErrorMessage"))
			{
				ErrorMessage = data.ErrorMessage;
				if (string.IsNullOrEmpty(ErrorMessage) && !string.IsNullOrEmpty(reason)) ErrorMessage = reason;
			}
		}

		private void SetErrorIdIfFound(dynamic data)
		{
			if (data.ContainsKey("ErrorId"))
				ErrorId = (int)data.ErrorId;
		}

		private bool ResponseIsOk(IRestResponse response)
		{
			if (response == null) return false;
			_httpStatusCode = (int)response.StatusCode;
			if (response.StatusCode != HttpStatusCode.OK) return false;
			return true;
		}
	}
}