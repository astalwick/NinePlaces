//#define MULTIVACATION

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using BasicEditors;
using Common.Interfaces;
using NinePlaces.Helpers;
using NinePlaces.Undo;
using NinePlaces.ViewModels;
using Common;
using System.IO;
using NinePlaces.Managers;


namespace NinePlaces
{
    public interface IZoomable
    {
        double Zoom { get; set; }
    }

    public interface ITimeline : IZoomable, IInterfaceElementWithVM, IDurationBasedElement
    {
        DateTime PositionToTime(double in_dXPosition);
        double TimeToPosition(DateTime in_dtToCheck);

        event SizeChangedEventHandler TimelineSizeChanged;

        event EventHandler TimelineLoadedEvent;
        bool TimelineLoaded  { get; }

        void ClearTimeline();

        void LoadVacations();

        void ShowVacation(IVacationViewModel in_oVM);
        void NewVacation();

        event EventHandler ActiveVacationChanged;
        IVacationViewModel ActiveVacation { get; }

        List<DockLine> DockLines { get; }

        void AnimateViewport(DateTime in_dtStart, TimeSpan in_tsDuration);
    }

	public partial class Timeline : UserControl, ITimeline
	{
        public event EventHandler DurationChanged;
        public event EventHandler StartTimeChanged;
        public event EventHandler TimelineLoadedEvent;
        public event SizeChangedEventHandler TimelineSizeChanged;

        public event EventHandler ActiveVacationChanged;

        public ITickRibbon TicksRibbon = null;

        private DispatcherTimer m_timerScroll = new DispatcherTimer();          // a timer to handle scrolling left/right.
        private TextBlock m_tbHoverDate = null;
        private Line m_lnHoverDateCursor = null;
        private bool m_bDraggingTimeline = false;       // true if we're scrolling the timeline.
        private Point m_oLastDragPosition;
        private IDraggable m_oDraggedItem = null;

        private double m_dPixelsPerHour = 0;             // the number of pixels that represent one day on the timeline.
        private Point m_ptLastMousePos;
        
        // the canvas scroll wheel zoom
        // this gets set into the model and remembered on next load.
        public double Zoom
        {
            get
            {
                return m_TimelineVM.TimelineZoom;
            }
            set
            {
                if (m_TimelineVM.TimelineZoom != value)
                {
                    SetZoom(value, 0.5);
                }
            }
        }

        public void UpdateZoomFromStartEnd()
        {
            double dZoom = Math.Log10(Duration.TotalDays / 3.65) * 20.0 - 20;
            Zoom = dZoom;
        }

        public List<DockLine> DockLines { get; set; }
        public void Disconnect()
        {
            // 
            // we have cleanup to do.
            //
            ActiveVacation = null;

            foreach (DockLine d in DockLines)
                d.Clear();
            
            TimelineCanvas.Children.Clear();
            m_oDraggedItem = null;
        }

        private ITimelineViewModel m_TimelineVM =  null; //new TimelineViewModel() as ITimelineViewModel; 
        protected ITimelineViewModel TimelineVM
        {
            get
            {
                return m_TimelineVM;
            }
            private set
            {
                if (value != m_TimelineVM)
                {
                    if (m_TimelineVM != null)
                    {
                        m_TimelineVM.DisconnectFromModel();
                        Disconnect();
                    }
                    
                    m_TimelineVM = value as ITimelineViewModel;
                    m_TimelineVM.ChildrenLoadedEvent += new EventHandler(m_TimelineVM_ChildrenLoadedEvent);
                    m_TimelineVM.ChildAdded += new VMChildChangedEventHandler(m_TimelineVM_ChildAdded);
                    m_TimelineVM.ChildRemoved += new VMChildChangedEventHandler(m_TimelineVM_ChildRemoved);
                    //
                    // the timelinevm needs to let us know when it's updated.
                    //
                    m_TimelineVM.LoadComplete += new EventHandler(TimelineLoadComplete);
                    m_TimelineVM.LoadError += new EventHandler(TimelineLoadError);

                    if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                        m_TimelineVM.AllowPersist = false;
                }
            }
        }
        void m_TimelineVM_ChildRemoved(object sender, VMChildChangedEventArgs args)
        {
            if (args.ChildVM is IVacationViewModel)
            {
                RemoveVacation(args.ChildVM as IVacationViewModel);
                if (ActiveVacation == args.ChildVM as IVacationViewModel)
                    ActiveVacation = null;
            }
        }

        void m_TimelineVM_ChildAdded(object sender, VMChildChangedEventArgs args)
        {
            if (!(args.ChildVM is IVacationViewModel))
                return;

            IVacationViewModel vm = args.ChildVM as IVacationViewModel;
            vm.LoadComplete += new EventHandler(ChildVM_LoadComplete);
            if (vm.Loaded && ActiveVacation == null && (m_TimelineVM.LastVacationUniqueID == 0 || m_TimelineVM.LastVacationUniqueID == vm.UniqueID))
            {
                ShowVacation(vm);
            }
        }

        void ChildVM_LoadComplete(object sender, EventArgs e)
        {
            IVacationViewModel vm = sender as IVacationViewModel;
            if (!string.IsNullOrEmpty(OverrideUserName))
            {

                // we're loading someone else's vacation.
                if (!string.IsNullOrEmpty(vm.VacationShortName) && vm.VacationShortName.ToLower() == OverrideVacation.ToLower())
                {
                    ShowVacation(vm);
                }
            }
            else if (ActiveVacation == null && (m_TimelineVM.LastVacationUniqueID == 0 || m_TimelineVM.LastVacationUniqueID == vm.UniqueID))
            {
                ShowVacation(vm);
            }

            CenterVacation();
        }

        public void CenterVacation()
        {
            if (ActiveVacation == null)
                return;

            if (ActiveVacation.SortedChildren<IDockableViewModel>().Count >= 1)
            {
                UpdateViewPort(ActiveVacation.FirstIconDate.AddDays(-1), (ActiveVacation.LastIconDate - ActiveVacation.FirstIconDate).Add(new TimeSpan(2, 0, 0, 0)));
            }            
        }

        public bool TimelineLoaded
        {
            get;
            internal set;
        }

        void m_TimelineVM_ChildrenLoadedEvent(object sender, EventArgs e)
        {
            OverrideUserName = string.Empty;
            TimelineLoaded = true;
            if( TimelineLoadedEvent != null )
                TimelineLoadedEvent.Invoke(this, new EventArgs());

            TimelineVM.SaveTimeline();

        }

        public IViewModel VM
        {
            get
            {
                return TimelineVM as IViewModel;
            }
            set
            {
                if (value == null)
                    TimelineVM = null;
                else
                {
                    System.Diagnostics.Debug.Assert(value is ITimelineViewModel);
                    TimelineVM = value as ITimelineViewModel;
                }
            }
        }

        public Control Control
        {
            get
            {
                return this as Control;
            }
        }

        #region StartTimeTicks (DependencyProperty)

        /// <summary>
        /// A description of the property.
        /// </summary>
        public double StartTimeTicks
        {
            get { return (double)GetValue(StartTimeTicksProperty); }
            set { SetValue(StartTimeTicksProperty, value); }
        }
        public static readonly DependencyProperty StartTimeTicksProperty =
            DependencyProperty.Register("StartTimeTicks", typeof(double), typeof(Timeline),
            new PropertyMetadata(0.0, new PropertyChangedCallback(OnStartTimeTicksChanged)));

        private static void OnStartTimeTicksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Timeline)d).OnStartTimeTicksChanged(e);
        }

        protected virtual void OnStartTimeTicksChanged(DependencyPropertyChangedEventArgs e)
        {
            TicksRibbon.StartTime = m_TimelineVM.TimelineViewStartTime = new DateTime(Convert.ToInt64((double)e.NewValue));

        }
        #endregion

        #region DurationTicks (DependencyProperty)

        /// <summary>
        /// A description of the property.
        /// </summary>
        public double DurationTicks
        {
            get { return (double)GetValue(DurationTicksProperty); }
            set { SetValue(DurationTicksProperty, value); }
        }
        public static readonly DependencyProperty DurationTicksProperty =
            DependencyProperty.Register("DurationTicks", typeof(double), typeof(Timeline),
            new PropertyMetadata(0.0, new PropertyChangedCallback(OnDurationTicksChanged)));

        private static void OnDurationTicksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Timeline)d).OnDurationTicksChanged(e);
        }

        protected virtual void OnDurationTicksChanged(DependencyPropertyChangedEventArgs e)
        {
            TicksRibbon.Duration = m_TimelineVM.TimelineViewDuration = new TimeSpan(Convert.ToInt64((double)e.NewValue));
            UpdateDaysPerPixel();
        }

        #endregion
        // timeline start time
        public DateTime StartTime
        {
            get
            {
                return m_TimelineVM.TimelineViewStartTime;
            }
            set
            {
                if (m_TimelineVM.TimelineViewStartTime != value)
                {
                    m_TimelineVM.TimelineViewStartTime = value;
                    if (StartTimeChanged != null)
                        StartTimeChanged.Invoke(this, new EventArgs());
                }
            }
        }

        public DateTime EndTime
        {
            get
            {
                return StartTime + Duration;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                return m_TimelineVM.TimelineViewDuration;
            }
            set
            {
                if (m_TimelineVM.TimelineViewDuration != value)
                {
                    m_TimelineVM.TimelineViewDuration = value;
                    UpdateDaysPerPixel();
                    if (DurationChanged != null)
                        DurationChanged.Invoke(this, new EventArgs());
                }
            }
        }

        public Timeline()
        {
            // Required to initialize variables
            InitializeComponent();
            TimelineLoaded = false;

            DockLines = new List<DockLine>();
            DockLines.Add(PhotoDockline);
            DockLines.Add(TravelDockline);
            DockLines.Add(LodgingDockline);
            DockLines.Add(ActivityDockline);

            foreach (DockLine d in DockLines)
                d.ParentTimeline = this;

            WhereAmI.ParentTimeline = this;
            //
            // begin the download of the RestModel xap file.
            //
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                TimelineVM = new TimelineViewModel() as ITimelineViewModel; 
                App.AssemblyLoadManager.LoadXap("REST_XML_Model.xap", new EventHandler(wcDownloadXap_OpenReadCompleted));
            }

            TicksRibbon = Ribbon as ITickRibbon;
            TicksRibbon.ParentTimeline = this;
            TicksRibbon.Clicked += new TickElementClickedEventHandler(TicksRibbon_Clicked);


            //
            // event handlers
            //
            SizeChanged += new SizeChangedEventHandler(Timeline_SizeChanged);

            UploadPhotos.Click += new RoutedEventHandler(UploadPhotos_Click);

            Loaded += new RoutedEventHandler(Timeline_Loaded);
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                // this handles mousewheel.
                new MouseWheelHelper(this).Moved += new EventHandler<NinePlaces.Helpers.MouseWheelEventArgs>(MouseWheelHandler);
            }
        }

        public string OverrideVacation { get; set; }
        public string OverrideUserName { get; set; }

        public void LoadVacation(IVacationViewModel v)
        {
            ShowVacation(v);
        }

        void TicksRibbon_Clicked(object sender, TickElementClickedEventArgs args)
        {
            DateTime dtStart = DateTime.Now;
            TimeSpan l_tsDuration = TimeSpan.MinValue;

            if (args.Duration == new TimeSpan(1, 0, 0))
            {
                dtStart = args.StartTime.AddMinutes(-5);
                l_tsDuration = args.Duration + new TimeSpan(0, 10, 0);
            }
            else if( args.Duration == new TimeSpan(1, 0, 0 ,0 ))
            {
                dtStart = args.StartTime.AddHours(-1);
                l_tsDuration = args.Duration + new TimeSpan(2, 0, 0);
            }
            else if( args.Duration > new TimeSpan( 25,0,0, 0) )
            {
                dtStart = args.StartTime.AddDays(-1);
                l_tsDuration = args.Duration + new TimeSpan(2, 0, 0, 0); 
            }


            // Animate the timeline to focus this element.
            AnimateViewport(dtStart, l_tsDuration);
        }

        void UploadPhotos_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofn = new OpenFileDialog();
            ofn.Filter = "Photos (.jpg)|*.jpg";
            ofn.Multiselect = true;
            if (ofn.ShowDialog() == true)
            {
                foreach (FileInfo fi in ofn.Files)
                {
                    NewPhoto(fi);
                }
            }
        }

        void wcDownloadXap_OpenReadCompleted(object sender, EventArgs e)
        {
            App.LoginControl.VM.AuthenticationStatusChanged += new AuthenticationEventHandler(LoginControl_AuthenticationStatusChanged);

            //
            // lets set up our default timezone.
            // eventually, this should be set based on user location.
            //
            SimpleTimeZoneDriver driver = new SimpleTimeZoneDriver();
            driver.TimeZoneID = "America/Montreal";
            driver.Start = DateTime.MinValue;
            App.DefaultTimeZone = driver;

            if (string.IsNullOrEmpty(OverrideUserName))
                m_TimelineVM.LoadTimeline();
            else
            {
                m_TimelineVM.LoadTimeline(OverrideUserName);
            }
        }

        void LoginControl_AuthenticationStatusChanged(object sender, AuthenticationEventArgs e)
        {
            UpdateUploadPhotoToolTip();
            if (!string.IsNullOrEmpty(OverrideUserName))
            {
                return;
            }

            //
            // we're logged in or not.
            //
            if (e.Authenticated)
            {
                // ok, do we have edits to the timeline?  if we do, we need to prompt the user 
                // to import them.
                if (ActiveVacation != null && ( ActiveVacation.WritePermitted == false || ActiveVacation.Children.Count == 0 ))
                {
                    ClearTimeline();
                }

                m_TimelineVM.LoadTimeline();
                
                return;
            }
            else if (!e.Failed)
            {
                // we're not authenticated, and nothing *failed*,
                // so we should clear the timeline - we've logged out.
                ClearTimeline();
            }
        }

        private void UpdateUploadPhotoToolTip()
        {
            if (!string.IsNullOrEmpty(OverrideUserName) || App.Timeline.ActiveVacation == null || !App.Timeline.ActiveVacation.WritePermitted)
            {
                UploadPhotosBorder.Visibility = Visibility.Collapsed;
                return;
            }

            UploadPhotosBorder.Visibility = Visibility.Visible;

            if (App.Timeline.ActiveVacation != null && App.Timeline.ActiveVacation.WritePermitted && App.LoginControl.VM.Authenticated)
            {
                UploadPhotos.IsEnabled = true;
                ToolTipService.SetToolTip(UploadPhotosBorder, string.Empty);
            }
            else
            {
                UploadPhotos.IsEnabled = false;
                ToolTipService.SetToolTip(UploadPhotosBorder, "Sign-in to attach photos to your vacation!");
            }
        }

        void Timeline_Loaded(object sender, RoutedEventArgs e)
        {
            TimelineRoot.MouseLeftButtonDown += new MouseButtonEventHandler(Timeline_MouseLeftButtonDown);
            TimelineRoot.MouseMove += new MouseEventHandler(Timeline_MouseMove);
            TimelineRoot.MouseLeftButtonUp += new MouseButtonEventHandler(Timeline_MouseLeftButtonUp);

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                App.IconDock.IconCreated += new IconDockEventHandler(Icon_IconCreated);
                UpdateViewPort(m_TimelineVM.TimelineViewStartTime, m_TimelineVM.TimelineViewDuration);
                Log.TrackInitCompleted();
                Log.TrackPageView("/NinePlaces/App");
            }
        }

        private void MouseWheelHandler(object sender, NinePlaces.Helpers.MouseWheelEventArgs e)
        {
            using (new NoUndo())
            {
                App.SelectionMgr.ClearSelection();
                double mouseDelta = 0.0;

                mouseDelta = -1.0 * Math.Sign(e.Delta);

                // we mark the event as handled EVEN IF we don't do anything here.  
                // we don't want the scroll of the ie window to happen
                e.Handled = true;

                // limit the range of zooming!
                if (Math.Abs(Zoom + mouseDelta) > 40)
                    return;

                GeneralTransform aobjGeneralTransform = ((UIElement)this).TransformToVisual(TimelineCanvas as UIElement);
                Point pt = aobjGeneralTransform.Transform(m_ptLastMousePos);

                double dTimelineWidth = TimelineCanvas.ActualWidth;
                double dRelativeMouseLocation = pt.X / dTimelineWidth;


                SetZoom(Math.Round(Zoom + mouseDelta), dRelativeMouseLocation);
            }
            
        }

        private void TimelineLoadError(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void TimelineLoadComplete(object sender, EventArgs e)
        {
            using (new NoUndo())
            {
                UpdateViewPort(m_TimelineVM.TimelineViewStartTime, m_TimelineVM.TimelineViewDuration);
            }
        }

        private void RemoveVacation(IVacationViewModel in_oVM)
        {
        }

        private IVacationViewModel m_oActiveVacationVM = null;
        public IVacationViewModel ActiveVacation
        {
            get
            {
                return m_oActiveVacationVM;
            }
            set
            {
                if (m_oActiveVacationVM != value)
                {
                    if( m_oActiveVacationVM != null )
                        m_oActiveVacationVM.ChildAdded -= new VMChildChangedEventHandler(VacationVM_ChildAdded);

                    m_oActiveVacationVM = value;

                    if( m_oActiveVacationVM != null )
                        m_oActiveVacationVM.ChildAdded += new VMChildChangedEventHandler(VacationVM_ChildAdded);

                    if (ActiveVacationChanged != null)
                        ActiveVacationChanged.Invoke(this, new EventArgs());

                    UpdateUploadPhotoToolTip();
                }
            }
        }

        public void NewVacation()
        {
            IVacationViewModel vm = null;
            using (new NoUndo())
            {
                vm = m_TimelineVM.NewVacation();
            }
            ShowVacation(vm);
        }

        public void LoadVacations()
        {
            ClearTimeline();
            TimelineVM.LoadTimeline();
        }

        public void ShowVacation(IVacationViewModel in_oVM)
        {
            if (in_oVM == ActiveVacation)
                return;

            App.TimeZones.NotifyOnZoneChange = false;
            // we cannot undo after switching from on vacation to another.
            if( m_TimelineVM.LastVacationUniqueID != 0 && m_TimelineVM.LastVacationUniqueID != in_oVM.UniqueID )
                App.UndoMgr.ClearUndo();

            Log.WriteLine("ShowVacation " + in_oVM.UniqueID);

            //
            // hide other vacations.
            //

            if (ActiveVacation != null)
            {
                foreach (DockLine d in DockLines)
                    d.Clear();

                TimelineCanvas.Children.Clear();
            }

            ActiveVacation = in_oVM;

            using (new NoUndo())
            {
                m_TimelineVM.LastVacationUniqueID = in_oVM.UniqueID;
            }

            foreach (IHierarchicalViewModel vm in in_oVM.Children)
            {
                AddVacationChild(vm);
            }

            App.TimeZones.NotifyOnZoneChange = true;

            CenterVacation();
        }

        private void Timeline_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDaysPerPixel();

            if (TimelineSizeChanged != null)
                TimelineSizeChanged.Invoke(sender, e); 
        }

        public void ClearTimeline()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                foreach (DockLine d in DockLines)
                    d.Clear();
                ActiveVacation = null;
                TimelineVM = new TimelineViewModel();
            }
        }

        private bool WireUpNewIcon(DockableControlBase in_oIcon)
        {
            in_oIcon.DragMoved += new EventHandler(Icon_DragMoved);
            in_oIcon.DragStarted += new EventHandler(Icon_DragStarted);
            in_oIcon.DragFinished += new EventHandler(Icon_DragFinished);
            in_oIcon.DestroyMe += new EventHandler(Icon_DestroyMe);
            TimelineCanvas.Children.Add(in_oIcon.Control);

            return true;
        }

        private NoUndo m_oDragUndoPrevent = null;
        private void Timeline_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //
            // ok, leftmousebuttondown on the timeline (that is not handled elsewhere)
            // could be the beginning of a timeline scroll.
            //
            m_bDraggingTimeline = true;
        }

        private void Timeline_MouseMove(object sender, MouseEventArgs e)
        {
            m_ptLastMousePos = e.GetPosition(this as UIElement);
            if (m_bDraggingTimeline)
            {
                if (!App.Dragging)
                {
                    m_oDragUndoPrevent = new NoUndo();
                    App.Dragging = true;
                    // we need to know where we started
                    m_oLastDragPosition = e.GetPosition(TimelineCanvas);
                    // and we need to track the mouse.
                    TimelineCanvas.CaptureMouse();
                }

                //
                // move the timeline left, then redraw ticks.
                //
                Point pt = e.GetPosition(TimelineCanvas);

                //
                // ok, figure out how many pixels we've moved,
                // and then convert that into days.
                //
                double dPixelsMoved = m_oLastDragPosition.X - pt.X;
                double dHours = dPixelsMoved / m_dPixelsPerHour;

                //
                // adjust the start/end of the timeline to 
                // account for the days moved.
                //
                StartTime = StartTime.AddHours(dHours);

                //
                // remember where we are
                //
                m_oLastDragPosition = pt;

                // and update.
            }

            //
            // draw the date hovertext if necessary
            //

            Point ptTicks = e.GetPosition(Ribbon);
            Point ptTicksActual = e.GetPosition(TimelineCanvas);
            if (ptTicks.Y - Ribbon.ActualHeight < 30 && ptTicks.Y > -30)
            {
                if (m_tbHoverDate == null)
                {
                    //
                    // create up the textblock for this month.
                    m_tbHoverDate = new TextBlock();
                    

                    m_tbHoverDate.Foreground = new SolidColorBrush(Color.FromArgb(150, 101, 101, 101));
                    m_tbHoverDate.FontFamily = new FontFamily("Portable User Interface");
                    m_tbHoverDate.FontSize = 8;
                    TimelineCanvas.Children.Add(m_tbHoverDate);

                    m_lnHoverDateCursor = new Line();

                    SolidColorBrush stroke = new SolidColorBrush(Color.FromArgb(100, 101, 101, 101));

                    m_lnHoverDateCursor.X1 = 0;
                    m_lnHoverDateCursor.X2 = 0;
                    m_lnHoverDateCursor.Y1 = 0;
                    m_lnHoverDateCursor.Y2 = 10;

                    m_lnHoverDateCursor.Stroke = stroke;

                    TimelineCanvas.Children.Add(m_lnHoverDateCursor);
                }

                double dTop = (ptTicksActual.Y - ptTicks.Y) + Ribbon.ActualHeight;

                DateTime dtUtc = PositionToTime(ptTicks.X);
                DateTime dtLocal = dtUtc.AddHours(App.TimeZones.OffsetAtTime(dtUtc));
                m_tbHoverDate.Text = dtLocal.ToCultureAwareString("dddd MMMM d");

                Canvas.SetLeft(m_tbHoverDate, ptTicks.X - (m_tbHoverDate.ActualWidth + 5));
                Canvas.SetTop(m_tbHoverDate, dTop + 5);

                Canvas.SetLeft(m_lnHoverDateCursor, ptTicks.X);
                Canvas.SetTop(m_lnHoverDateCursor, dTop);

                if (m_tbHoverDate.Visibility == Visibility.Collapsed)
                {
                    m_tbHoverDate.Visibility = Visibility.Visible;
                    m_lnHoverDateCursor.Visibility = Visibility.Visible;
                }

                // draw a little tick to indicate where the cursor really is.
            }
            else
            {
                if (m_tbHoverDate != null && m_tbHoverDate.Visibility == Visibility.Visible)
                {
                    m_tbHoverDate.Visibility = Visibility.Collapsed;
                    m_lnHoverDateCursor.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Timeline_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // we're done.  release mouse capture and forget all about the dragging.
            m_bDraggingTimeline = false;
            if (m_oDragUndoPrevent != null)
            {
                m_oDragUndoPrevent.Dispose();
                m_oDragUndoPrevent = null;
            }

            App.Dragging = false;
            TimelineCanvas.ReleaseMouseCapture();
            if (m_timerScroll.IsEnabled)
            {
                m_timerScroll.Tick -= new EventHandler(ScrollTimelineRight);
                m_timerScroll.Tick -= new EventHandler(ScrollTimelineLeft);
                m_timerScroll.Stop();
            }
        }

        private void SetZoom( double in_dNewZoom, double in_dRelMouseLocation )
        {
            m_TimelineVM.TimelineZoom = in_dNewZoom;

            // this basically creates a TOTAL timespan for the whole timeline of
            // 3.65 days at maximum zoom, 36.5 at default (starting) zoom, and 365 days at minimum zoom.
            TimeSpan tsTotalTimeline = TimeSpan.FromDays(3.65 * Math.Pow(10.0, ((Zoom + 20.0) / 20.0)));

            TimeSpan tsDiff = tsTotalTimeline.Subtract(Duration);

            UpdateViewPort(StartTime.AddDays(-1.0 * tsDiff.TotalDays * in_dRelMouseLocation), tsTotalTimeline);
        }

        private void UpdateDaysPerPixel()
        {
            m_dPixelsPerHour = TimelineCanvas.ActualWidth / Duration.TotalHours;
        }

        private AutoComplexUndo m_oDragUndo = null;
        private void Icon_DragStarted(object sender, EventArgs args)
        {
            IIcon ic = sender as IIcon;
            
            if (ic != null && ic.Control.Parent != TimelineCanvas && ic.DockLine == null)
            {
                // we're dragging a docked icon.
                ic.IconVM = IconRegistry.NewTempIconVM(ic.IconType);

                IDraggable iDragged = sender as IDraggable;

                ic.Control.Width = App.IconWidthHeight;
                ic.Control.Height = App.IconWidthHeight;

                // we're parent now.
                // transform the child's position to a new position relateive to us.
                GeneralTransform objGeneralTransform = App.AppRootVisual.TransformToVisual(TimelineCanvas);
                Point point = objGeneralTransform.Transform(iDragged.DragPosition());
                
                Canvas.SetLeft(ic.Control, point.X);
                Canvas.SetTop(ic.Control, point.Y);

                TimelineCanvas.Children.Add(ic.Control);
            }

            IDockable id = sender as IDockable;
            foreach (DockLine d in DockLines)
                d.DragStart(id);

            m_oDragUndo = new AutoComplexUndo();
        }

        private void Icon_DragMoved(object sender, EventArgs args)
        {
            // since we're parented to the timeline (by definition - by having
            // picked up the child for dragging), 
            // we can check if we're within the dock region by testing the top/left
            // of the child.  if the absolute value is within X, then we dock.
            IDraggable iDraggableElement = sender as IDraggable;
            IDockable iDockableElement = sender as IDockable;

            if (iDockableElement == null )
                return;

            GeneralTransform objGeneralTransform = App.AppRootVisual.TransformToVisual(TimelineCanvas);
            Point ptDockDrag = objGeneralTransform.Transform(iDraggableElement.DragPosition());
            double dLeft = ptDockDrag.X + (iDraggableElement.IconWidth / 2.0);
            iDockableElement.DockTime = PositionToTime(dLeft);


            IconDragScrollTimeline(iDraggableElement);

            if (iDockableElement.DockLine != null && iDockableElement.DockLine.IsOverDockLine( iDockableElement) ) // iDockableElement.DockLine.Dock(iDockableElement) )
            {
                return;
            }

            foreach (DockLine d in DockLines)
            {
                if (d.IsOverDockLine(iDockableElement) && d.Dock(iDockableElement))
                {
                    // the dockline container has taken responsibility for this object.
                    // no need to do anything else.
                    return;
                }
            }
            
            if ((iDockableElement as FrameworkElement).Parent != TimelineCanvas)
            {
                iDockableElement.DockLine = null;
                iDockableElement.MoveToPos(ptDockDrag, false);
                TimelineCanvas.Children.Add(iDockableElement as UIElement);

                return;
            }
        }

        private void Icon_DragFinished(object sender, EventArgs args)
        {
            if (m_timerScroll.IsEnabled)
            {
                m_timerScroll.Tick -= new EventHandler(ScrollTimelineRight);
                m_timerScroll.Tick -= new EventHandler(ScrollTimelineLeft);
                m_timerScroll.Stop();
            }

            IIcon ic = sender as IIcon;
            if (ic != null && !ic.Docked && !ic.IconVM.HasModel)
            {
                foreach (DockLine d in DockLines)
                {
                    if (d.AcceptsDock(ic as IDockable))
                    {
                        
                        App.Dragging = false;
                        m_oDragUndo.Dispose();
                        m_oDragUndo = null;

                        ic.DockTime = PositionToTime(Canvas.GetLeft(ic.Control));
                        ic.MoveCompleted += new EventHandler(ic_MoveCompleted);
                        
                        d.Dock(ic);
                        return;
                    }
                }
            }
            else if (ic != null && ic.Docked && !ic.IconVM.HasModel)
            {
                ConstructNewIcon( ic );
            }

            IDockable id = sender as IDockable;
            if (!id.Docked && (id as FrameworkElement).Parent == TimelineCanvas)
            {
                // we're responsible for it.  hide it, because it got 
                // dropped somewhere else.
                id.Hidden = true;
            }

            foreach (DockLine d in DockLines)
                d.DragEnd(id);

            App.Dragging = false;
            m_oDragUndo.Dispose();
            m_oDragUndo = null;
        }

        void ic_MoveCompleted(object sender, EventArgs e)
        {
            using (new AutoComplexUndo())
            {
                IIcon ic = sender as IIcon;
                ic.MoveCompleted -= new EventHandler(ic_MoveCompleted);
                ConstructNewIcon(ic);
            
                ic.Hidden = true;

                foreach (DockLine d in DockLines)
                    d.DragEnd(ic as IDockable);

            }
        }

        public void ConstructNewIcon(IIcon in_oIcon)
        {

            
            if (ActiveVacation == null)
            {
                ActiveVacation = m_TimelineVM.NewVacation();
                m_TimelineVM.LastVacationUniqueID = ActiveVacation.UniqueID;
            }

            // ok, we're creating a new child with a real VM to go on the timeline.
            IIconViewModel oNewVM = ActiveVacation.NewIcon(in_oIcon.IconVM);

            // hide the one that we dragged.  this is a hack.
            in_oIcon.Control.Opacity = 0;
            if (in_oIcon.Docked)
                in_oIcon.DockLine.Undock(in_oIcon);
        }

        public void NewPhoto(FileInfo in_fiPhotoFileInfo)
        {
            using (new AutoComplexUndo())
            {
                DateTime dDropTime = StartTime + Duration.Subtract(new TimeSpan(Duration.Ticks / 2));
                if (ActiveVacation == null)
                {
                    ActiveVacation = m_TimelineVM.NewVacation();
                }

                // ok, we're creating a new child with a real VM to go on the timeline.
                IPhotoViewModel oNewVM = ActiveVacation.NewPhoto(dDropTime, in_fiPhotoFileInfo);
            }
        }

        private void IconDragScrollTimeline(IDraggable in_oDraggable)
        {
            UIElement uiElement = in_oDraggable.Control;
            if (uiElement == null)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            double dX = Canvas.GetLeft(uiElement) + in_oDraggable.IconWidth / 2.0;

            double dScrollAreaWidth = TimelineCanvas.ActualWidth * 0.05;
            if (dX < dScrollAreaWidth)
            {
                if (!m_timerScroll.IsEnabled)
                {
                    m_oDraggedItem = in_oDraggable;
                    m_timerScroll.Interval = new TimeSpan(0, 0, 0, 0, 50);
                    m_timerScroll.Tick += new EventHandler(ScrollTimelineLeft);
                    m_timerScroll.Start();
                }
            }
            else if (dX > TimelineCanvas.ActualWidth - dScrollAreaWidth)
            {
                if (!m_timerScroll.IsEnabled)
                {
                    m_oDraggedItem = in_oDraggable;
                    m_timerScroll.Interval = new TimeSpan(0, 0, 0, 0, 50);
                    m_timerScroll.Tick += new EventHandler(ScrollTimelineRight);
                    m_timerScroll.Start();
                }
            }
            else
            {
                if (m_timerScroll.IsEnabled)
                {
                    m_oDraggedItem = in_oDraggable;
                    m_timerScroll.Tick -= new EventHandler(ScrollTimelineRight);
                    m_timerScroll.Tick -= new EventHandler(ScrollTimelineLeft);
                    m_timerScroll.Stop();
                }
            }
        }

        private void ScrollTimelineRight(object sender, EventArgs e)
        {
            double dToAdd = Duration.TotalDays * 0.015;
            StartTime = StartTime.AddDays(dToAdd);
            UpdateDraggedIconPosition(false);
        }

        private void ScrollTimelineLeft(object sender, EventArgs e)
        {
            double dToAdd = Duration.TotalDays * 0.015;
            StartTime = StartTime.AddDays(-1 * dToAdd);
            UpdateDraggedIconPosition(true);
        }

        private void UpdateDraggedIconPosition(bool in_bLeft)
        {
            if (m_oDraggedItem is IIcon)
            {
                UIElement uiElement = m_oDraggedItem.Control;
                if (uiElement == null)
                {
                    System.Diagnostics.Debug.Assert(false);
                    return;
                }

                double dScroll = TimelineCanvas.ActualWidth - TimelineCanvas.ActualWidth * 0.02;
                if (in_bLeft)
                    dScroll = TimelineCanvas.ActualWidth * 0.02;

                (m_oDraggedItem as IIcon).DockTime = PositionToTime(dScroll);
            }
        }
        
        void VacationVM_ChildAdded(object sender, VMChildChangedEventArgs args)
        {
            // we've added an child to this vacation.
            AddVacationChild(args.ChildVM);
        }

        void AddVacationChild(IViewModel in_oVM)
        {
            // we've added an child to this vacation.
            if (in_oVM is IIconViewModel)
            {
                IIconViewModel ivm = in_oVM as IIconViewModel;
                IconControlBase icB = IconRegistry.NewIcon(ivm.IconType, ivm);
                WireUpNewIcon(icB);

                if (ivm is IMutableIconProperties)
                {
                    IMutableIconProperties m = ivm as IMutableIconProperties;
                    foreach (DockLine d in DockLines)
                    {
                        if (d.Class == m.CurrentClass)
                            d.Dock(icB);
                    }
                }
                else
                {
                    foreach (DockLine d in DockLines)
                    {
                        d.Dock(icB);
                    }
                }
            }
            else if (in_oVM is IPhotoViewModel)
            {
                IPhotoViewModel ipvm = in_oVM as IPhotoViewModel;
                NinePlaces.Icons.PhotoIcon p = new NinePlaces.Icons.PhotoIcon(ipvm);

                // set the height.
                p.Width = 80;
                p.Height = 80;

                p.DragMoved += new EventHandler(Icon_DragMoved);
                p.DragStarted += new EventHandler(Icon_DragStarted);
                p.DragFinished += new EventHandler(Icon_DragFinished);
                p.DestroyMe += new EventHandler(Icon_DestroyMe);

                TimelineCanvas.Children.Add(p.Control);

                PhotoDockline.Dock(p as IDockable);
            }
        }

        private void Icon_DestroyMe(object sender, EventArgs e)
        {
            //
            // this is a notification from the child that it
            // has been disabled, and that it should be destroyed
            // and de-parented.
            //
            if (TimelineCanvas.Children.Contains(sender as UIElement))
            {
                (sender as UIElement).Visibility = Visibility.Collapsed;
                TimelineCanvas.Children.Remove(sender as UIElement);
            }
        }

        private void Icon_IconCreated(object sender, IconDockEventArgs args)
        {
            //
            // we need to know about every child drag event.
            //

            args.Icon.DestroyMe += new EventHandler(Icon_DestroyMe);
            
            args.Icon.DragMoved += new EventHandler(Icon_DragMoved);
            args.Icon.DragStarted += new EventHandler(Icon_DragStarted);
            args.Icon.DragFinished += new EventHandler(Icon_DragFinished);
        }


        public double TimeToPosition(DateTime in_dtToCheck)
        {
            //
            // now, figure out exactly where the child is supposed to be relative
            // to the start time of the timeline
            //
            TimeSpan ts = in_dtToCheck - StartTime;
            return ts.TotalHours * m_dPixelsPerHour;
        }

        public DateTime PositionToTime(double in_dXPosition)
        {
            double dLocation = in_dXPosition;
            DateTime dtActual = StartTime.AddHours(dLocation / m_dPixelsPerHour).AddMinutes(-0.5);        // -.5 accounts for rounding errors.
            DateTime dtRet = new DateTime(dtActual.Year, dtActual.Month, dtActual.Day, 0, 0, 0);

            //
            //
            // now, we need to push the time to its correct time increment
            // based on the zoom of the timeline. (based on pixels perday, rather)
            //
            //
            // half hour, hour, six hour, 12 hour, 1 day
            //

            double nHourIncrement = 0.0;

            if (m_dPixelsPerHour < 1.5 / 24.0)
                nHourIncrement = 24;
            else if (m_dPixelsPerHour < 3.0 / 24.0)
                nHourIncrement = 12;
            else if (m_dPixelsPerHour < 6.0 / 24.0)
                nHourIncrement = 6;
            else if (m_dPixelsPerHour < 12.0 / 24.0)
                nHourIncrement = 3;
            else if (m_dPixelsPerHour < 36.0 / 24.0)
                nHourIncrement = 1;
            else if (m_dPixelsPerHour < 400.0 / 24.0)
                nHourIncrement = 0.5;
            else
                nHourIncrement = 0.25;

            if (nHourIncrement > 0)
            {
                while (dtRet.CompareTo(dtActual) < 0)
                {
                    dtRet = dtRet.AddHours(nHourIncrement);
                }
            }

            return dtRet;
        }
        private NoUndo m_oUndoZoomPrevent = null;
        private DateTime m_dtAnimStart;
        private TimeSpan m_tsDuration;
        private Storyboard m_sbViewportAnimation = null;
        public void AnimateViewport(DateTime in_dtStartTime, TimeSpan in_tsDuration)
        {
            Log.WriteLine("AnimateViewport called", LogEventSeverity.Verbose);

            if (m_sbViewportAnimation != null)
            {
                m_sbViewportAnimation.Completed -= new EventHandler(m_sbViewportAnimation_Completed);
                m_sbViewportAnimation.SkipToFill();
            }

            Log.WriteLine("new undozoom", LogEventSeverity.Verbose);
            m_oUndoZoomPrevent = new NoUndo();

            m_tsDuration = in_tsDuration;
            m_dtAnimStart = in_dtStartTime;
            //
            // create the position animation.
            //
            m_sbViewportAnimation = new Storyboard();
            DoubleAnimation ptStart = new DoubleAnimation();
            ptStart.Duration = TimeSpan.FromSeconds(0.50);
            ptStart.From = (double)StartTime.Ticks;
            ptStart.To = (double)in_dtStartTime.Ticks;
            Storyboard.SetTarget(ptStart, this);
            PropertyPath pStart = new PropertyPath("StartTimeTicks");
            Storyboard.SetTargetProperty(ptStart, pStart);
            m_sbViewportAnimation.Children.Add(ptStart);

            DoubleAnimation ptEnd = new DoubleAnimation();
            ptEnd.Duration = TimeSpan.FromSeconds(0.50);
            ptEnd.From = (double)Duration.Ticks;
            ptEnd.To = (double)in_tsDuration.Ticks;
            Storyboard.SetTarget(ptEnd, this);
            PropertyPath pEnd = new PropertyPath("DurationTicks");
            Storyboard.SetTargetProperty(ptEnd, pEnd);
            m_sbViewportAnimation.Children.Add(ptEnd);


            m_sbViewportAnimation.Completed += new EventHandler(m_sbViewportAnimation_Completed);
            m_sbViewportAnimation.Begin();
        }

        void m_sbViewportAnimation_Completed(object sender, EventArgs e)
        {
            m_sbViewportAnimation = null;
            UpdateViewPort(m_dtAnimStart, m_tsDuration);
            Log.WriteLine("destroyed undozoom", LogEventSeverity.Verbose);
            if (m_oUndoZoomPrevent != null)
            {
                m_oUndoZoomPrevent.Dispose();
                m_oUndoZoomPrevent = null;
            }
        }

        private void UpdateViewPort(DateTime in_dtStart, TimeSpan in_tsDuration)
        {
            m_TimelineVM.TimelineViewDuration = in_tsDuration;
            m_TimelineVM.TimelineViewStartTime = in_dtStart;

            //
            // ok, we've figured out our timeline's time span, so redraw,
            //
            UpdateZoomFromStartEnd();
            UpdateDaysPerPixel();

            if (DurationChanged != null)
                DurationChanged.Invoke(this, new EventArgs());
            if (StartTimeChanged != null)
                StartTimeChanged.Invoke(this, new EventArgs());
        }
    }
}