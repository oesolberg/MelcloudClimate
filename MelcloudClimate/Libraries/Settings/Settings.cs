using HomeSeerAPI;

namespace HSPI_MelcloudClimate.Libraries.Settings
{
    public class Setting : Library
    {
        public new IHSApplication _hs;

        public Setting(IHSApplication HS)
        {
            _hs = HS;
        }

      
    }
}