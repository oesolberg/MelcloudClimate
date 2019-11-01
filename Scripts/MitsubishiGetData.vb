Imports System.Diagnostics
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Data.OleDb
Imports System.IO
Imports System.Net

Sub Main(parm as object) 


	Dim strURL 			as String = "https://app.melcloud.com/Mitsubishi.Wifi.Client/Device/Get?id=193687&buildingID=124842"
	Dim myWebReq 		as HttpWebRequest
	Dim myWebResp 		as HttpWebResponse
	Dim encoding 		as New System.Text.UTF8Encoding
	Dim sr 				as StreamReader
	Dim Token 			as String = "X-MitsContextKey: " & hs.GetVar("mitsubishi_token")


	System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12
	
	Try
		myWebReq = DirectCast(WebRequest.Create(strURL), HttpWebRequest)
		myWebReq.Method = "GET"
		myWebReq.Headers.Add(Token)
		myWebResp = DirectCast(myWebReq.GetResponse(), HttpWebResponse)
		
		sr = New StreamReader(myWebResp.GetResponseStream())
		Dim jsonText As String = sr.ReadToEnd()
		Dim ResponseLen as integer = len(jsonText)
	
	
		'hs.WriteLog("Mitsubishi", "Response: " & jsonText)
		
		Dim deserialized = JsonConvert.DeserializeObject(jsonText)
		
		Dim SetTemperatureZone1 		as String		= deserialized("SetTemperatureZone1")	'("Devices")(0)("DeviceID") 
		Dim RoomTemperatureZone1 		as String		= deserialized("RoomTemperatureZone1")
		Dim OperationMode 				as String		= deserialized("OperationMode")
		'Dim OperationModeZone1			as String		= deserialized("OperationModeZone1")

		Dim HCControlType 				as String		= deserialized("HCControlType")		
		Dim TankWaterTemperature 		as String		= deserialized("TankWaterTemperature")
		Dim SetTankWaterTemperature 	as String		= deserialized("SetTankWaterTemperature")
		Dim ForcedHotWaterMode 			as String		= deserialized("ForcedHotWaterMode")
		'Dim UnitStatus					as String		= deserialized("UnitStatus")
		
		Dim OutdoorTemperature 			as String		= deserialized("OutdoorTemperature")
		Dim IdleZone1 					as String		= deserialized("IdleZone1")
		Dim HolidayMode 				as String		= deserialized("HolidayMode")	
		Dim ProhibitHotWater 			as String		= deserialized("ProhibitHotWater")			
		
		Dim Power	 					as String		= deserialized("Power")

		
		
		'OperationMode values
		'====================
		'0		- Everything idle
		'1		- DHW on
		'2		- Heating on
		'3		- Unknown (cooling?!!)
		'4		- Unknown (defrost?)
		'5		- Unknown (but has been used) (standby)
		'6		- Legionella prevention cycle
		'>=7	- Unknown
		
		'Write to the virtual devices
		
		hs.SetDeviceValueByRef(659,SetTemperatureZone1,True)
		hs.SetDeviceValueByRef(598,RoomTemperatureZone1,True)
		hs.SetDeviceValueByRef(614,OperationMode,True)
		'hs.SetDeviceValueByRef(628,OperationModeZone1,True)
		'hs.SetDeviceValueByRef(660,HCControlType,True)
		hs.SetDeviceValueByRef(661,TankWaterTemperature,True)
		hs.SetDeviceValueByRef(662,SetTankWaterTemperature,True)
		
		If ForcedHotWaterMode = "False" then
			hs.SetDeviceValueByRef(663,0,True)
		elseif ForcedHotWaterMode = "True" then
			hs.SetDeviceValueByRef(663,1,True)
		else
			hs.SetDeviceValueByRef(663,100,True)
		end if
		
		'hs.SetDeviceValueByRef(664,UnitStatus,True)
		hs.SetDeviceValueByRef(665,OutdoorTemperature,True)
		
		If IdleZone1 = "False" then
			hs.SetDeviceValueByRef(657,0,True)
		elseif IdleZone1 = "True" then
			hs.SetDeviceValueByRef(657,1,True)
		else
			hs.SetDeviceValueByRef(657,100,True)
		end if
		
		If HolidayMode = "False" then
			hs.SetDeviceValueByRef(666,0,True)
		elseif HolidayMode = "True" then
			hs.SetDeviceValueByRef(666,1,True)
		else
			hs.SetDeviceValueByRef(666,100,True)
		end if
		
		If OperationMode = 1 then
			hs.SetDeviceValueByRef(667,1,True)
		else
			hs.SetDeviceValueByRef(667,0,True)
		end if
		
		If OperationMode = 6 then
			hs.SetDeviceValueByRef(628,1,True)
		else
			hs.SetDeviceValueByRef(628,0,True)
		end if
		
		If Power = "False" then
			hs.SetDeviceValueByRef(668,0,True)
		elseif Power = "True" then
			hs.SetDeviceValueByRef(668,1,True)
		else
			hs.SetDeviceValueByRef(668,100,True)
		end if


	Catch ex As Exception : hs.writelog("Mitsubishi", "Error:  " & ex.Message.ToString)
	End Try

	
End Sub