using HomeSeerAPI;
using System;

namespace HSPI_MelcloudClimate.Libraries.WebPages
{
    public class WebPage : Library
    {

        public new IHSApplication _hs;

        public WebPage(IHSApplication HS)
        {

            _hs = HS;
        }


        
    }
}