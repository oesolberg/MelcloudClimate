using Hspi;

namespace HSPI_MelcloudClimate
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Connector.Connect<HSPI>(args);
        }

    }
}