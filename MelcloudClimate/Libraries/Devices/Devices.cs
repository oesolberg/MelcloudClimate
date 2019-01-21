using System;
using System.Collections.Generic;
using HomeSeerAPI;
using Scheduler;

namespace HSPI_MelcloudClimate.Libraries.Devices
{
    class Context
    {
        public string DeviceLevel { get; set; }
        public string DeviceName { get; set; }
        public string DeviceLocation  { get; set; } 
        public string DeviceLocation1  { get; set; } 
        public int ParentDevice  { get; set; } 
        public int DeviceRefId  { get; set; } 
    }
    
    
    public partial class Device : Library
    {
        private Context _context = new Context();
       
        private int _ParentDeviceId { get; set; } = 0;
        private bool _Exists { get; set; } = false;
        private int _CurrentDeviceId { get; set; }
        
        
        private int Id { get; set; }
        public int ParentId { get; set; }
        public Device Parent { get; set; }
        public string  Name { get; set; }
        public string Unique { get; set; }
        private string Location { get; set; }
        private string Location2 { get; set; }
        private bool CanDim { get; set; } 
        private string Image { get; set; }
        private Dictionary<string, string> PEDStorage { get; set; } = new System.Collections.Generic.Dictionary<string, string>();
 

        

        public Device(IHSApplication HS, Device ReferenceDevice = null)
        {


            _hs = HS;
            
            //init();
            //If A reference device was passed, and it was not null or zero
            //We can assume this will be a child of that device
            //Not this could also be set directly in the constructor as an int
            if (ReferenceDevice != null && ReferenceDevice.Id > 0)
            {
                Console.WriteLine("Debug ref parent: " + ReferenceDevice.Id);
                ParentId = ReferenceDevice.Id;
                Parent = ReferenceDevice;
            }



        }


    }
}