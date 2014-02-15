using System;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Input;
using NinePlaces.IconDockControls;
using System.IO;
using System.Net;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Common;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Globalization;

namespace NinePlaces
{
    public partial class Page : UserControl
	{
        Popup pGetStarted = new Popup();
        Popup pFirstVisit = new Popup();
        public Page(string in_strUser, string in_strVacation) : this()
        {
            Timeline.OverrideUserName = in_strUser;
            Timeline.OverrideVacation = in_strVacation;
        }

		public Page()
		{
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                App.AssemblyLoadManager.LoadXap("REST_XML_Model.xap", new EventHandler(wcDownloadXap_OpenReadCompleted));
            }
			
            // Required to initialize variables
			InitializeComponent();

            App.IconDock = IconDock as IIconDock;
            App.Timeline = Timeline as ITimeline;

            Timeline.ActiveVacationChanged += new EventHandler(Timeline_ActiveVacationChanged);
            TitleBar.GetStartedButton.Click += new RoutedEventHandler(GetStartedButton_Click);
            Loaded += new RoutedEventHandler(Page_Loaded);
            KeyDown += new KeyEventHandler(Page_KeyDown);

            SizeChanged += new SizeChangedEventHandler(Page_SizeChanged);
		}

        void Timeline_ActiveVacationChanged(object sender, EventArgs e)
        {
            if (App.Timeline.ActiveVacation == null)
                IconDock.Visibility = Visibility.Visible;
            else if (!App.Timeline.ActiveVacation.WritePermitted)
                IconDock.Visibility = Visibility.Collapsed;
            else
                IconDock.Visibility = Visibility.Visible;
        }

        void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (pGetStarted.IsOpen)
                SizePopop(pGetStarted);
                       
            double dDesiredWidth = App.Current.Host.Content.ActualWidth;
            double dDesiredHeight = App.Current.Host.Content.ActualHeight;

            if (dDesiredWidth < 850 || dDesiredHeight < 600)
            {
                double dtoSetWidth = dDesiredWidth;
                double dtoSetHeight = dDesiredHeight;

                double dTransformY = 1.0;
                double dTransformX = 1.0;
                if (dDesiredHeight < 600)
                {

                    // no good.  needs to be a minimum of 550.
                    dTransformY = dtoSetHeight / 600.0;
                }
                if (dDesiredWidth < 850)
                {
                    //
                    // ok, lets figure out the transform that gets us to zero.
                    //
                    dTransformX = dtoSetWidth / 850.0;
                }


                if (dTransformY < dTransformX)
                    dTransformX = dTransformY;
                else if (dTransformX < dTransformY)
                    dTransformY = dTransformX;


                if (PageRoot.RenderTransform == null || !(PageRoot.RenderTransform is ScaleTransform))
                {
                    PageRoot.RenderTransform = new ScaleTransform();
                }

                (PageRoot.RenderTransform as ScaleTransform).ScaleX = dTransformX;
                (PageRoot.RenderTransform as ScaleTransform).ScaleY = dTransformY;
                PageContainer.Width = App.Current.Host.Content.ActualWidth * 1 / dTransformX;
                PageContainer.Height = App.Current.Host.Content.ActualHeight * 1 / dTransformY;
            }
            else
            {
                PageRoot.RenderTransform = null;
                PageContainer.Width = double.NaN;
                PageContainer.Height = double.NaN;

            }
        }

        void SizePopop(Popup in_pToSize)
        {
            in_pToSize.Child.UpdateLayout();

            double dWidth = App.Current.Host.Content.ActualWidth;
            double dHeight = App.Current.Host.Content.ActualHeight;

            
            FrameworkElement fe = in_pToSize.Child as FrameworkElement;

            double dDesiredHeight = fe.DesiredSize.Height;
            double dDesiredWidth = fe.DesiredSize.Width;
            double dTransformY = 1.0;
            double dTransformX = 1.0;
            if ((dHeight - 100) - fe.DesiredSize.Height < 0)
            {
                //
                // ok, lets figure out the transform that gets us to zero.
                //
                dTransformY = (dHeight - 100) / fe.DesiredSize.Height;
            }
            if ((dWidth - 100) - fe.DesiredSize.Width < 0)
            {
                //
                // ok, lets figure out the transform that gets us to zero.
                //
                dTransformX = (dWidth - 100) / fe.DesiredSize.Width;
            }

            if (dTransformY < dTransformX)
                dTransformX = dTransformY;
            else if ( dTransformX < dTransformY )
                dTransformY = dTransformX;


            dDesiredWidth = fe.DesiredSize.Width * dTransformX;
            dDesiredHeight = fe.DesiredSize.Height * dTransformY;

            if (in_pToSize.RenderTransform == null || !(in_pToSize.RenderTransform is ScaleTransform) )
            {
                in_pToSize.RenderTransform = new ScaleTransform();
            }

            (in_pToSize.RenderTransform as ScaleTransform).ScaleX = dTransformX;
            (in_pToSize.RenderTransform as ScaleTransform).ScaleY = dTransformY;

            in_pToSize.VerticalOffset = (dHeight / 2.0) - (dDesiredHeight / 2.0);
            in_pToSize.HorizontalOffset = (dWidth / 2.0) - (dDesiredWidth / 2.0);
        }



        void GetStartedButton_Click(object sender, RoutedEventArgs e)
        {
            if (!pGetStarted.IsOpen)
            {
                
                double dWidth = App.Current.Host.Content.ActualWidth;
                double dHeight = App.Current.Host.Content.ActualHeight;

                pGetStarted.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                pGetStarted.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                Editors.GetStartedPopup gs = new Editors.GetStartedPopup();
                gs.Close += new EventHandler(gs_Close);
                pGetStarted.Child = gs;
                
                

                pGetStarted.Child.LostFocus += new RoutedEventHandler(Child_LostFocus);
                pGetStarted.IsOpen = true;

                SizePopop(pGetStarted);
            }
        }

        void gs_Close(object sender, EventArgs e)
        {
            Log.TrackPageView("/NinePlaces/App"); 
            pGetStarted.IsOpen = false;
        }

        void Child_LostFocus(object sender, RoutedEventArgs e)
        {
            if (pGetStarted != null && pGetStarted.IsOpen)
            {
                Log.TrackPageView("/NinePlaces/App"); 
                pGetStarted.IsOpen = false;
            }
        }

        void wcDownloadXap_OpenReadCompleted(object sender, EventArgs e)
        {
            App.AssemblyLoadManager.LoadXap("Editors.xap", null);
        }

        void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control ||
                (Keyboard.Modifiers & ModifierKeys.Apple) == ModifierKeys.Apple)
            {
                if (e.Key == Key.Z)
                {
                    //Undo!
                    App.UndoMgr.Undo();
                    e.Handled = true;
                }
                else if (e.Key == Key.Y)
                {
                    //Redo!
                    App.UndoMgr.Redo();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape )
            {
                if(pGetStarted != null && pGetStarted.IsOpen)
                {
                    Log.TrackPageView("/NinePlaces/App"); 
                    pGetStarted.IsOpen = false;
                }
            }
        }

        void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //
            // we've created the timeline and the children.  
            // we need to tell them about one another.
            //
            MouseLeftButtonDown += new MouseButtonEventHandler(Page_MouseLeftButtonDown);

            if (FirstTimeVisit)
            {
                FirstVisitDialog fv = new FirstVisitDialog();

                pFirstVisit     = new Popup();
                pFirstVisit.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                pFirstVisit.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                fv.Close += new EventHandler(fv_Close);

                pFirstVisit.Child = fv;
                pFirstVisit.IsOpen = true;
                pFirstVisit.CenterPopup();

                FirstTimeVisit = false;
            }
        }

        public bool FirstTimeVisit
        {
            get
            {
                return !IsolatedStorageSettings.ApplicationSettings.Contains("NinePlaces");
            }
            set
            {
                if (FirstTimeVisit != value)
                {
                    if (!value)
                        IsolatedStorageSettings.ApplicationSettings.Add("NinePlaces", "true");
                    else
                        IsolatedStorageSettings.ApplicationSettings.Remove("NinePlaces");
                }

            }
        }


        void fv_Close(object sender, EventArgs e)
        {
            pFirstVisit.IsOpen = false;
        }

        void Page_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            App.SelectionMgr.ClearSelection();
            if(pGetStarted != null && pGetStarted.IsOpen)
            {
                Log.TrackPageView("/NinePlaces/App"); 
                pGetStarted.IsOpen = false;
            }
        }

        private void btnTwelveHour_Click(object sender, RoutedEventArgs e)
        {
            CrossAssemblyNotifyContainer.InvokeHourFormatChange(false);
        }

        private void btnTwentyFourHour_Click(object sender, RoutedEventArgs e)
        {
            CrossAssemblyNotifyContainer.InvokeHourFormatChange(true);
        }

        private void btnEnglish_Click(object sender, RoutedEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            CrossAssemblyNotifyContainer.InvokeLocalizationChange("en-US");
        }

        private void btnFrench_Click(object sender, RoutedEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");
            CrossAssemblyNotifyContainer.InvokeLocalizationChange("fr-FR");
        }
	}
}