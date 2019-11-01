Imports System.Diagnostics
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Data.OleDb
Imports System.IO
Imports System.Net

Sub Main(ByVal Temperature As string)

	
	' EffectiveFlags settings
	' temp		8589934592
	' sww/flow	281474976710688
	' mode		8
	' power		1
	' force sww 65536
	' all 		281483566710825
	
	Dim TempNum			as double	=	CDbl(Val(Temperature))
	Dim DeviceID		as String	=	hs.GetVar("mitsubishi_deviceid")

	hs.writelog("Mitsubishi","Setting MelCloud heating to " & TempNum & " degrees C")
	
	Dim json As String = "{" & chr(34) & "DeviceID" & chr(34) & ":" & chr(34) & DeviceID & chr(34) & "," & chr(34) & "SetTemperatureZone1" & chr(34) & ":"  & TempNum & "," & chr(34) & "HasPendingCommand" & chr(34) & ":" & "true" & "," & chr(34) & "EffectiveFlags" & chr(34) & ":" & "8589934592" & "}"
	
	'hs.writelog("Mitsubishi",json)

	
	Dim strURL 			as String = "https://app.melcloud.com/Mitsubishi.Wifi.Client/Device/SetAtw"
	
	Dim myWebReq 		as HttpWebRequest
	Dim myWebResp 		as HttpWebResponse
	Dim encoding 		as New System.Text.UTF8Encoding
	Dim sr 				as StreamReader
	Dim Token 			as String = "X-MitsContextKey: " & hs.GetVar("mitsubishi_token")
	
	
	Try
		Dim data As Byte() = encoding.GetBytes(json)
		myWebReq = DirectCast(WebRequest.Create(strURL), HttpWebRequest)
		myWebReq.ContentType = "application/json; charset=utf-8"
		myWebReq.ContentLength = data.Length
		myWebReq.Method = "POST"
		myWebReq.Headers.Add(Token)
		Dim myStream As Stream = myWebReq.GetRequestStream()
       
		myStream.Write(data, 0, data.Length)
		myStream.Close()
		
		myWebResp = DirectCast(myWebReq.GetResponse(), HttpWebResponse)
		sr = New StreamReader(myWebResp.GetResponseStream())
		Dim jsonText As String = sr.ReadToEnd()
		
		'hs.WriteLog("Mitsubishi", "Response: " & jsonText)
				
		Dim deserialized = JsonConvert.DeserializeObject(jsonText)

	Catch ex As Exception : hs.writelog("Mitsubishi", "Error:  " & ex.Message.ToString)
	End Try
	
	
End Sub