using HomeSeerAPI;

namespace HSPI_MelcloudClimate.Libraries.Settings
{
	public class Setting : Library
	{
		public new IHSApplication _hs;

		public const string InifileName = "MelcloudClimate.ini";
		public const string UserSection = "User";
		public const string MelcloudUserName = "Username";
		public const string MelcloudPassword = "Password";


		public Setting(IHSApplication HS)
		{
			_hs = HS;
		}


		public string GetEmail()
		{
			var userName = _hs.GetINISetting(UserSection, MelcloudUserName, "", InifileName);
			return userName;
		}

		public void DoInifileTemplateIfFileMissing()
		{
			if (!InifileExists())
			{
				CreateInifileTemplate();
			}
		}

		private void CreateInifileTemplate()
		{
			_hs.SaveINISetting(UserSection, MelcloudUserName, "InsertUsername", InifileName);
			_hs.SaveINISetting(UserSection, MelcloudPassword, "InsertPassword", InifileName);
		}

		private bool InifileExists()
		{
			var foundUsername= _hs.GetINISetting(UserSection, MelcloudPassword, "", InifileName);
			if (string.IsNullOrEmpty(foundUsername)) return false;
			return true;
		}

		public string GetPassword()
		{
			

			var password = _hs.GetINISetting(UserSection, MelcloudPassword, "", InifileName);
			return password;
		}
	}
}