using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Amazon.SimpleDB.Model;
using Amazon.SimpleDB;
using System.Collections.Specialized;
using Amazon;
using System.Configuration;
using Amazon.S3.Model;
using System.IO;

namespace NinePlaces_Service
{
    public enum LogSeverity
    {
        Undefined,
        Verbose,
        Debug,
        Informational,
        Warning,
        Error, 
        CriticalError
    }


    public class Log : ITextSendToS3, ISendToSimpleDB
    {
        public string ItemName { get { return m_lID.ToString(); } }
        public bool QuietLogs { get { return true; } }
        public string Domain { get { return "NPLogs"; } }
        public string DestinationBucket { get { return "logs.nineplaces.com"; } }
        public string DestinationKey {get;protected set;}
        public bool PublicRead { get { return true; } }
        public string ContentType { get { return "text/plain"; } }
        public bool SentToS3 { get; set; }
        public bool SentToSimpleDB { get; set; }
        public string Text
        {
            get
            {
                lock (m_sbLog)
                    return m_sbLog.ToString();
            }
        }

        protected static Random r;
        protected LogSeverity m_eSeverity = LogSeverity.Undefined;
        protected ulong m_lID = 0;
        protected StringBuilder m_sbLog = new StringBuilder(5000);
        protected DateTime dtStart = DateTime.Now;
        static Log()
        {
            r = new Random();
        }

        public Log()
        {
            byte[] b = new byte[8];
            r.NextBytes(b);

            m_lID = System.BitConverter.ToUInt64(b, 0);
            DestinationKey = DateTime.UtcNow.ToString("yyyy-MM-dd") + "/" + m_lID.ToString();
        }

        private static Log GetLog
        {
            get
            {
                return HttpContext.Current.Items["Log"] as Log;
            }
        }

        public static Log Undefined(string in_strLogString)
        {
            return GetLog.WriteLog(in_strLogString, LogSeverity.Undefined);
        }

        public static Log Verbose(string in_strLogString)
        {
            return GetLog.WriteLog(in_strLogString, LogSeverity.Verbose);
        }

        public static Log Debug(string in_strLogString)
        {
            return GetLog.WriteLog(in_strLogString, LogSeverity.Debug);
        }

        public static Log Informational(string in_strLogString)
        {
            return GetLog.WriteLog(in_strLogString, LogSeverity.Informational);
        }

        public static Log Warning(string in_strLogString)
        {
            return GetLog.WriteLog(in_strLogString, LogSeverity.Warning);
        }

        public static Log Error(string in_strLogString)
        {
            return GetLog.WriteLog(in_strLogString, LogSeverity.Error);
        }

        public static Log CriticalError(string in_strLogString)
        {
            return GetLog.WriteLog(in_strLogString, LogSeverity.CriticalError);
        }

        public static Log Exception(Exception in_ex)
        {
            GetLog.WriteLog(in_ex.Message, LogSeverity.CriticalError);
            return GetLog.WriteLog(in_ex.StackTrace, LogSeverity.CriticalError);
        }

        public Log WriteLog(string in_strLogEntry, LogSeverity in_eSeverity)
        {
            
            string strCaller = Caller();
            StringBuilder sb = new StringBuilder("[" + in_eSeverity.ToString().Substring(0, 5) + "] [");
            if (strCaller.Length > 25)
                sb.Append(".." + strCaller.Substring(strCaller.Length - 23, 23));
            else
                sb.Append(strCaller.PadRight(25));
            sb.Append("] : ");

            lock (m_sbLog)
            {
                if (Submitted)
                    return this as Log;
                m_sbLog.AppendLine(sb.ToString() + in_strLogEntry);
                if (in_eSeverity > m_eSeverity)
                    m_eSeverity = in_eSeverity;
            }

            return this as Log;
        }

        protected string Caller()
        {
            try
            {
                MethodBase method;
                int nCount = 1;

                // we loop here so that we can find the first NON-LOGMGR caller.
                do
                {
                    StackFrame frame = new StackFrame(nCount++);
                    method = frame.GetMethod();
                }
                while (nCount <= 5 && method.ReflectedType.FullName == "NinePlaces_Service.Log" );

                return method.ReflectedType.FullName + "." + method.Name;
            }
            catch( Exception ex )
            {
            }
            return string.Empty;
        }

        public static void SubmitLog()
        {
            GetLog.DoSubmitLog();
        }

        public void DoSubmitLog()
        {
            TimeSpan ts = DateTime.Now - dtStart;
            Log.Verbose("TOOK: " + ts.TotalMilliseconds + "ms");
            lock (m_sbLog)
            {
                if (Submitted)
                    return;

                GetLog.Submitted = true;
                if (m_eSeverity >= LogSeverity.Verbose)
                {
                    QueueManager.Submit(this as ISendToS3);
                }
                
            }
        }

        private bool m_bSubmitted = false;
        public bool Submitted
        {
            get
            {
                return m_bSubmitted;
            }
            protected set
            {
                if (m_bSubmitted != value)
                    m_bSubmitted = value;
            }
        }

        public ReplaceableItem ToSimpleDBRequests()
        {
            ReplaceableItem oToRet = new ReplaceableItem();
            oToRet.ItemName = m_lID.ToString();
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "ID", m_lID.ToString(), true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "Severity", SimpleDBHelpers.ValueToSimpleDB(((int)m_eSeverity).ToString()), true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "DateTime", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), true);
            return oToRet;
        }
    }
}