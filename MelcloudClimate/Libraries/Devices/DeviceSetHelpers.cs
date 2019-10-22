namespace HSPI_MelcloudClimate.Libraries.Devices
{
    public partial class Device
    {

        private void setParentId(int parentRefId)
        {
            _context.ParentDevice = parentRefId;
            
        }

        public Device AddPED(string Key, dynamic Value)
        {
            if (PEDStorage.ContainsKey(Key) == false) {
                //Key does not exist
                PEDStorage.Add(Key, Value.ToString());
                System.Console.WriteLine("Saved PED"+Key);

            }

            return this;
        }
        
    }
}