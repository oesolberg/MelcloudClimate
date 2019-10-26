using System;

namespace HSPI_MelcloudClimate.Common
{

	public delegate void IniSettingsChangedForUserNamePasswordEventHandler(Object sender, EventArgs eventArgs);
	public delegate void IniSettingsChangedForCheckIntervalEventHandler(Object sender, EventArgs eventArgs);
	public delegate void IniSettingsChangedForLogLevelEventHandler(Object sender, EventArgs eventArgs);

	public class DelegateDummy
	{

	}

	public class MelcloudClimateEventArgs : EventArgs
	{
		public bool IsConnected { get; set; }
		//public Common.Calendar.CalendarType CalendarType { get; set; }
	}
}