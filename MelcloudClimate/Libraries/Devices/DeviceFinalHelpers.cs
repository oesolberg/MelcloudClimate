using Scheduler.Classes;
using System;
using HomeSeerAPI;

namespace HSPI_MelcloudClimate.Libraries.Devices
{
    public partial class Device
    {

        private string GetDeviceAddressName()
        {
            //Determine the device type
            if (ParentId == 0)
                return GetName() + "_" + Unique + "_" + Name + "_root";
            else
                return GetName() + "_" + Unique + "_" + Name + "_child";
        }


        public Device AddStatusField(int startValue, int endValue,  string suffix, bool includeValues = true, string graphicsPath = null)
        {

            var vsPair = new VSVGPairs.VSPair(ePairStatusControl.Status)
            {
                PairType = VSVGPairs.VSVGPairType.Range,
                RangeStart = startValue,
                RangeEnd = endValue,
                RangeStatusSuffix = suffix,
                IncludeValues = includeValues

            };

            _hs.DeviceVSP_AddPair(Id, vsPair);

            if (graphicsPath != null)
                AddStatusGraphicField(startValue, endValue, graphicsPath);


            return this;
        }

        public Device AddStatusControlRangeField(int startValue, int endValue, string suffix, bool includeValues = true, string graphicsPath = null)
        {

      



            var vsPair = new VSVGPairs.VSPair(ePairStatusControl.Both)
            {
                PairType = VSVGPairs.VSVGPairType.Range,
                RangeStart = startValue,
                RangeEnd = endValue,
                RangeStatusSuffix = suffix,
                IncludeValues = includeValues,
             


            };

            _hs.DeviceVSP_AddPair(Id, vsPair);

            if (graphicsPath != null)
                AddStatusGraphicField(startValue, endValue, graphicsPath);


            return this;
        }


        public Device AddButton(double value, string status, string graphicsPath = null)
        {





            var vsPair = new VSVGPairs.VSPair(ePairStatusControl.Both)
            {
                PairType = VSVGPairs.VSVGPairType.SingleValue,
                Value = value,
                Status = status,
                Render = Enums.CAPIControlType.Button




            };



            _hs.DeviceVSP_AddPair(Id, vsPair);

            if (graphicsPath != null)
                AddStatusGraphicField((int)value, -1, graphicsPath);

            return this;
        }

        public Device AddDropdown(int startValue, int endValue, string suffix, string graphicsPath = null)
        {


            var vsPair = new VSVGPairs.VSPair(ePairStatusControl.Both)
            {
                PairType = VSVGPairs.VSVGPairType.Range,
                RangeStart = startValue,
                RangeEnd = endValue,
                RangeStatusSuffix = suffix,
                Render = Enums.CAPIControlType.ValuesRange




            };



            _hs.DeviceVSP_AddPair(Id, vsPair);


            if (graphicsPath != null)
                AddStatusGraphicField(startValue, endValue, graphicsPath);

            return this;
        }



        public Device AddStatusControlSingleField(double value, string status, string graphicsPath = null, bool button = false)
        {





            var vsPair = new VSVGPairs.VSPair(ePairStatusControl.Both)
            {
                PairType = VSVGPairs.VSVGPairType.SingleValue,
                Value = value,
                Status = status,
           



        };

        

            _hs.DeviceVSP_AddPair(Id, vsPair);



            return this;
        }


        public Device AddStatusGraphicField(int startValue, int endValue, string graphicsPath = "")
        {
            if (endValue == -1)
            {
                //Create single graphic
                var vgPair = new VSVGPairs.VGPair
                {
                    PairType = VSVGPairs.VSVGPairType.SingleValue,
                    Set_Value = startValue,
                    Graphic = graphicsPath
                };

                _hs.DeviceVGP_AddPair(Id, vgPair);
            }
            else if (endValue > 0)
            {
                var vgPair = new VSVGPairs.VGPair
                {
                    PairType = VSVGPairs.VSVGPairType.Range,
                    RangeStart = startValue,
                    RangeEnd = endValue,
                    Graphic = graphicsPath
                };

                _hs.DeviceVGP_AddPair(Id, vgPair);
            }
            

            

            return this;
        }

        public void SetValue(double value)
        {
            _hs.SetDeviceValueByRef(Id, value, false);
           
        }



        public Device CheckAndCreate(double defaultValue = 0)
        {
        //Check if device exists, if not create it, if it exists, load it and return it
        if (_hs.DeviceExistsAddress(GetDeviceAddressName(), false) == -1)
        {
                Console.WriteLine("Device is missing, creating it");
            return Create(defaultValue); //Just run the create function
        }
        else
        {
                Console.WriteLine("Device exists, loading it");
                Id = _hs.DeviceExistsAddress(GetDeviceAddressName(), false);
                Console.WriteLine("Loading device with id: "+ GetDeviceAddressName());
                Refresh(); //Update local device
                return this;
                

        }

            




    }

        public Device Create(double defaultValue = 0) //Create the device
        {
            if (Id != 0)
                Update(); //If the item exists, do not rebuild it, but update it
            else
            {
                if (ParentId == 0)
                    createRootDevice();
                else
                    createChildDevice();
            }

                SetValue(defaultValue);

            return this;
        }
        
        //Update the device
        public Device Update()
        {
            return this;
        }
        
        
    }
}