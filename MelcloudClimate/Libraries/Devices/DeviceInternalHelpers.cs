using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HomeSeerAPI;
using Scheduler.Classes;

namespace HSPI_MelcloudClimate.Libraries.Devices
{
    public partial class Device
    {
        
        private int CreateBasicDevice()
        {
            try
            {
                
              
                //Creating a brand new device, and get the actual device from the device reference
                var fullName = Location + GetName() + Location2 + GetName() +
                               Name + Unique;
                
                var dv = (DeviceClass)_hs.GetDeviceByRef(_hs.NewDeviceRef(fullName));
                
                var dvRef = dv.get_Ref(_hs);

                //Setting the type to plugin device
                var typeInfo = new DeviceTypeInfo_m.DeviceTypeInfo
                {
                    Device_Type = (int) DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Plug_In,
                    Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Plug_In,
                    Device_SubType_Description = Name
                };
                
                dv.set_DeviceType_Set(_hs, typeInfo);
                
                var pluginExtraData = new PlugExtraData.clsPlugExtraData();
                //var pluginExtraData = new PlugExtraData.clsPlugExtraData();
                if (PEDStorage.Count > 0)
                {
                   
                    //PED storage got something
                    foreach (KeyValuePair<string, string> pair in PEDStorage)
                    {
                        Console.WriteLine("Found PED");
                        pluginExtraData.AddNamed(pair.Key.ToString(), pair.Value.ToString());
                    }
                } 

                
                dv.set_PlugExtraData_Set(_hs, pluginExtraData);

                dv.set_Interface(_hs, GetName()); //Don't change this, or the device won't be associated with your plugin
                dv.set_InterfaceInstance(_hs, InstanceFriendlyName()); //Don't change this, or the device won't be associated with that particular instance

                dv.set_Device_Type_String(_hs, Name);
                dv.set_Can_Dim(_hs, false);

                //Setting the name and locations
                dv.set_Name(_hs, Name);
                dv.set_Location(_hs, Location);
                dv.set_Location2(_hs, Location2);

                //Misc options
                dv.set_Status_Support(_hs, true); //Set to True if the devices can be polled, False if not. (See PollDevice in hspi.vb)
                dv.MISC_Set(_hs, Enums.dvMISC.SHOW_VALUES); //If not set, device control options will not be displayed.
                //dv.MISC_Set(_hs, Enums.dvMISC.NO_LOG); //As default, we don't want to Log every device value change to the Log

                //Committing to the database, clear value-status-pairs and graphic-status pairs
                _hs.SaveEventsDevices();

                _hs.DeviceVSP_ClearAll(dvRef, true);
                _hs.DeviceVGP_ClearAll(dvRef, true);

                Id = dvRef;
                
                return dvRef; //Return the reference
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating basic device: " + ex.Message, ex);
            }
        }
        
        
        private void createRootDevice()
        {
            Console.WriteLine("Creating a new root device");
            //Log.Write("Creating a new root device");
            
            
            try
            {
                //Creating a brand new device, and get the actual device from the device reference
                var rootDevice = (DeviceClass)_hs.GetDeviceByRef(CreateBasicDevice());
                rootDevice.set_Device_Type_String(_hs, Name + GetName() + "root");
                rootDevice.set_Relationship(_hs, Enums.eRelationship.Parent_Root);
                rootDevice.set_Address(_hs, GetDeviceAddressName() );

                int dvRef = rootDevice.get_Ref(_hs);

                //Committing to the database, clear value-status-pairs and graphic-status pairs
                _hs.SaveEventsDevices();
                
                Console.WriteLine("Created device with ID " + dvRef);
                
                
                //Since everything went ok, trigger the refresh method
                Id = dvRef;
                Refresh();


            }
            catch (Exception ex)
            {

                throw new Exception("Error creating root device: " + ex.Message, ex);
            }
            
           
        }
        
        private int createChildDevice()
        {
          
            try
            {
                //Creating a brand new device, and get the actual device from the device reference
                var childDevice = (DeviceClass)_hs.GetDeviceByRef(CreateBasicDevice());
                childDevice.set_Device_Type_String(_hs, Name + GetName() + "child");
                childDevice.set_Relationship(_hs, Enums.eRelationship.Child);
                childDevice.set_Address(_hs, GetDeviceAddressName() );
                childDevice.AssociatedDevice_Add(_hs, ParentId);

				int dvRef = childDevice.get_Ref(_hs);

                var rootDevice = (DeviceClass) _hs.GetDeviceByRef(ParentId);
                rootDevice.AssociatedDevice_Add(_hs, dvRef); //Then associated that child reference with the root.

                //Committing to the database, clear value-status-pairs and graphic-status pairs
                _hs.SaveEventsDevices();

                return dvRef; //Return the reference
            }
            catch (Exception ex)
            {
              
                throw new Exception("Error creating child device: " + ex.Message, ex);
            }
           
        }

        public int getDeviceId()
        {
            return _context.DeviceRefId;
        }


        private void Refresh()
        {
            //This will refresh this object with the current settings
            var device = (DeviceClass)_hs.GetDeviceByRef(Id);
            
            Name = device.get_Name(_hs);
            Location = device.get_Location(_hs);
            Location2 = device.get_Location2(_hs);
            Image = device.get_Image(_hs);
            CanDim = device.get_Can_Dim(_hs);
            
            
        }

        
    }
}