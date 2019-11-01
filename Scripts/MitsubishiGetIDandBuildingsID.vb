Imports System.Diagnostics
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Data.OleDb
Imports System.IO
Imports System.Net

Sub Main(parm as object) 



	Dim strURL 			as String = "https://app.melcloud.com/Mitsubishi.Wifi.Client/User/ListDevices"
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
	
		jsonText = jsonText.Substring(1, ResponseLen-2)			'Because for some reason the json is surrounded by [ and ]
		
		'hs.WriteLog("Mitsubishi", "Response: " & jsonText)
		
		Dim deserialized = JsonConvert.DeserializeObject(jsonText)
		
		Dim mitsubishi_buildingid 	as String		= deserialized("ID") 	'("eddi")(0)("sta")
		Dim mitsubishi_deviceid 	as String		= deserialized("Structure")("Devices")(0)("DeviceID") 	'("eddi")(0)("sta")

		'hs.writelog("Mitsubishi","mitsubishi_buildingid: " & mitsubishi_buildingid)	
		'hs.writelog("Mitsubishi","mitsubishi_deviceid: " & mitsubishi_deviceid)
		'hs.writelog("Mitsubishi","BuildingID and DeviceID retrieved successfully")		
		
		hs.SaveVar("mitsubishi_buildingid",mitsubishi_buildingid)
		hs.SaveVar("mitsubishi_deviceid",mitsubishi_deviceid)

		
	Catch ex As Exception : hs.writelog("Mitsubishi", "Error:  " & ex.Message.ToString)
	End Try

	
End Sub