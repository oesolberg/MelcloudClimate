using Hspi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Threading.Tasks;
using HSPI_MelcloudClimate.Libraries.Devices;
using System.Collections.Generic;
using HomeSeerAPI;
using HSPI_MelcloudClimate.ConfigPage;
using HSPI_MelcloudClimate.Libraries;
using HSPI_MelcloudClimate.Libraries.Logs;
using HSPI_MelcloudClimate.Libraries.Settings;

namespace HSPI_MelcloudClimate
{
	// ReSharper disable once InconsistentNaming
	public class HSPI : HspiBase2
	{
		protected string Location = "MelcloudClimate";
		protected string Location2 = "MelcloudClimate";
		//protected string MelcloudEmail = "YOUREMAILHEREFORMELCLOUD";
		//protected string MelcloudPassword = "YOURPASSWORDHEREFORMELCLOUD";
		private dynamic ContextKey;
		//private System.Collections.Generic.List<Device> ClimateDevices;
		private dynamic ClimateDevices = new Dictionary<string, Dictionary<string, Device>>();
		private dynamic JsonCommand = new Dictionary<string, JObject>();
		private static System.Timers.Timer timer;
		private object pedData = 0;
		private RestClient client = new RestClient("https://app.melcloud.com/Mitsubishi.Wifi.Client/");
		private Log Log;
		public static bool bShutDown = false;
		private Setting _settings;
		private GeneralConfig _config;

		public Log.LogType LOG_TYPE_INFO { get; private set; }
		public Log.LogType LOG_TYPE_ERROR { get; private set; }

		protected override string GetName()
		{
			return MelCloudPluginName;
		}

		public string MelCloudPluginName => "MelcloudClimate";

		protected override bool GetHscomPort()
		{
			return false; //No com port
		}

		public string GetPagePlugin(string page, string user, int userRights, string queryString)
		{
			return _config.GetPagePlugin(page, user, userRights, queryString);
		}

		public string PostBackProc(string page, string data, string user, int userRights)
		{
			return _config.PostBackProc(page, data, user, userRights);
		}

		public override void SetIOMulti(List<HomeSeerAPI.CAPI.CAPIControl> colSend)
		{
			foreach (CAPI.CAPIControl CC in colSend)
			{


				Console.WriteLine(JsonCommand["119788"].ToString());
				Console.WriteLine("SetIOMulti set value: " + CC.ControlValue.ToString() + "->ref:" + CC.Ref.ToString());

				//Get the device that did the request

				HS.SetDeviceValueByRef(CC.Ref, CC.ControlValue, false);

				var device = (Scheduler.Classes.DeviceClass)HS.GetDeviceByRef(CC.Ref);


				//This is a parent device, use its builtin device id
				var ped = (PlugExtraData.clsPlugExtraData)device.get_PlugExtraData_Get(HS);
				pedData = ped.GetNamed("DeviceId");
				var pedType = ped.GetNamed("Type");


				if (pedType.ToString() == "Target")
					SetTemperature(Convert.ToInt32(pedData), Convert.ToInt32(CC.ControlValue));
				else if (pedType.ToString() == "State")
				{
					if (CC.ControlValue == 1) //Turn on ac
						PowerOn(Convert.ToInt32(pedData));
					else if (CC.ControlValue == 0) //Turn off ac
						PowerOff(Convert.ToInt32(pedData));
				}
				else if (pedType.ToString() == "OperationMode")
					SetOperationMode(Convert.ToInt32(pedData), Convert.ToInt32(CC.ControlValue));
				else if (pedType.ToString() == "FanSpeedDevice")
					SetFanSpeed(Convert.ToInt32(pedData), Convert.ToInt32(CC.ControlValue));



			}
		}


		public override string InitIO(string port)
		{
			Log = new Log(HS);

			Log.Write("Starting plugin", LOG_TYPE_INFO);

			_config = new GeneralConfig(HS,Callback, MelCloudPluginName);
			_config.Register();

			_settings = new Setting(HS);
			_settings.DoInifileTemplateIfFileMissing();



			try
			{
				Login(); //Login to the system
				Task.Run((Action)RunApplication);
			}
			catch (Exception ex)
			{
				//bShutDown = true;
				Shutdown = true;
				return "Error on InitIO: " + ex.Message;
			}

			Shutdown = false;
			return "";
			// debug

		}

		public override void ShutdownIO()
		{
			// do your shutdown stuff here
			Console.WriteLine("Shutting down plugin");
			Shutdown = true;
			// setting this flag will cause the plugin to disconnect immediately from HomeSeer
		}


		//Login to Melcloud, if context is set, then reset the key
		private void Login()
		{
			Log.Info("Logging in to melcloud");

			ContextKey = null; //Reset context key
			IRestResponse response = null; //Reset response if it would be set



			var melcloudEmail = _settings.GetEmail();
			//_settings.SetPassword("1Gangtil");
			var melcloudPassword = _settings.GetPassword();

			try
			{
				var request = new RestRequest("Login/ClientLogin", Method.POST);
				request.AddHeader("Accept", "application/json");
				request.RequestFormat = DataFormat.Json;
				request.Parameters.Clear();
				request.AddJsonBody(new { Email = melcloudEmail, Password = melcloudPassword, Language = 0, AppVersion = "1.16.1.2", Persist = "false", CaptchaResponse = "" });
				response = client.Execute(request);
			}
			catch (Exception ex)
			{
				Log.Info("Could not login to Melcloud" + ex);
			}

			if ((int)response.StatusCode == 200)
			{
				Log.Debug("Got a successful response from melcloud");
				dynamic data = JsonConvert.DeserializeObject(response.Content); //Convert data

				if (data.ContainsKey("ErrorId") && data.ErrorId == null)
				{
					Log.Debug("Seems like a login was successful");
					ContextKey = data.LoginData.ContextKey;
					Log.Info("Successfully logged in to Melcloud");
				}
				else
				{
					Log.Debug("Username or password invalid");
					throw new Exception("Username or password invalid");
				}
			}
			else
			{
				Log.Debug("Other Error");
				throw new Exception("Other Error");
			}


		}
		private void RunApplication()
		{



			Log.Debug("Running Application Task");

			try
			{
				GetDevices();
				RefreshDevices();

				Log.Debug("Starting a loop timer");
				timer = new System.Timers.Timer();
				timer.Interval = 70000;

				timer.Elapsed += OnTimedEvent;
				timer.AutoReset = true;
				timer.Enabled = true;

			}
			catch (Exception ex)
			{
				//bShutDown = true;
				Shutdown = true;
			}
		}

		private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
		{
			Console.WriteLine("Raised: {0}", e.SignalTime);


			//Periodical run system check
			//First check if its local changes to save to cloud
			foreach (KeyValuePair<string, JObject> pair in JsonCommand)
			{
				if (JsonCommand[pair.Key.ToString()].HasPendingCommand == true)
					SaveToCloud(Convert.ToInt32(pair.Key));
				else
					RefreshDevices();

			}


		}

		private void CreateMelcloudDevice(dynamic device)
		{
			string deviceId = device.DeviceID.ToString();

			ClimateDevices.Add(deviceId, new Dictionary<String, Device>());

			JsonCommand.Add(deviceId, new JObject()); //Create a new object

			JsonCommand[deviceId].DeviceID = deviceId;
			JsonCommand[deviceId].BuildingId = device.BuildingID;
			JsonCommand[deviceId].HasPendingCommand = false;


			double powerState = 0;
			if (device.Power == true)
				powerState = 1;


			Device RootDevice = new Device(HS)
			{
				Name = device.DeviceName,
				Unique = deviceId

			}
					.addPED("DeviceId", deviceId)
					.addPED("Type", "State")
					.CheckAndCreate(powerState)
					.AddButton(1, "On", $"images/HomeSeer/contemporary/on.gif")
					.AddButton(0, "Off", $"images/HomeSeer/contemporary/off.gif");

			ClimateDevices[deviceId].Add("RootDevice", RootDevice);

			//Set the device to the modus picked up

			Console.WriteLine("Parent id " + deviceId);
			Device CurrentTemperatureDevice = new Device(HS, RootDevice)
			{
				Name = "Current Temperature",
				Unique = deviceId

			}
			.addPED("DeviceId", deviceId)
			.addPED("Type", "Current")
			.CheckAndCreate((double)device.Device.RoomTemperature)
			.AddStatusControlRangeField(0, 50, " " + (char)176 + "C", true, $"images/HomeSeer/contemporary/Thermometer-110.png");

			ClimateDevices[deviceId].Add("CurrentTemperatureDevice", CurrentTemperatureDevice);



			Device SetpointTemperatureDevice = new Device(HS, RootDevice)
			{
				Name = "Temperature Setpoint",
				Unique = deviceId

			}
			.addPED("DeviceId", deviceId)
			.addPED("Type", "Target")
			.CheckAndCreate((double)device.Device.SetTemperature)
					.AddDropdown(5, 35, " " + (char)176 + "C", $"images/HomeSeer/contemporary/Thermometer-110.png");

			//Get current setpoint
			JsonCommand[deviceId].SetTemperature = device.Device.SetTemperature;
			ClimateDevices[deviceId].Add("SetpointTemperatureDevice", SetpointTemperatureDevice);

			Device OperationalModeDevice = new Device(HS, RootDevice)
			{
				Name = "Operational mode",
				Unique = deviceId

			}
			.addPED("DeviceId", deviceId)
			.addPED("Type", "OperationMode")
			.CheckAndCreate((double)device.Device.OperationMode)
					.AddButton(1, "Heat", $"images/HomeSeer/contemporary/Heat.png")
					.AddButton(2, "Dry", $"images/HomeSeer/contemporary/water.gif")
					.AddButton(3, "Cool", $"images/HomeSeer/contemporary/Cool.png")
					.AddButton(7, "Fan", $"images/HomeSeer/contemporary/fan-on.png")
					.AddButton(8, "Auto", $"images/HomeSeer/contemporary/auto-mode.png");

			//Get current setpoint
			JsonCommand[deviceId].OperationMode = device.Device.OperationMode;

			ClimateDevices[deviceId].Add("OperationalModeDevice", OperationalModeDevice);

			Device FanSpeedDevice = new Device(HS, RootDevice)
			{
				Name = "Fan speed",
				Unique = deviceId

			}
		   .addPED("DeviceId", deviceId)
		   .addPED("Type", "FanSpeed")
		   .CheckAndCreate((double)device.Device.FanSpeed)
				  .AddDropdown(0, (int)device.Device.NumberOfFanSpeeds, null, $"images/HomeSeer/contemporary/fan-on.png");

			//Get current setpoint
			// JsonCommand[device.DeviceID.ToString()].FanSpeed = device.Device.FanSpeed;

			ClimateDevices[deviceId].Add("FanSpeedDevice", FanSpeedDevice);


		}

		private bool PowerOn(int DeviceId)
		{
			JsonCommand[DeviceId.ToString()].Power = true;
			Console.WriteLine("Queued turn on aircon");
			JsonCommand[DeviceId.ToString()].HasPendingCommand = true;
			return true;
		}


		private bool SetOperationMode(int DeviceId, int operationMode)
		{
			JsonCommand[DeviceId.ToString()].OperationMode = operationMode;
			Console.WriteLine("Setting operational mode to: " + operationMode);
			JsonCommand[DeviceId.ToString()].HasPendingCommand = true;
			return true;
		}


		private bool SetTemperature(int DeviceId, int target)
		{
			JsonCommand[DeviceId.ToString()].SetTemperature = target;
			Console.WriteLine("Setting temperature to: " + target);
			JsonCommand[DeviceId.ToString()].HasPendingCommand = true;
			return true;
		}

		private bool SetFanSpeed(int DeviceId, int target)
		{
			JsonCommand[DeviceId.ToString()].SetFanSpeed = target;
			Console.WriteLine("Setting fan speed to: " + target);
			JsonCommand[DeviceId.ToString()].HasPendingCommand = true;
			return true;
		}


		private bool PowerOff(int DeviceId)
		{
			JsonCommand[DeviceId.ToString()].Power = false;
			Console.WriteLine("Queued turn off aircon");
			JsonCommand[DeviceId.ToString()].HasPendingCommand = true;
			return true;
		}


		private bool RefreshDevices()
		{

			//Temporary copy the json

			//Refresh all devices 
			foreach (KeyValuePair<string, JObject> pair in JsonCommand)
			{
				if (JsonCommand[pair.Key.ToString()].HasPendingCommand == true)
				{
					Log.Debug("Waiting for changes, abort this update");
					break;
				}


				client = new RestClient("https://app.melcloud.com/Mitsubishi.Wifi.Client/");
				Console.WriteLine("Updating devices");
				//req = requests.get("https://app.melcloud.com/Mitsubishi.Wifi.Client/Device/Get", headers = { 'X-MitsContextKey': self._authentication.getContextKey()}, data = { 'id': self._deviceid, 'buildingID': self._buildingid})
				//{ "id": "112833", "buildingID": "57359"}
				var request = new RestRequest("Device/Get", Method.GET);
				request.AddHeader("Accept", "application/json");
				request.AddHeader("X-MitsContextKey", ContextKey.ToString());
				request.AddHeader("Content-Type", "application/json");
				request.RequestFormat = DataFormat.Json;
				//request.Parameters.Clear();
				dynamic RequestBody = new JObject();
				RequestBody.id = pair.Value.GetValue("DeviceID");
				RequestBody.buildingID = pair.Value.GetValue("BuildingId");

				request.AddParameter("id", pair.Value.GetValue("DeviceID"));
				request.AddParameter("buildingID", pair.Value.GetValue("BuildingId"));

				IRestResponse response = client.Execute(request);

				dynamic deviceResponse = JObject.Parse(response.Content);

				//Update fields


				//Check for changes
				if (JsonCommand[pair.Key.ToString()].ContainsKey("EffectiveFlags") &&
					JsonCommand[pair.Key.ToString()].EffectiveFlags != deviceResponse.EffectiveFlags)
				{
					JsonCommand[pair.Key.ToString()].EffectiveFlags = deviceResponse.EffectiveFlags;
				}



				if (JsonCommand[pair.Key.ToString()].ContainsKey("RoomTemperature") &&
					JsonCommand[pair.Key.ToString()].RoomTemperature != deviceResponse.RoomTemperature)
				{
					JsonCommand[pair.Key.ToString()].RoomTemperature = deviceResponse.RoomTemperature;
					ClimateDevices[pair.Key.ToString()]["CurrentTemperatureDevice"].SetValue((double)deviceResponse.RoomTemperature);
				}


				if (JsonCommand[pair.Key.ToString()].ContainsKey("SetTemperature") &&
					JsonCommand[pair.Key.ToString()].SetTemperature != deviceResponse.SetTemperature)
				{
					JsonCommand[pair.Key.ToString()].SetTemperature = deviceResponse.SetTemperature;
					ClimateDevices[pair.Key.ToString()]["SetpointTemperatureDevice"].SetValue((double)deviceResponse.SetTemperature);
				}
				else if (JsonCommand[pair.Key.ToString()].ContainsKey("SetTemperature") == false)
				{
					JsonCommand[pair.Key.ToString()].SetTemperature = deviceResponse.SetTemperature;
					ClimateDevices[pair.Key.ToString()]["SetpointTemperatureDevice"].SetValue((double)deviceResponse.SetTemperature);
				}


				if (JsonCommand[pair.Key.ToString()].ContainsKey("SetFanSpeed") &&
					JsonCommand[pair.Key.ToString()].SetFanSpeed != deviceResponse.v)
				{
					JsonCommand[pair.Key.ToString()].SetFanSpeed = deviceResponse.SetFanSpeed;
					ClimateDevices[pair.Key.ToString()]["FanSpeedDevice"].SetValue((double)deviceResponse.SetFanSpeed);
				}
				else if (JsonCommand[pair.Key.ToString()].ContainsKey("SetFanSpeed") == false)
				{
					JsonCommand[pair.Key.ToString()].SetFanSpeed = deviceResponse.SetFanSpeed;
					ClimateDevices[pair.Key.ToString()]["FanSpeedDevice"].SetValue((double)deviceResponse.SetFanSpeed);
				}

				if (JsonCommand[pair.Key.ToString()].ContainsKey("OperationMode") &&
					JsonCommand[pair.Key.ToString()].OperationMode != deviceResponse.OperationMode)
				{
					JsonCommand[pair.Key.ToString()].OperationMode = deviceResponse.OperationMode;
					ClimateDevices[pair.Key.ToString()]["OperationalModeDevice"].SetValue((double)deviceResponse.OperationMode);
				}


				if (JsonCommand[pair.Key.ToString()].ContainsKey("VaneHorizontal") &&
					JsonCommand[pair.Key.ToString()].VaneHorizontal != deviceResponse.VaneHorizontal)
					JsonCommand[pair.Key.ToString()].VaneHorizontal = deviceResponse.VaneHorizontal;

				if (JsonCommand[pair.Key.ToString()].ContainsKey("VaneVertical") &&
					JsonCommand[pair.Key.ToString()].VaneVertical != deviceResponse.VaneVertical)
					JsonCommand[pair.Key.ToString()].VaneVertical = deviceResponse.VaneVertical;

				if (JsonCommand[pair.Key.ToString()].ContainsKey("NumberOfFanSpeeds") &&
					JsonCommand[pair.Key.ToString()].NumberOfFanSpeeds != deviceResponse.NumberOfFanSpeeds)
					JsonCommand[pair.Key.ToString()].NumberOfFanSpeeds = deviceResponse.NumberOfFanSpeeds;

				if (JsonCommand[pair.Key.ToString()].ContainsKey("DefaultHeatingSetTemperature") &&
					JsonCommand[pair.Key.ToString()].DefaultHeatingSetTemperature != deviceResponse.DefaultHeatingSetTemperature)
					JsonCommand[pair.Key.ToString()].DefaultHeatingSetTemperature = deviceResponse.DefaultHeatingSetTemperature;

				if (JsonCommand[pair.Key.ToString()].ContainsKey("DefaultCoolingSetTemperature") &&
					JsonCommand[pair.Key.ToString()].DefaultCoolingSetTemperature != deviceResponse.DefaultCoolingSetTemperature)
					JsonCommand[pair.Key.ToString()].DefaultCoolingSetTemperature = deviceResponse.DefaultCoolingSetTemperature;

				if (JsonCommand[pair.Key.ToString()].ContainsKey("InStandbyMode") &&
				   JsonCommand[pair.Key.ToString()].InStandbyMode != deviceResponse.InStandbyMode)
					JsonCommand[pair.Key.ToString()].InStandbyMode = deviceResponse.InStandbyMode;

				if (JsonCommand[pair.Key.ToString()].ContainsKey("Power") &&
				   JsonCommand[pair.Key.ToString()].Power != deviceResponse.Power)
				{
					JsonCommand[pair.Key.ToString()].Power = deviceResponse.Power;
					ClimateDevices[pair.Key.ToString()]["RootDevice"].SetValue((double)deviceResponse.Power);
				}

				if (JsonCommand[pair.Key.ToString()].ContainsKey("Power") &&
					JsonCommand[pair.Key.ToString()].Power != deviceResponse.Power)
				{
					if (deviceResponse.Power == true)
					{
						JsonCommand[pair.Key.ToString()].Power = true;
						ClimateDevices[pair.Key.ToString()]["RootDevice"].SetValue((double)1);
					}
					else
					{
						JsonCommand[pair.Key.ToString()].Power = false;
						ClimateDevices[pair.Key.ToString()]["RootDevice"].SetValue((double)0);

					}

				}
				else if (JsonCommand[pair.Key.ToString()].ContainsKey("Power") == false)
				{
					if (deviceResponse.Power == true)
					{
						JsonCommand[pair.Key.ToString()].Power = true;
						ClimateDevices[pair.Key.ToString()]["RootDevice"].SetValue((double)1);
					}
					else
					{
						JsonCommand[pair.Key.ToString()].Power = false;
						ClimateDevices[pair.Key.ToString()]["RootDevice"].SetValue((double)0);

					}
				}


				if (JsonCommand[pair.Key.ToString()].ContainsKey("HasPendingCommand") &&
				   JsonCommand[pair.Key.ToString()].HasPendingCommand != deviceResponse.HasPendingCommand)
					JsonCommand[pair.Key.ToString()].HasPendingCommand = false;
			}
			return true;

		}

		private bool SaveToCloud(int deviceId)
		{
			//Save all changes to the cloud

			Console.WriteLine("Sending changes to cloud");



			JsonCommand[deviceId.ToString()].HasPendingCommand = true;
			JsonCommand[deviceId.ToString()].EffectiveFlags = 0x1F;

			//TODO share this
			client = new RestClient("https://app.melcloud.com/Mitsubishi.Wifi.Client/");

			var request = new RestRequest("Device/SetAta", Method.POST);
			request.AddHeader("Accept", "application/json");
			request.AddHeader("X-MitsContextKey", ContextKey.ToString());
			request.RequestFormat = DataFormat.Json;
			request.AddJsonBody(JsonCommand[deviceId.ToString()].ToString());

			IRestResponse response = client.Execute(request);
			Console.WriteLine("Tried to save: " + JsonCommand[deviceId.ToString()].ToString());
			Console.WriteLine(response.Content);

			JsonCommand[deviceId.ToString()].HasPendingCommand = false; //Reset pending changes

			return true;
		}

		private void GetDevices(int retry = 0)
		{
			Log.Info("Fetching Devies from MelCloud");

			if (retry > 1)
			{
				Log.Error("Could not get devices after trying two times");
				throw new Exception("Could not get devices. Aborting. Check your user is active and Melcloud is working");
			}

			try
			{
				var request = new RestRequest("User/ListDevices", Method.GET);
				request.AddHeader("Accept", "application/json");
				request.AddHeader("X-MitsContextKey", ContextKey.ToString());
				request.RequestFormat = DataFormat.Json;

				IRestResponse response = client.Execute(request);

				if ((int)response.StatusCode == 200)
				{
					Log.Debug("Got a successful response from melcloud");
					dynamic data = JsonConvert.DeserializeObject(response.Content); //Convert data

					//Process all Floor devices
					Log.Debug("Process all devices pr floor");
					if (data[0].Structure.ContainsKey("Floors") && data[0].Structure.Floors.Count > 0)
					{
						for (int i = 0; i < data[0].Structure.Floors.Count; i++)
						{
							Log.Debug("A floor detected");
							for (int j = 0; j < data[0].Structure.Floors[i].Devices.Count; j++)
							{
								Log.Debug("A device detected");
								CreateMelcloudDevice(data[0].Structure.Floors[i].Devices[j]);
							}
						}
					}
					else
						Log.Debug("No floor devices found");

					Log.Debug("Process all devices pr area");
					if (data[0].Structure.ContainsKey("Areas") && data[0].Structure.Areas.Count > 0)
					{
						for (int i = 0; i < data[0].Structure.Area.Count; i++)
						{
							for (int j = 0; j < data[0].Structure.Area[i].Devices.Count; j++)
							{
								CreateMelcloudDevice(data[0].Structure.Area[i].Devices[j]);
							}
						}
					}
					else
						Log.Debug("No area devices found");

					Log.Debug("Process all devices pr clients");
					if (data[0].Structure.ContainsKey("Clients") && data[0].Structure.Clients.Count > 0)
					{
						for (int i = 0; i < data[0].Structure.Clients.Count; i++)
						{
							for (int j = 0; j < data[0].Structure.Clients[i].Devices.Count; j++)
							{
								CreateMelcloudDevice(data[0].Structure.Clients[i].Devices[j]);
							}
						}
					}
					else
						Log.Debug("No client devices found");

					Log.Debug("Process all devices pr devices");
					if (data[0].Structure.ContainsKey("Devices") && data[0].Structure.Devices.Count > 0)
					{
						for (int i = 0; i < data[0].Structure.Devices.Count; i++)
						{
							//Create if we have multiple devices
							if (data[0].Structure.Devices[i].Devices != null &&
								data[0].Structure.Devices[i].Devices.Count > 0)
							{
								for (int j = 0; j < data[0].Structure.Devices[i].Devices.Count; j++)
								{
									CreateMelcloudDevice(data[0].Structure.Devices[i].Devices[j]);
								}
							}
							//Create if we have a single device
							if (data[0].Structure.Devices[i].Devices == null &&
								data[0].Structure.Devices[i].Device != null)
							{
								CreateMelcloudDevice(data[0].Structure.Devices[i]);
							}


						}
					}
					else
						Log.Debug("No client devices found");

				}
				else
				{
					if (retry <= 1)
					{
						//Give it another chance - try to login first
						Login();
						GetDevices(retry += 1); //Raise attempt with one

					}
					else
					{
						Log.Debug("No access, giving up");
						Shutdown = true; //Shutdown plugin - should not run after this error
						throw new Exception("No access, giving up");

					}
				}


			}
			catch (Exception ex)
			{
				Log.Info("Could not connect to Melcloud for fetching devices" + ex);
			}


		}


	}
}