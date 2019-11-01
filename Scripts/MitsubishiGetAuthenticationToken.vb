Imports System.Diagnostics
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Data.OleDb
Imports System.IO
Imports System.Net

Sub Main(parm as object) 



	Dim json As String = "{" & chr(34) & "Email" & chr(34) & ":" & chr(34) & "someone@email.com" & chr(34) & "," & chr(34) & "Password" & chr(34) & ":" & chr(34) & "YourPwd" & chr(34) & "," & chr(34) & "Language" & chr(34) & ":" & "0" & "," & chr(34) & "AppVersion" & chr(34) & ":" & chr(34) & "1.18.5.1" & chr(34) & "," & chr(34) & "Persist" & chr(34) & ":" & "false"& "," & chr(34) & "CaptchaResponse" & chr(34) & ":" & "null" & "}"
	
	
	'hs.writelog("Mitsubishi",json)	'it should look like this:	{"Email":"someone@email.com","Password":"YourPwd","Language":0,"AppVersion":"1.18.5.1","Persist":false,"CaptchaResponse":null}

	
	Dim strURL 			as String = "https://app.melcloud.com/Mitsubishi.Wifi.Client/Login/ClientLogin"
	
	Dim myWebReq 		as HttpWebRequest
	Dim myWebResp 		as HttpWebResponse
	Dim encoding 		as New System.Text.UTF8Encoding
	Dim sr 				as StreamReader
	
		
	
	Try
		Dim data As Byte() = encoding.GetBytes(json)
		myWebReq = DirectCast(WebRequest.Create(strURL), HttpWebRequest)
		myWebReq.ContentType = "application/json; charset=utf-8"
		myWebReq.ContentLength = data.Length
		myWebReq.Method = "POST"
		Dim myStream As Stream = myWebReq.GetRequestStream()
       
		myStream.Write(data, 0, data.Length)
		myStream.Close()
		
		myWebResp = DirectCast(myWebReq.GetResponse(), HttpWebResponse)
		sr = New StreamReader(myWebResp.GetResponseStream())
		Dim jsonText As String = sr.ReadToEnd()
		
		'hs.WriteLog("Mitsubishi", "Response: " & jsonText)
				
		Dim deserialized = JsonConvert.DeserializeObject(jsonText)

		'Set the variables
		Dim Token as String		= deserialized("LoginData")("ContextKey") 	'("eddi")(0)("sta")
		'hs.writelog("Mitsubishi","Token: " & Token)
		hs.SaveVar("mitsubishi_token",Token)

	
		'hs.writelog("Mitsubishi","Authorization token retrieved")
		'hs.writelog("Mitsubishi","Token: " & hs.GetVar("mitsubishi_token"))

	Catch ex As Exception : hs.writelog("Mitsubishi", "Error:  " & ex.Message.ToString)
	End Try
	
	
End Sub