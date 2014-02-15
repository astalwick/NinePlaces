using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Windows.Browser;

namespace Common
{
    public enum LogEventSeverity
    {
        Undefined = 0,
        Verbose = 1,                    // the almost-always unnecessary to see log text.
        Debug = 2,                      // useful logs when debugging the app.
        Informational = 3,              // use this mainly for logging the starts of major app events
        Warning = 4,                    // hm, we may be in a weird state.
        Error = 5,                      // an error has occurred, but it is not the end of the world.
        CriticalError = 6               // a serious, must-be-resolved error has occurred.
    };

    /// <summary>
    /// LogEvent is the internal log object - it represents a single log entry.
    /// </summary>
    internal class LogEvent
    {
        protected LogEventSeverity m_nSeverity = LogEventSeverity.Undefined;
        protected string strText = string.Empty;
        protected string strCallingMethod = string.Empty;
        protected DateTime dtEventTime = DateTime.MinValue;
        protected int nIndent = 0;

        public LogEvent(string in_strText, string in_strCallingMethod, DateTime in_dtTime, LogEventSeverity in_nSev, int in_nIndent)
        {
            m_nSeverity = in_nSev;
            strText = in_strText;
            strCallingMethod = in_strCallingMethod;
            nIndent = in_nIndent;
            dtEventTime = in_dtTime;
#if( DEBUG )
            // if we're debugging, output the log entry to the console if it is within 
            // our "we care about this" threshold.
            if (Log.g_lLogLevel <= in_nSev)
            {
                System.Diagnostics.Debug.WriteLine(ToString().TrimEnd( new char[] {'\r','\n'} ));
            }
#endif
        }

        public override string ToString()
        {
            string strRet = string.Empty;
            StringBuilder sb = new StringBuilder("[" + m_nSeverity.ToString().Substring( 0, 5 ) + "] ");
            sb.Append("[");
            if (strCallingMethod.Length > 31)
                sb.Append(".." + strCallingMethod.Substring( strCallingMethod.Length - 29, 29 ));
            else 
                sb.Append(strCallingMethod.PadRight( 31 ));

            string[] arLinesToOutput = strText.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);

            sb.Append("] - " + IndentToTabs());
            
            foreach( string strLine in arLinesToOutput )
            {
                strRet += sb.ToString() + strLine + "\r\n";
            }

            return strRet;
        }

        private string IndentToTabs()
        {
            string strRet = string.Empty;
            for (int i = 0; i < nIndent; i++)
            {

                strRet += "\t";
            }
            return strRet;
        }

    }

    public static class Log
    {
#if (DEBUG)
        internal static int g_nMaxMessages = 10000;
        public static LogEventSeverity g_lLogLevel = LogEventSeverity.Verbose;        // System will ignore log events of lower priority than g_lLogLevel.
#else
        internal static int g_nMaxMessages = 1000;
        public static LogEventSeverity g_lLogLevel = LogEventSeverity.Warning;              // System will ignore log events of lower priority than g_lLogLevel.
#endif

        public static object g_oQueueLock = new object();
        internal static Queue<LogEvent> g_qLogs = new Queue<LogEvent>();
        public static int g_nIndent = 0;

        public static void TrackInitCompleted()
        {
             try
             {
                 System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                     {
                         HtmlPage.Window.Invoke("initCompleted");
                     });

            }
            catch
            {
            }
        }

        private static string g_strLastPage = string.Empty;
        public static void TrackPageView(string in_strPageURL)
        {
            lock (g_strLastPage)
            {
                if (g_strLastPage.CompareTo(in_strPageURL) == 0)
                    return;
                else
                    g_strLastPage = in_strPageURL;
            }

            try
            {
                 System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                     {
                         HtmlPage.Window.Invoke("slTrackPageView", in_strPageURL);
                     });

                
            }
            catch
            {
            }

        }

        public static void TrackEvent(string in_strCategory, string in_strAction, string in_strLabel = null, int in_nValue = int.MinValue)
        {
            try
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (in_nValue != int.MinValue)
                            HtmlPage.Window.Invoke("slTrackEvent", in_strCategory, in_strAction, in_strLabel, in_nValue);
                        else if (in_nValue != int.MinValue)
                            HtmlPage.Window.Invoke("slTrackEvent", in_strCategory, in_strAction, in_strLabel);
                        else
                            HtmlPage.Window.Invoke("slTrackEvent", in_strCategory, in_strAction);
                    });
            }
            catch
            {
            }
        }

        public static readonly int[] g_arBuckets = new int[] {  10, 20,  50, 100, 200, 300, 400, 500, 750, 1000, 1500, 2000, 2500, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000, 15000, 20000, 25000, 30000, 40000, 50000, 75000, 100000 }; 
        public static void TrackTiming( string in_strName, TimeSpan in_tsTiming )
        {
            
            int nMS = (int) in_tsTiming.TotalMilliseconds;
            int i;
            for( i = 0; i < g_arBuckets.Length; i++ )
            {
                if( nMS < g_arBuckets[i] )
                {
                    break;
                }
            }

            string strBucket = (i == 0 ? "0" : g_arBuckets[i-1].ToString()) + "-" + (g_arBuckets[i] - 1).ToString();
            TrackEvent("Timings", in_strName, strBucket, nMS );
        }

        /// <summary>
        /// Determines the calling class/method and outputs it
        /// as a string, for standardized (simple) logging.
        /// </summary>
        /// <returns></returns>
        private static string CallingMethod()
        {
            MethodBase method;
            int nCount = 1;
            do
            {
                StackFrame frame = new StackFrame(nCount++);
                method = frame.GetMethod();
            }
            while (nCount <= 5 && method.ReflectedType.FullName == "Common.Log" );

            return method.ReflectedType.FullName + "." + method.Name;
        }

        /// <summary>
        /// Outputs the exception in a more readable format!
        /// </summary>
        public static void Exception(Exception ex)
        {
            StringBuilder sb = new StringBuilder("** EXCEPTION   " + CallingMethod() + "   EXCEPTION **\r\n");
            sb.AppendLine("\t" + ex.Message);
            sb.AppendLine("\t" + ex.StackTrace);
            if (ex.InnerException != null)
            {
                sb.AppendLine("\t\t** INNEREXCEPTION   " + CallingMethod() + "   INNEREXCEPTION **\r\n");
                sb.AppendLine("\t\t" + ex.InnerException.Message);
                sb.AppendLine("\t\t" + ex.InnerException.StackTrace);
            }
            Log.TrackEvent("Errors", CallingMethod(), ex.GetType().ToString(), ((int)LogEventSeverity.Error));
            WriteLine(sb.ToString());

            //
            // every exception gets fired to the server.
            //

            try
            {
                
                Uri uri = new Uri(Helpers.GetAPIURL() + "ReportException");

                HttpWebRequest wrSendException = (HttpWebRequest)WebRequest.Create(uri);

                wrSendException.Method = "POST";
                wrSendException.ContentType = "text/plain";


                byte[] byteData = UTF8Encoding.UTF8.GetBytes(sb.ToString());
                wrSendException.AllowReadStreamBuffering = true;

                IAsyncResult aGetRequestResult = wrSendException.BeginGetRequestStream(x =>
                {
                    System.IO.Stream rs = wrSendException.EndGetRequestStream(x);
                    rs.Write(byteData, 0, byteData.Length);
                    rs.Close();

                    IAsyncResult result = wrSendException.BeginGetResponse(new AsyncCallback(ReportExceptionCompleted), wrSendException);
                }, null);


                
            }
            catch
            {
                // do nothing if error reporting fails.
            }
        }

        /// <summary>
        /// Callback when the create user request returns.
        /// </summary>
        /// <param name="result"></param>
        public static void ReportExceptionCompleted(IAsyncResult result)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
            }
        }

        public static void Indent()
        {
            Interlocked.Increment(ref Log.g_nIndent);
        }

        public static void Unindent()
        {
            Interlocked.Decrement(ref Log.g_nIndent);
        }
        
        public static void WriteLine(string in_strLogText)
        {
            WriteLine(in_strLogText, LogEventSeverity.Informational);
        }

        public static void WriteLine(string in_strLogText, LogEventSeverity in_eSeverity)
        {
#if (DEBUG == false)
            //
            // in debug, we log everything and output only the level we want to see.
            // in release, we IGNORE everythign below the level we want to see.
            //
            if( in_eSeverity < g_lLogLevel )
                return;
#endif
            
            lock (g_oQueueLock)
            {
                Log.g_qLogs.Enqueue(new LogEvent(in_strLogText, CallingMethod(), DateTime.Now, in_eSeverity, g_nIndent));

                // dequeue dowbn to our MAX MESSAGE count.  
                // dequeued items get garbage collected when everyone gets bored.
                while (Log.g_qLogs.Count > g_nMaxMessages)
                {
                    g_qLogs.Dequeue();
                }
            }

            if (in_eSeverity > LogEventSeverity.Warning)
            {
                Log.TrackEvent("Errors", CallingMethod(), "", ((int)in_eSeverity));
                System.Diagnostics.Debug.Assert(false);
            }
        }
    }
}
