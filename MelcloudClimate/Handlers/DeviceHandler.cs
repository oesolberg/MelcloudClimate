using System;
using System.Collections.Generic;
using System.Linq;
using HomeSeerAPI;
using HSPI_MelcloudClimate.Common;
using HSPI_MelcloudClimate.Libraries.Logs;
using Scheduler.Classes;

namespace HSPI_MelcloudClimate.Handlers
{
	public interface IDeviceHandler
	{
		void SetConnectedDevicesToNotConnected(string reason=null);
		void SetConnectedDevicesToConnected();
	}
	public class DeviceHandler : IDeviceHandler
	{
		private readonly IHSApplication _hs;
		private readonly ILog _log;

		private List<Scheduler.Classes.DeviceClass> _melcloudDevices = new List<Scheduler.Classes.DeviceClass>();
		private DateTime _lastDeviceFetchTime;

		public DeviceHandler(IHSApplication hs, ILog log)
		{
			_hs = hs;
			_log = log;
		}

		public void SetConnectedDevicesToNotConnected(string reason = null)
		{

			UpdateConnectedDevices(0, "Not connected"+reason);
		}

		public void SetConnectedDevicesToConnected()
		{
			UpdateConnectedDevices(1, "Connected");
		}

		private void UpdateConnectedDevices(double value, string text)
		{
			UpdateDevicesIfNecessary();
			var devicesToUpdate = _melcloudDevices.Where(x => x.IsOfPedType(Constants.Connection, _hs)).ToList();
			foreach (var deviceClass in devicesToUpdate)
			{
				var deviceRef = deviceClass.get_Ref(_hs);
				_hs.SetDeviceValueByRef(deviceRef, value, true);
				_hs.SetDeviceString(deviceRef, text, false);
			}
		}

		private void UpdateDevicesIfNecessary()
		{
			if (_melcloudDevices.Count == 0 || _lastDeviceFetchTime > SystemTime.Now().AddHours(2))
			{
				GetDevicesFromHomeSeer();
			}
		}

		private void GetDevicesFromHomeSeer()
		{
			var deviceList = new List<Scheduler.Classes.DeviceClass>();
			var deviceEnumerator = (clsDeviceEnumeration)_hs.GetDeviceEnumerator();
			while (!deviceEnumerator.Finished)
			{
				var foundDevice = deviceEnumerator.GetNext();
				deviceList.Add(foundDevice);
			}
			//==  Where d.Interface(hs) = Me.Name
			_melcloudDevices = deviceList.Where(x => x.get_Interface(_hs) == Utility.PluginName).ToList();
			_lastDeviceFetchTime = SystemTime.Now();
		}
	}

	public static class DeviceClassHelper
	{
		public static bool IsOfPedType(this DeviceClass device, string pedTypeToFind, IHSApplication hs)
		{
			var ped = (PlugExtraData.clsPlugExtraData)device.get_PlugExtraData_Get(hs);
			var pedTypeAsObject = ped.GetNamed("Type");
			if (pedTypeAsObject == null) return false;
			var pedType = pedTypeAsObject as string;
			if (string.IsNullOrEmpty(pedType)) return false;
			if (!String.Equals(pedType, pedTypeToFind, StringComparison.CurrentCultureIgnoreCase)) return false;

			return true;
		}
	}
}