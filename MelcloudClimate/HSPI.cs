﻿using Hspi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Threading.Tasks;
using HSPI_MelcloudClimate.Libraries.Devices;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using HomeSeerAPI;
using HSPI_MelcloudClimate.Common;
using HSPI_MelcloudClimate.ConfigPage;
using HSPI_MelcloudClimate.Handlers;
using HSPI_MelcloudClimate.Libraries;
using HSPI_MelcloudClimate.Libraries.Logs;
using HSPI_MelcloudClimate.Libraries.Settings;

namespace HSPI_MelcloudClimate
{
	// ReSharper disable once InconsistentNaming
	public class HSPI : HspiBase
	{
		protected string Location = "MelcloudClimate";
		protected string Location2 = "MelcloudClimate";
		private dynamic ContextKey;
		//private System.Collections.Generic.List<Device> ClimateDevices;
		private dynamic ClimateDevices = new Dictionary<string, Dictionary<string, Device>>();
		private dynamic JsonCommand = new Dictionary<string, JObject>();
		private static System.Timers.Timer _timer;
		private object pedData = 0;
		private RestClient _client = new RestClient("https://app.melcloud.com/Mitsubishi.Wifi.Client/");
		private ILog _log;
		public static bool bShutDown = false;
		private Setting _settings;
		private GeneralConfig _config;
		private IIniSettings _iniSettings;
		private IRestHandler _restHandler;
		private static readonly object LockObject = new object();

		protected override string GetName()
		{
			return Utility.PluginName;
		}

		protected override bool GetHscomPort()
		{
			return false; //No com port
		}

		public override string GetPagePlugin(string page, string user, int userRights, string queryString)
		{
			return _config.GetPagePlugin(page, user, userRights, queryString);
		}

		public override string PostBackProc(string page, string data, string user, int userRights)
		{
			return _config.PostBackProc(page, data, user, userRights);
		}

		public override void SetIOMulti(List<HomeSeerAPI.CAPI.CAPIControl> colSend)
		{
			foreach (CAPI.CAPIControl CC in colSend)
			{
				_log.Debug("SetIOMulti set value: " + CC.ControlValue.ToString() + "->ref:" + CC.Ref.ToString());

				//Get the device that did the request

				HS.SetDeviceValueByRef(CC.Ref, CC.ControlValue, false);

				var device = (Scheduler.Classes.DeviceClass)HS.GetDeviceByRef(CC.Ref);


				//This is a parent device, use its builtin device id
				var ped = (PlugExtraData.clsPlugExtraData)device.get_PlugExtraData_Get(HS);
				pedData = ped.GetNamed("DeviceIdKey");
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
				else if (pedType.ToString() == Constants.FanSpeed)
					SetFanSpeed(Convert.ToInt32(pedData), Convert.ToInt32(CC.ControlValue));



			}
		}


		public override string InitIO(string port)
		{
			_iniSettings = new IniSettings(HS);

			_iniSettings.IniSettingsChanged += IniSettingsChanged;

			_log = new Log(HS, _iniSettings);

			_log.Info("Starting plugin");
			_config = new GeneralConfig("MelCloud_General_Config", HS, Callback, _iniSettings, _log);
			_config.Register();

			_settings = new Setting(HS);
			_settings.DoIniFileTemplateIfFileMissing();

			_restHandler = new RestHandler(_log);

			

			StartLoginAndDataFetchingInNewThread();

			

			Shutdown = false;
			return "";
		}

		private void IniSettingsChanged(object sender, EventArgs eventArgs)
		{
			//Reset timer
			_timer.Close();

			//Login with username and password
			Login();
			if (!_restHandler.NoContext)
			{
				SetTimer();
			}
		}

		private void StartLoginAndDataFetchingInNewThread()
		{
			//Run fetching of first data if we can

			//_workThread = new Thread(DoWork) { Name = GetPortAsString() };
			//_workThread.Start();
			try
			{
				//if (!Debugger.IsAttached)//Added to not run application when debugging 
				{
					Login(); //Login to the system
					if (_restHandler.NoContext)
					{
						SetConnectedToFalse();
					}
					var runningTask = Task.Run((Action)RunApplication);
				}
			}
			catch (Exception ex)
			{
				//bShutDown = true;
				_log.Error($"Error on InitIO: {ex.Message}");
				Shutdown = true;
				return;
				//return "Error on InitIO: " + ex.Message;
			}
			SetTimer();
		}

		private void SetConnectedToFalse()
		{
			foreach (KeyValuePair<string, JObject> pair in JsonCommand)
			{
				Device connectionDevice = ClimateDevices[pair.Key.ToString()][Constants.Connection];
				connectionDevice.SetValue((double)0).SetText("Not connected");
				//.SetText;
			}
		}

		private void SetTimer()
		{
			//Setting timer 
			_log.Debug($"Setting timer with {_iniSettings.CheckMelCloudTimerInterval} seconds interval");
			_timer = new System.Timers.Timer();
			_timer.Interval = _iniSettings.CheckMelCloudTimerIntervalInMilliseconds;
			_timer.Elapsed += OnTimedEvent;
			_timer.AutoReset = true;
			_timer.Enabled = true;
		}


		public override void ShutdownIO()
		{
			// do your shutdown stuff here
			_log.Info($"Shutting down plugin {Utility.PluginName}");
			Shutdown = true;
			// setting this flag will cause the plugin to disconnect immediately from HomeSeer
		}


		//Login to Melcloud, if context is set, then reset the key
		private void Login()
		{
			_log.Info("Trying to log in to melcloud");

			if (!_iniSettings.PasswordAndUsernameOk())
			{
				return;
			}

			var result = _restHandler.Login(_iniSettings.UserNameMelCloud, _iniSettings.PasswordMelCloud);

			if (result.Success)
			{
				dynamic data = JsonConvert.DeserializeObject(result.ResponseContent); //Convert data

				if (data.ContainsKey("ErrorId") && data.ErrorId == null)
				{
					_log.Debug("Seems like a login was successful");
					ContextKey = data.LoginData.ContextKey;
					_log.Info("Successfully logged in to Melcloud");
				}
				else
				{
					_log.Debug("Username or password invalid");

				}
			}
			else
			{
				_log.Info($"Could not login to Melcloud : {result.Error}");
			}

		}

		private void RunApplication()
		{
			_log.Debug("Running Application Task");

			try
			{
				GetDevices();
				RefreshDevices();

				_log.Debug("Starting a loop timer");

			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
				//bShutDown = true;
				Shutdown = true;
			}
		}

		private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
		{
			_log.Debug($"Raised: {e.SignalTime}");

			if (_restHandler.NoContext) return;

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

			Device ConnectedRootDevice = new Device(HS)
			{
				Name = device.DeviceName,
				Unique = deviceId
			}
			.AddPED(Constants.DeviceIdKey, deviceId)
			.AddPED(Constants.TypeKey, Constants.Connection)
			.CheckAndCreate()
			.AddStatusGraphicField(0, -1, "images/MelcloudClimate/notConnected.png")
			.AddStatusGraphicField(1, -1, "images/MelcloudClimate/Connected.png")
			.SetText("Connected")
			.SetValue(1);

			ClimateDevices[deviceId].Add(Constants.Connection, ConnectedRootDevice);

			Device powerDevice = new Device(HS, ConnectedRootDevice)
			{
				Name = "Power",
				Unique = deviceId

			}
			.AddPED("DeviceIdKey", deviceId)
			.AddPED("Type", "State")

			.CheckAndCreate(powerState)
			.AddButton(1, "On", $"images/HomeSeer/contemporary/on.gif")
			.AddButton(0, "Off", $"images/HomeSeer/contemporary/off.gif");

			ClimateDevices[deviceId].Add(Constants.PowerDevice, powerDevice);

			//Set the device to the modus picked up

			_log.Debug("Parent id " + deviceId);
			Device currentTemperatureDevice = new Device(HS, ConnectedRootDevice)
			{
				Name = "Current Temperature",
				Unique = deviceId

			}

			.AddPED("DeviceIdKey", deviceId)
			.AddPED("Type", "Current")
			.CheckAndCreate((double)device.Device.RoomTemperature)
			.AddStatusControlRangeField(0, 50, " " + (char)176 + "C", true, $"images/HomeSeer/contemporary/Thermometer-110.png");

			ClimateDevices[deviceId].Add("CurrentTemperatureDevice", currentTemperatureDevice);



			Device setpointTemperatureDevice = new Device(HS, ConnectedRootDevice)
			{
				Name = "Temperature Setpoint",
				Unique = deviceId

			}
			.AddPED("DeviceIdKey", deviceId)
			.AddPED("Type", "Target")
			.CheckAndCreate((double)device.Device.SetTemperature)
					.AddDropdown(5, 35, " " + (char)176 + "C", $"images/HomeSeer/contemporary/Thermometer-110.png");

			//Get current setpoint
			JsonCommand[deviceId].SetTemperature = device.Device.SetTemperature;
			ClimateDevices[deviceId].Add("SetpointTemperatureDevice", setpointTemperatureDevice);

			Device operationalModeDevice = new Device(HS, ConnectedRootDevice)
			{
				Name = "Operational mode",
				Unique = deviceId

			}
			.AddPED("DeviceIdKey", deviceId)
			.AddPED("Type", "OperationMode")
			.CheckAndCreate((double)device.Device.OperationMode)
					.AddButton(1, "Heat", $"images/HomeSeer/contemporary/Heat.png")
					.AddButton(2, "Dry", $"images/HomeSeer/contemporary/water.gif")
					.AddButton(3, "Cool", $"images/HomeSeer/contemporary/Cool.png")
					.AddButton(7, "Fan", $"images/HomeSeer/contemporary/fan-on.png")
					.AddButton(8, "Auto", $"images/HomeSeer/contemporary/auto-mode.png");

			//Get current setpoint
			JsonCommand[deviceId].OperationMode = device.Device.OperationMode;

			ClimateDevices[deviceId].Add("OperationalModeDevice", operationalModeDevice);

			Device fanSpeedDevice = new Device(HS, ConnectedRootDevice)
			{
				Name = "Fan speed",
				Unique = deviceId

			}
		   .AddPED("DeviceIdKey", deviceId)
		   .AddPED("Type", Constants.FanSpeed)
		   .CheckAndCreate((double)device.Device.FanSpeed)
				  .AddDropdown(0, (int)device.Device.NumberOfFanSpeeds, null, $"images/HomeSeer/contemporary/fan-on.png");

			ClimateDevices[deviceId].Add(Constants.FanSpeed, fanSpeedDevice);


		}

		private bool PowerOn(int deviceId)
		{
			JsonCommand[deviceId.ToString()].Power = true;
			_log.Debug("Queued turn on aircon");
			JsonCommand[deviceId.ToString()].HasPendingCommand = true;
			return true;
		}


		private bool SetOperationMode(int deviceId, int operationMode)
		{
			JsonCommand[deviceId.ToString()].OperationMode = operationMode;
			_log.Debug("Setting operational mode to: " + operationMode);
			JsonCommand[deviceId.ToString()].HasPendingCommand = true;
			return true;
		}


		private bool SetTemperature(int deviceId, int target)
		{
			JsonCommand[deviceId.ToString()].SetTemperature = target;
			_log.Debug("Setting temperature to: " + target);
			JsonCommand[deviceId.ToString()].HasPendingCommand = true;
			return true;
		}

		private bool SetFanSpeed(int deviceId, int target)
		{
			JsonCommand[deviceId.ToString()].SetFanSpeed = target;
			_log.Debug("Setting fan speed to: " + target);
			JsonCommand[deviceId.ToString()].HasPendingCommand = true;
			return true;
		}


		private bool PowerOff(int deviceId)
		{
			JsonCommand[deviceId.ToString()].Power = false;
			_log.Debug("Queued turn off aircon");
			JsonCommand[deviceId.ToString()].HasPendingCommand = true;
			return true;
		}


		private bool RefreshDevices()
		{
			lock (LockObject)
			{
				if (_restHandler.NoContext)
				{
					foreach (KeyValuePair<string, JObject> pair in JsonCommand)
					{
						Device connectionDevice = ClimateDevices[pair.Key.ToString()][Constants.Connection];
						connectionDevice.SetValue((double)0).SetText("Not connected");
						//.SetText;
					}
					return false;
				}
				//Temporary copy the json

				//Refresh all devices 
				foreach (KeyValuePair<string, JObject> pair in JsonCommand)
				{

					Device connectionDevice = ClimateDevices[pair.Key.ToString()][Constants.Connection];
					connectionDevice.SetValue((double)1).SetText($"Connected - {DateTime.Now.ToString("HH:mm:ss")}");

					if (JsonCommand[pair.Key.ToString()].HasPendingCommand == true)
					{
						_log.Debug("Waiting for changes, abort this update");
						break;
					}

					var deviceId = pair.Value.GetValue("DeviceID");
					var buildingId = pair.Value.GetValue("BuildingId");
					var result = _restHandler.DoDeviceGet(deviceId, buildingId);

					if (!result.Success)
					{
						continue;
					}

					dynamic deviceResponse = JObject.Parse(result.ResponseContent);

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
						ClimateDevices[pair.Key.ToString()]["CurrentTemperatureDevice"]
							.SetValue((double)deviceResponse.RoomTemperature);
					}


					if (JsonCommand[pair.Key.ToString()].ContainsKey("SetTemperature") &&
						JsonCommand[pair.Key.ToString()].SetTemperature != deviceResponse.SetTemperature)
					{
						JsonCommand[pair.Key.ToString()].SetTemperature = deviceResponse.SetTemperature;
						ClimateDevices[pair.Key.ToString()]["SetpointTemperatureDevice"]
							.SetValue((double)deviceResponse.SetTemperature);
					}
					else if (JsonCommand[pair.Key.ToString()].ContainsKey("SetTemperature") == false)
					{
						JsonCommand[pair.Key.ToString()].SetTemperature = deviceResponse.SetTemperature;
						ClimateDevices[pair.Key.ToString()]["SetpointTemperatureDevice"]
							.SetValue((double)deviceResponse.SetTemperature);
					}


					if (JsonCommand[pair.Key.ToString()].ContainsKey("SetFanSpeed") &&
						JsonCommand[pair.Key.ToString()].SetFanSpeed != deviceResponse.v)
					{
						JsonCommand[pair.Key.ToString()].SetFanSpeed = deviceResponse.SetFanSpeed;
						ClimateDevices[pair.Key.ToString()][Constants.FanSpeed]
							.SetValue((double)deviceResponse.SetFanSpeed);
					}
					else if (JsonCommand[pair.Key.ToString()].ContainsKey("SetFanSpeed") == false)
					{
						JsonCommand[pair.Key.ToString()].SetFanSpeed = deviceResponse.SetFanSpeed;
						ClimateDevices[pair.Key.ToString()][Constants.FanSpeed]
							.SetValue((double)deviceResponse.SetFanSpeed);
					}

					if (JsonCommand[pair.Key.ToString()].ContainsKey("OperationMode") &&
						JsonCommand[pair.Key.ToString()].OperationMode != deviceResponse.OperationMode)
					{
						JsonCommand[pair.Key.ToString()].OperationMode = deviceResponse.OperationMode;
						ClimateDevices[pair.Key.ToString()]["OperationalModeDevice"]
							.SetValue((double)deviceResponse.OperationMode);
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
						JsonCommand[pair.Key.ToString()].DefaultHeatingSetTemperature !=
						deviceResponse.DefaultHeatingSetTemperature)
						JsonCommand[pair.Key.ToString()].DefaultHeatingSetTemperature =
							deviceResponse.DefaultHeatingSetTemperature;

					if (JsonCommand[pair.Key.ToString()].ContainsKey("DefaultCoolingSetTemperature") &&
						JsonCommand[pair.Key.ToString()].DefaultCoolingSetTemperature !=
						deviceResponse.DefaultCoolingSetTemperature)
						JsonCommand[pair.Key.ToString()].DefaultCoolingSetTemperature =
							deviceResponse.DefaultCoolingSetTemperature;

					if (JsonCommand[pair.Key.ToString()].ContainsKey("InStandbyMode") &&
						JsonCommand[pair.Key.ToString()].InStandbyMode != deviceResponse.InStandbyMode)
						JsonCommand[pair.Key.ToString()].InStandbyMode = deviceResponse.InStandbyMode;

					if (JsonCommand[pair.Key.ToString()].ContainsKey("Power") &&
						JsonCommand[pair.Key.ToString()].Power != deviceResponse.Power)
					{
						JsonCommand[pair.Key.ToString()].Power = deviceResponse.Power;
						ClimateDevices[pair.Key.ToString()][Constants.PowerDevice].SetValue((double)deviceResponse.Power);
					}

					if (JsonCommand[pair.Key.ToString()].ContainsKey("Power") &&
						JsonCommand[pair.Key.ToString()].Power != deviceResponse.Power)
					{
						if (deviceResponse.Power == true)
						{
							JsonCommand[pair.Key.ToString()].Power = true;
							ClimateDevices[pair.Key.ToString()][Constants.PowerDevice].SetValue((double)1);
						}
						else
						{
							JsonCommand[pair.Key.ToString()].Power = false;
							ClimateDevices[pair.Key.ToString()][Constants.PowerDevice].SetValue((double)0);

						}

					}
					else if (JsonCommand[pair.Key.ToString()].ContainsKey("Power") == false)
					{
						if (deviceResponse.Power == true)
						{
							JsonCommand[pair.Key.ToString()].Power = true;
							ClimateDevices[pair.Key.ToString()][Constants.PowerDevice].SetValue((double)1);
						}
						else
						{
							JsonCommand[pair.Key.ToString()].Power = false;
							ClimateDevices[pair.Key.ToString()][Constants.PowerDevice].SetValue((double)0);

						}
					}


					if (JsonCommand[pair.Key.ToString()].ContainsKey(Constants.HasPendingCommand) &&
						JsonCommand[pair.Key.ToString()].HasPendingCommand != deviceResponse.HasPendingCommand)
						JsonCommand[pair.Key.ToString()].HasPendingCommand = false;
				}
			}

			return true;

		}

		private bool SaveToCloud(int deviceId)
		{
			//Save all changes to the cloud
			lock (LockObject)
			{
				_log.Debug("Sending changes to cloud");

				JsonCommand[deviceId.ToString()].HasPendingCommand = true;
				JsonCommand[deviceId.ToString()].EffectiveFlags = 0x1F;

				RestHandlerResult result = _restHandler.UpdateDevice(JsonCommand[deviceId.ToString()].ToString());
				_log.Debug("Tried to save: " + JsonCommand[deviceId.ToString()].ToString());
				if (result.Success)
					JsonCommand[deviceId.ToString()].HasPendingCommand = false; //Reset pending changes
			}

			return true;
		}

		private void GetDevices(int retry = 0)
		{
			lock (LockObject)
			{


				_log.Info("Fetching Devies from MelCloud");

				if (retry > 1)
				{
					_log.Error("Could not get devices after trying two times");
					//throw new Exception("Could not get devices. Aborting. Check your user is active and Melcloud is working");
					return;
				}

				try
				{
					var result = _restHandler.DoDeviceListing();
					_log.Debug(result.ResponseContent);
					if (result.Success)
					{
						_log.Debug("Got a successful response from melcloud");
						dynamic data = JsonConvert.DeserializeObject(result.ResponseContent); //Convert data

						//Process all Floor devices
						_log.Debug("Process all devices pr floor");
						if (data[0].Structure.ContainsKey("Floors") && data[0].Structure.Floors.Count > 0)
						{
							for (int i = 0; i < data[0].Structure.Floors.Count; i++)
							{
								_log.Debug("A floor detected");
								for (int j = 0; j < data[0].Structure.Floors[i].Devices.Count; j++)
								{
									_log.Debug("A device detected");
									CreateMelcloudDevice(data[0].Structure.Floors[i].Devices[j]);
								}
							}
						}
						else
							_log.Debug("No floor devices found");

						_log.Debug("Process all devices pr area");
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
							_log.Debug("No area devices found");

						_log.Debug("Process all devices pr clients");
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
							_log.Debug("No client devices found");

						_log.Debug("Process all devices pr devices");
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
							_log.Debug("No client devices found");

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
							_log.Debug("No access, giving up");
							Shutdown = true; //Shutdown plugin - should not run after this error
							throw new Exception("No access, giving up");

						}
					}


				}
				catch (Exception ex)
				{
					_log.Info("Could not connect to Melcloud to fetch devices: " + ex);
				}
			}

		}


		////// NOT USED (yet) ///////////////

		public override object PluginFunction(string functionName, object[] parameters)
		{
			return (object)null;
		}
		public override object PluginPropertyGet(string propertyName, object[] parameters)
		{
			return (object)null;
		}

		public override void PluginPropertySet(string propertyName, object value)
		{
		}
		public override void SetDeviceValue(int deviceId, double value, bool trigger = true)
		{
			this.HS.SetDeviceValueByRef(deviceId, value, trigger);
		}

		public override string ConfigDevice(int deviceId, string user, int userRights, bool newDevice)
		{
			return "";
		}

		public override Enums.ConfigDevicePostReturn ConfigDevicePost(
			int deviceId,
			string data,
			string user,
			int userRights)
		{
			return Enums.ConfigDevicePostReturn.DoneAndCancel;
		}

		public override string get_TriggerName(int triggerNumber)
		{
			return "";
		}

		public override bool HandleAction(IPlugInAPI.strTrigActInfo actionInfo)
		{
			return false;
		}

		public override void SpeakIn(int deviceId, string text, bool wait, string host)
		{
		}

		public override string GenPage(string link)
		{
			return "";
		}

		public override string PagePut(string data)
		{
			return "";
		}


		public override string get_ActionName(int actionNumber)
		{
			return "";
		}

		public override bool get_Condition(IPlugInAPI.strTrigActInfo actionInfo)
		{
			return false;
		}

		public override void set_Condition(IPlugInAPI.strTrigActInfo actionInfo, bool value)
		{
		}

		public override bool get_HasConditions(int triggerNumber)
		{
			return false;
		}

		public override string TriggerBuildUI(
			string uniqueControlId,
			IPlugInAPI.strTrigActInfo triggerInfo)
		{
			return "";
		}

		public override string TriggerFormatUI(IPlugInAPI.strTrigActInfo actionInfo)
		{
			return "";
		}

		public override IPlugInAPI.strMultiReturn TriggerProcessPostUI(
			NameValueCollection postData,
			IPlugInAPI.strTrigActInfo actionInfo)
		{
			return new IPlugInAPI.strMultiReturn();
		}

		public override bool TriggerReferencesDevice(IPlugInAPI.strTrigActInfo actionInfo, int deviceId)
		{
			return false;
		}

		public override bool TriggerTrue(IPlugInAPI.strTrigActInfo actionInfo)
		{
			return false;
		}

		public override int get_SubTriggerCount(int triggerNumber)
		{
			return 0;
		}

		public override string get_SubTriggerName(int triggerNumber, int subTriggerNumber)
		{
			return "";
		}

		public override bool get_TriggerConfigured(IPlugInAPI.strTrigActInfo actionInfo)
		{
			return true;
		}

		public override SearchReturn[] Search(string searchString, bool regEx)
		{
			return (SearchReturn[])null;
		}

		public override string ActionBuildUI(
			string uniqueControlId,
			IPlugInAPI.strTrigActInfo actionInfo)
		{
			return "";
		}

		public override bool ActionConfigured(IPlugInAPI.strTrigActInfo actionInfo)
		{
			return true;
		}

		public override int ActionCount()
		{
			return 0;
		}

		public override string ActionFormatUI(IPlugInAPI.strTrigActInfo actionInfo)
		{
			return "";
		}

		public override IPlugInAPI.strMultiReturn ActionProcessPostUI(
			NameValueCollection postData,
			IPlugInAPI.strTrigActInfo actionInfo)
		{
			return new IPlugInAPI.strMultiReturn();
		}

		public override bool ActionReferencesDevice(IPlugInAPI.strTrigActInfo actionInfo, int deviceId)
		{
			return false;
		}

		public override bool RaisesGenericCallbacks()
		{
			return false;
		}

		public override void HSEvent(Enums.HSEvent eventType, object[] parameters)
		{
		}


		public override IPlugInAPI.PollResultInfo PollDevice(int deviceId)
		{
			return new IPlugInAPI.PollResultInfo()
			{
				Result = IPlugInAPI.enumPollResult.Device_Not_Found,
				Value = 0.0
			};
		}

		protected override bool GetHasTriggers()
		{
			return false;
		}

		protected override int GetTriggerCount()
		{
			return 0;
		}

		public override bool SupportsAddDevice()
		{
			return false;
		}

		public override bool SupportsConfigDevice()
		{
			return false;
		}

		public override bool SupportsConfigDeviceAll()
		{
			return false;
		}

		public override bool SupportsMultipleInstances()
		{
			return false;
		}

		public override bool SupportsMultipleInstancesSingleEXE()
		{
			return false;
		}

		public override int Capabilities()
		{
			return 4;
		}

		public override int AccessLevel()
		{
			return 1;
		}

		public override IPlugInAPI.strInterfaceStatus InterfaceStatus()
		{
			return new IPlugInAPI.strInterfaceStatus()
			{
				intStatus = IPlugInAPI.enumInterfaceStatus.OK
			};
		}

		public override string InstanceFriendlyName()
		{
			return string.Empty;
		}
	}
}