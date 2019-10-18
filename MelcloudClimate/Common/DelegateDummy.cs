using System;

namespace HSPI_MelcloudClimate.Common
{
	//public delegate void GCalSeerConnectionChangedEventHandler(Object sender, GcalEventArgs eventArgs);

	//public delegate void GCalSeerEventDataUpdatedEventHandler(Object sender, EventArgs eventArgs);

	public delegate void IniSettingsChangedEventHandler(Object sender, EventArgs eventArgs);

	public class DelegateDummy
	{

	}

	public class MelcloudClimateEventArgs : EventArgs
	{
		public bool IsConnected { get; set; }
		//public Common.Calendar.CalendarType CalendarType { get; set; }
	}
}