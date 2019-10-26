using System;

namespace HSPI_MelcloudClimate.Common
{
	public class SystemTime
	{
		public static Func<DateTime> Now = () => DateTime.Now;

		public static void ResetDateTime()
		{
			Now = () => DateTime.Now;
		}
	}
}