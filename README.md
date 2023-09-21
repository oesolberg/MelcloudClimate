# MelcloudClimate
A HomeSeer 3 plugin for the MelCloud (Mitsubishi AC). Forked from https://github.com/alekslyse/MelcloudClimate

## Important!!! 
Stop Homeseer and make a copy of the directory with all subdirectories (normally "**c:\program files (x86)\Homeseer HS3**" for Windows). Save this copy to somewhere safe in case of something should go belly up and you need to roll back.

When you have buildt the project copy all files in directory **...\MelcloudClimate\MelcloudClimate\bin\Release** or **...\MelcloudClimate\MelcloudClimate\bin\Debug** into your HomeSeer directory

Most up to date info found at 
https://www.hjemmeautomasjon.no/forums/topic/5558-melcloudclimate-plugin/

If from Release:

You should have 2 new files **HSPI_MelcloudClimate.exe** and **HSPI_MelcloudClimate.exe.config** in your HomeSeer directory. And the directory **Homeseer HS3\bin** should have a new sub directory  - **MelcloudClimate** - with 12 files.

Restart Homeseer

Go into **Homeseer->Plugins->Manage** and press enable for MelcloudClimate

The plugin will now start and create an ini-file(MelcloudClimate.ini) in the directory **<Homeseer HS3>\Config**

Open it with notepad or any other application for editing text files.

It hopefully looks like this:
```
[User]
Username=InsertUsername
Password=InsertPassword
```
after **Username=** replace **InsertUsername** with your username/email

after **Password=** replace **InsertPassword** with your password

You might have to restart the plugin again (go to HomeSeer->Plugins->Manage and turn off then on again the switch for MelcloudClimate).
