using System;
using System.Collections.Generic;
using System.Text;
using HomeSeerAPI;
using Scheduler.Classes;

namespace HSPI_MelcloudClimate.Libraries.Devices
{
    public partial class Device
    {
        public void ListDevices()
        {
            //var dv = (DeviceClass)_hs.GetDeviceByRef(709);
            Console.WriteLine(_hs.DeviceExistsAddress("Smappee_root", false));
        }


        public Device GetDeviceByName(string deviceName)
        {
            var SearchDevice = _hs.DeviceExistsAddress(deviceName, false);

            if (SearchDevice != -1)
            {

                Id = SearchDevice;
                Refresh();

            }

            return this;
        }

        public bool DeviceExists(string deviceName)
        {
            return DevicesExists(new string[] { deviceName });
        }

        public bool DevicesExists(Array Devices)
        {
            //Returns if an array of devices exists
            bool exists = true;
            
            foreach (string Device in Devices)
            {
                if (_hs.DeviceExistsAddress(Device, false) == -1)
                {
                    exists = false;
                    Console.WriteLine(Device+" does not exist");
                    
                }
                
          

            }
            
            return exists;
        }
    }
}