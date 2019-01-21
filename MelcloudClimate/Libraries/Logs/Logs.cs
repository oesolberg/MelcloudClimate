using HomeSeerAPI;
using System;

namespace HSPI_MelcloudClimate.Libraries.Logs
{
    public class Log : Library
    {

        public new IHSApplication _hs;

        public Log(IHSApplication HS)
        {


            _hs = HS;
        }


        public enum LogType
        {
            LOG_TYPE_INFO = 0,
            LOG_TYPE_ERROR = 1,
            LOG_TYPE_WARNING = 2
        }

        public void Debug(string msg)
        {
            if (msg == null)
                msg = "";

            _hs.WriteLog(GetName() + " Debug", msg);

            Console.WriteLine(msg);

        }

        public void Info(string msg)
        {
            if (msg == null)
                msg = "";

            _hs.WriteLog(GetName() + " Info", msg);

            Console.WriteLine(msg);

        }

        public void Error(string msg)
        {
            if (msg == null)
                msg = "";

            _hs.WriteLog(GetName() + " Error", msg);

            Console.WriteLine(msg);

        }

        public void Write(string msg, LogType logType)
        {

          try
            {
                if (msg == null)
                    msg = "";
                if (!Enum.IsDefined(typeof(LogType), logType))
                {
                    logType = LogType.LOG_TYPE_ERROR;
                }
                 Console.WriteLine(msg);
                switch (logType)
                {
                    case LogType.LOG_TYPE_ERROR:
                        _hs.WriteLog(GetName() + " Error", msg);
                        break;
                    case LogType.LOG_TYPE_WARNING:
                        _hs.WriteLog(GetName() + " Warning", msg);
                        break;
                    case LogType.LOG_TYPE_INFO:
                        _hs.WriteLog(GetName(), msg);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in LOG of " + GetName() + ": " + ex.Message);
            }

        }
    }
}