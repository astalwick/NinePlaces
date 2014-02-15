using System.Windows;
using System;
using NinePlaces.Managers;
using NinePlaces.IconDockControls;
using System.Resources;
using System.Reflection;
using NinePlaces.Localization;
using NinePlaces.Undo;
using System.Net;
using System.Net.Browser;
using System.Windows.Browser;
using Common.Interfaces;
using Common;

namespace NinePlaces
{
    
	public partial class App : Application ,IApp
	{
        public static LocalizedStrings LocalizedStrings = new LocalizedStrings();
        public static ResourceManager Resource = new ResourceManager("NinePlaces.Localization.StringLibrary", Assembly.GetExecutingAssembly());
        public static bool Dragging {get;set;}

        public static ITimeZoneManager TimeZones { get; internal set; }

        public static double ZoomIconSizeAdjust { get; internal set;  }
        public static double StaticIconWidthHeight { get; set; }
        private static double g_dIconWidthHeight = 32.0;
        public static double IconWidthHeight 
        { 
            get
            {
                return g_dIconWidthHeight;
            }
            set
            {
                if (g_dIconWidthHeight != value)
                {
                    g_dIconWidthHeight = value;
                    if( IconWidthHeightChanged != null )
                    {
                        IconWidthHeightChanged.Invoke(null, new EventArgs() );
                    }
                }
            }
        }

        public static event EventHandler IconWidthHeightChanged;
        public static long BytesWritten { get; set; }
        public static long BytesRead { get; set; }

        public static ITimeline Timeline { get; set; }
        public static IIconDock IconDock { get; set; }

        private static ISelectionMgr g_oSelectionMgr = new SelectionMgr() as ISelectionMgr;
        public static ISelectionMgr SelectionMgr
        {
            get
            {
                return g_oSelectionMgr;
            }
        }

        private static DLLLoadManager g_oLoadMgr = new DLLLoadManager();
        public static DLLLoadManager AssemblyLoadManager
        {
            get
            {
                return g_oLoadMgr;
            }
        }

        private static UndoMgr g_oUndoMgr = new UndoMgr();
        public static UndoMgr UndoMgr
        {
            get
            {
                return g_oUndoMgr;
            }
        }

        public static LoginSignUp LoginControl { get; set; }

        public static bool IsAncestor(FrameworkElement in_oAncestor, FrameworkElement in_oDescendant)
        {
            FrameworkElement curParent = in_oDescendant;
            while (curParent != null && curParent != App.Current.RootVisual && curParent != in_oAncestor)
            {
                curParent = System.Windows.Media.VisualTreeHelper.GetParent(curParent) as FrameworkElement;
            }

            return curParent == in_oAncestor;
        }

        [ScriptableMemberAttribute]
        public bool IsModified
        {
            get 
            {  
                if( App.Timeline == null || App.Timeline.VM == null || !App.Timeline.VM.WritePermitted )
                    return false;
                return !(App.Timeline.VM as ITimelineViewModel).IsFullyPersisted;
            }
        }

		public App() 
		{
            Common.Helpers.App = this;
            HtmlPage.RegisterScriptableObject("SLApp", this);
            StaticIconWidthHeight = 32.0;
            ZoomIconSizeAdjust = 1.0 / 2.0;     // set to 1.0 to stop zoomsize updates.
            IconWidthHeight = 48.0;
			this.Startup += this.OnStartup;
			this.Exit += this.OnExit;
			this.UnhandledException += this.Application_UnhandledException;

            TimeZones = new TimeZoneManager();

			InitializeComponent();
            

            TwentyFourHourMode = false;
            Common.CrossAssemblyNotifyContainer.HourFormatChange += new Common.HourFormatEventHandler(CrossAssemblyNotifyContainer_HourFormatChange);
		}

        public static UIElement AppRootVisual { get; set; }

        public UIElement PageRoot { get { return App.AppRootVisual; } }

        public static bool TwentyFourHourMode { get; set; }

        void CrossAssemblyNotifyContainer_HourFormatChange(object sender, Common.HourFormatEventArgs args)
        {
            TwentyFourHourMode = args.TwentyFourHour;
        }

        private static ITimeZoneDriver g_sDefaultTimeZone = null;
        public static ITimeZoneDriver DefaultTimeZone
        {
            get
            {
                return g_sDefaultTimeZone;
            }
            set
            {
                g_sDefaultTimeZone = value;
                TimeZones.DefaultTimeZone = g_sDefaultTimeZone;
            }
        }
		private void OnStartup(object sender, StartupEventArgs e) 
		{
			// Load the main control here
            bool httpResult = WebRequest.RegisterPrefix("http://", WebRequestCreator.ClientHttp);
            bool httpsResult = WebRequest.RegisterPrefix("https://", WebRequestCreator.ClientHttp);


            if (HtmlPage.Document.QueryString.ContainsKey("User") && HtmlPage.Document.QueryString.ContainsKey("Vacation"))
            {
                this.RootVisual = new Page(HtmlPage.Document.QueryString["User"], HtmlPage.Document.QueryString["Vacation"] );
            }
            else if (HtmlPage.Document.DocumentUri.AbsolutePath.Contains("/") ||HtmlPage.Document.DocumentUri.AbsolutePath.Contains("\\") )
            {
                string[] arStrings = HtmlPage.Document.DocumentUri.AbsolutePath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (arStrings.Length != 2)
                    this.RootVisual = new Page();
                else
                    this.RootVisual = new Page(arStrings[0], arStrings[1]);
            }
            else
            {
                this.RootVisual = new Page();
            }

            AppRootVisual = (Application.Current.RootVisual as Page).PageRoot;
		}

		private void OnExit(object sender, EventArgs e) 
		{
		}

		private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e) 
		{
			// If the app is running outside of the debugger then report the exception using
			// the browser's exception mechanism. On IE this will display it a yellow alert 
			// child in the status bar and Firefox will display a script error.
			if (!System.Diagnostics.Debugger.IsAttached)
			{

                try
                {
                    Log.Exception(e.ExceptionObject);
                }
                catch
                {
                }
				// NOTE: This will allow the application to continue running after an exception has been thrown
				// but not handled. 
				// For production applications this error handling should be replaced with something that will 
				// report the error to the website and stop the application.
				e.Handled = true;
				Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
			}
		}

		private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
		{
			try
			{
				string errorMsg = e.ExceptionObject.Message + @"\n" + e.ExceptionObject.StackTrace;
				errorMsg = errorMsg.Replace("\"", "\\\"").Replace("\r\n", @"\n");

				System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight 4 Application " + errorMsg + "\");");
			}
			catch (Exception)
			{
			}
		}
    }
}