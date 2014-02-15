using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using NinePlaces.Managers;
using NinePlaces.ViewModels;
using Common.Interfaces;
using Common;
using NinePlaces.Undo;
using NinePlaces.Helpers;

namespace NinePlaces
{
    public interface IDockable : IInterfaceElement
    {
        event EventHandler DockTimeChanged;
        DateTime DockTime{get;set;}
        bool Docked { get; }
        DockLine DockLine { get; set; }
        void MoveToPos(Point in_pt, bool in_bAnimate);
        void MoveToTime(DateTime in_dtNewTime, bool in_bAnimate);

        event EventHandler MoveCompleted;

        bool Hidden { get; set; }

        IconClass IconClass { get; }
        bool Stacked { get; set; }
        IStack Stack { get; set; }
        int StackOrder { get; set; }

        double IconWidth { get; }
        double IconHeight { get; }
        
    }

    public interface IDraggable : IInterfaceElement
    {
        bool Dragging { get; }
        event EventHandler DragStarted;
        event EventHandler DragMoved;
        event EventHandler DragFinished;
        Point DragPosition();

        double IconWidth { get; }
        double IconHeight { get; }
    }

    public interface IIcon : ISelectable, IDockable, IDraggable, IInterfaceElementWithVM
    {
        void RemoveIcon();
        IIconViewModel IconVM { get; set; }
        IconTypes IconType { get; }
        
        event EventHandler DestroyMe;
    }

    [TemplateVisualState(Name = "VSNotVisible", GroupName = "VSVisibility")]
    [TemplateVisualState(Name = "VSVisible", GroupName = "VSVisibility")]
    [TemplatePart(Name = "Content", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "BackgroundBrush", Type = typeof(Brush))]
    [TemplatePart(Name = "BaseCanvas", Type = typeof(Canvas))]
    [TemplatePart(Name = "IconHeight", Type = typeof(double))]
    [TemplatePart(Name = "IconWidth", Type = typeof(double))]
    [TemplatePart(Name = "IconScale", Type = typeof(ScaleTransform))]
    public abstract class DockableControlBase : ContentControl, IDockable, ISelectable, IInterfaceElementWithVM
    {
        #region Public members

        public double IconWidth
        {
            get { return (double)GetValue(IconWidthProperty); }
            set { SetValue(IconWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IconWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconWidthProperty =
            DependencyProperty.Register("IconWidth", typeof(double), typeof(DockableControlBase), new PropertyMetadata(App.IconWidthHeight));

        public double IconHeight
        {
            get { return (double)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IconHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconHeightProperty =
            DependencyProperty.Register("IconHeight", typeof(double), typeof(DockableControlBase), new PropertyMetadata(App.IconWidthHeight));

        public ScaleTransform IconScale
        {
            get { return (ScaleTransform)GetValue(IconScaleProperty); }
            set { SetValue(IconScaleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IconScale.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconScaleProperty =
            DependencyProperty.Register("IconScale", typeof(ScaleTransform), typeof(DockableControlBase), null);

        public Canvas BaseCanvas
        {
            get { return (Canvas)GetValue(BaseCanvasProperty); }
            set { SetValue(BaseCanvasProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BaseCanvas.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BaseCanvasProperty =
            DependencyProperty.Register("BaseCanvas", typeof(Canvas), typeof(DockableControlBase), null);

        public virtual Brush BackgroundBrush
        {
            get { return (Brush)GetValue(BackgroundBrushProperty); }
            set { SetValue(BackgroundBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackgroundBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register("BackgroundBrush", typeof(Brush), typeof(DockableControlBase), null);

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // wire up our template properties
            BaseCanvas = GetTemplateChild("BaseCanvas") as Canvas;
            IconScale = GetTemplateChild("IconScale") as ScaleTransform;

            LayoutUpdated += new EventHandler(IconControl_LayoutUpdated);

            // hide our timeline connector, but add it as a child.
            TimelineConnector.IsHitTestVisible = false;
            TimelineConnector.Visibility = Visibility.Collapsed;
            BaseCanvas.Children.Add(TimelineConnector);
            Duration.IsHitTestVisible = false;
            Duration.Visibility = Visibility.Collapsed;
            BaseCanvas.Children.Add(Duration);
            Canvas.SetZIndex(Duration, -100);

            // ditto, our date time.
            HoverText.IsHitTestVisible = false;
            BaseCanvas.Children.Add(HoverText);
            VisualStateManager.GoToState(HoverText as Control, "VSHidden", false);

            foreach (VisualStateGroup v in VisualStateManager.GetVisualStateGroups(BaseCanvas as FrameworkElement))
            {
                if (v.Name == "VSVisibility")
                {
                    //
                    // we need to add up a VisibilityStateChanged event, 
                    // so that when we've really been made invisible, we can 
                    // fire off a notification telling our parent to destroy us.
                    //
                    v.CurrentStateChanged += new EventHandler<VisualStateChangedEventArgs>(VisibilityStateChanged);
                }
            }
        }

        private IDetailsEditor m_oDetailsEditor = null;
        public IDetailsEditor DetailsEditor
        {
            get
            {
                return m_oDetailsEditor;
            }
            protected set
            {
                if (m_oDetailsEditor != value)
                    m_oDetailsEditor = value;
            }
        }

        private bool m_bInGravityWell = true;
        public double GravityWellStrength { get; set; }

        // DRAGGING
        private bool m_bDragged = false;
        private bool m_bDragging = false;   // are we dragging?
        public bool Dragging
        {
            get
            {
                return m_bDragging;
            }
            set
            {
                if (m_bDragging != value)
                {
                    App.Dragging = value;
                    m_bDragging = value;

                    VisualStateManager.GoToState(HoverText as Control, "VSHidden", false);
                    SetZIndexFromState();
                }
            }
        }

        private int m_nStackOrder = 0;
        public int StackOrder 
        { 
            get
            {
                return m_nStackOrder;
            }
            set
            {
                if (value != m_nStackOrder)
                {
                    m_nStackOrder = value;
                    SetZIndexFromState();
                }
            }
        }

        public void SetZIndexFromState()
        {
            int nZIndex = 0;
            if (Selected)
                nZIndex += 100;
            if (m_bDragging)
                nZIndex += 100;

            nZIndex += StackOrder;

            Canvas.SetZIndex(this, nZIndex);
        }

        private Point m_ptLastDragPosition;
        private Point m_ptMouseOffset;

        private bool m_bDraggingEnabled = true;
        protected bool DraggingEnabled
        {
            get { return m_bDraggingEnabled; }
            set { m_bDraggingEnabled = value; }
        }

        private TimelineConnector m_oTC = new TimelineConnector();  
        protected TimelineConnector TimelineConnector
        {
            get
            {
                return m_oTC;
            }
            set
            {
                m_oTC = value;
            }
        }


        private DurationIndicator m_oDuration = new DurationIndicator();
        protected DurationIndicator Duration
        {
            get
            {
                return m_oDuration;
            }
            set
            {
                m_oDuration = value;
            }
        }

        private bool m_bStacked = false;
        public bool Stacked
        {
            get
            {
                return m_bStacked;
            }
            set
            {
                if (m_bStacked != value)
                {
                    m_bStacked = value;
                    if (m_bStacked)
                    {
                        TimelineConnector.Visibility = Visibility.Collapsed;
                        Duration.Visibility = Visibility.Collapsed;
                        VisualStateManager.GoToState(this as Control, "Stacked", true);
                    }
                    else
                    {
                        if (Docked)
                        {
                            TimelineConnector.Visibility = Visibility.Visible;
                            Duration.Visibility = Visibility.Visible;
                        }
                        VisualStateManager.GoToState(this as Control, "NotStacked", true);
                    }

                }
            }
        }

        private IStack m_Stack = null;
        public IStack Stack
        {
            get
            {
                return m_Stack;
            }
            set
            {
                if (m_Stack != value)
                    m_Stack = value;
            }
        }

        DockLine m_oD = null;
        public virtual DockLine DockLine
        {
            get
            {
                return m_oD;
            }
            set
            {
                if (m_oD != value)
                {
                    DockLine dOld = m_oD;
                    
                    // set our current DockLine to null before we Undock,
                    // as Undock will re-enter this code to set m_oD to null
                    m_oD = null;
                    UIElement oldParent = this.Parent as UIElement;
                    if (dOld != null)
                        dOld.Undock(this);
                    if (Parent != null)
                        (Parent as Panel).Children.Remove(this);

                    // now set our real value before we carry on
                    m_oD = value;

                    if (m_oD != null )
                    {
                        if (oldParent == null)
                            System.Diagnostics.Debug.Assert(false);
                        // we're changing dock.  we need to update our X/Y to reflect the new parent.
                        GeneralTransform objGeneralTransform = oldParent.TransformToVisual(m_oD.LayoutRoot);
                        Point ptOldPos = objGeneralTransform.Transform(new Point(Canvas.GetLeft(this), Canvas.GetTop(this)));

                        // we're reparenting, so lets FORCE a canvas.x canvas.y update right now, so as to avoid flicker.
                        Canvas.SetTop(this, ptOldPos.Y);
                        Canvas.SetLeft(this, ptOldPos.X);

                        m_oD.DockedIcons.Children.Add(this);

                        Point ptNew = new Point(Canvas.GetLeft(this), 0.0);
                        MoveToPos(ptNew, Dragging);

                        HoverText.BackgroundBrush = BackgroundBrush = IconRegistry.BackgroundForClass(m_oD.Class);
                        

                        if (dOld == null && VM is ITimeZoneSlave)
                        {
                            App.TimeZones.RegisterIconVM(VM as ITimeZoneSlave);
                        }
                    }
                    else
                    {
                        TimelineConnector.Visibility = Visibility.Collapsed;
                        Duration.Visibility = Visibility.Collapsed;
						
						// we've removed the Icon from the Dockline - give it a generic (non-dockline) color
						// (we could perhaps make a specific Undocked color, rather than reuse generic?)
                        BackgroundBrush = Application.Current.Resources["GenericIconGradient"] as Brush;

                        if (dOld != null && VM is ITimeZoneSlave)
                        {
                            App.TimeZones.UnregisterIconVM(VM as ITimeZoneSlave);
                        }
                    }
                }
            }
        }

        private Storyboard m_sbDockTimeAnimation = null;

        // ICON PROPERTIES
        private IconHoverText m_dtDateTimeText = new IconHoverText(); // this is the date time text that floats above the child.
        protected IconHoverText HoverText
        {
            get
            {
                return m_dtDateTimeText;
            }
        }

        public virtual DateTime DockTime
        {
            get
            {
                return DockableVM.DockTime;
            }
            set
            {
                if (DockableVM.DockTime != value)
                {
                    DockableVM.DockTime = value;

                    if (DockTimeChanged != null)
                        DockTimeChanged.Invoke(this, new EventArgs());
                }
            }
        }
        

        // STATE
        private bool m_bHidden = true;      // are we hidden/visible?
        public bool Hidden
        {
            get
            {
                return m_bHidden;
            }
            set
            {
                if (value != m_bHidden)
                {
                    m_bHidden = value;
                    if (HiddenChanged != null)
                        HiddenChanged(this, new EventArgs());
                }
            }
        }

        public bool Docked
        {
            get
            {
                return DockLine != null;
            }
        }      // are we docked?

        #endregion


        #region Public events

        public event EventHandler DockTimeChanged;

        // Dragging
        public event EventHandler DragStarted;
        public event EventHandler DragMoved;
        public event EventHandler DragFinished;

        // Misc
        public event EventHandler DestroyMe;
        private event EventHandler HiddenChanged;

        #endregion

        // base classes implement the 'click()' method to 
        // handle mouseclicks on an icon.
        public virtual void Click()
        {
        }

        public abstract IconClass IconClass { get; }

        // one of our interfaces requires this.  really, it just redirects
        // to DockableVM.
        public IViewModel VM
        {
            get
            {
                return DockableVM as IViewModel;
            }
            set
            {
                if (value == null)
                    DockableVM = null;
                else
                {
                    System.Diagnostics.Debug.Assert(value is IDockableViewModel);
                    DockableVM = value as IDockableViewModel;
                }
            }
        }

        private IDockableViewModel m_oDockableVM = null;
        public IDockableViewModel DockableVM
        {
            get
            {
                return m_oDockableVM;
            }
            set
            {
                if (m_oDockableVM != value)
                {
                    //
                    // whoa dudes, we're setting a new Icon ViewModel
                    //

                    if (m_oDockableVM != null)
                        // we no longer care about the previous vm...
                        m_oDockableVM.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(VM_PropertyChanged);

                    m_oDockableVM = value;

                    if (m_oDockableVM != null)
                    {
                        m_oDockableVM.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(VM_PropertyChanged);
                        m_oDockableVM.Removed += new EventHandler(m_oDockableVM_Removed);
                    }

                    if (m_oDockableVM == null || !m_oDockableVM.HasModel || m_oDockableVM.Loaded)
                    {
                        DraggingEnabled = true;
                    }
                    else
                    {
                        // we're not allowed to drag *while we're loading*.  (which is what
                        // that if statement above means, basically)
                        DraggingEnabled = false;
                        m_oDockableVM.LoadComplete += new EventHandler(m_oIconVM_LoadComplete);

                    }

                    // set our datacontext and update everyone else's!
                    DataContext = m_oDockableVM;
                    HoverText.DataContext = m_oDockableVM;

                    // finally, if we can, set up the duration
                    Duration.DurationEnabled = DockableVM is IDurationIconViewModel && ((DockableVM as IDurationIconViewModel).DurationEnabled);
                }
            }
        }

        void m_oDockableVM_Removed(object sender, EventArgs e)
        {
            // this is a notification coming back fromm our viewmodel, telling us that
            // we have been removed.  all we need to do now is disconnect from our
            // viewmodel, and notify anyone that cares that we are no longer a real
            // icon.

            Disconnect();
        }

        void m_oIconVM_LoadComplete(object sender, EventArgs e)
        {
            // good news, we're loaded - we can re-enable dragging,
            // if it was disabled.
            DraggingEnabled = true;
        }

        protected virtual void VM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // intercept propertychanged notifications to do our animating to the right place.
            if (e.PropertyName == "DockTime" && !Dragging && DockLine != null)
            {
                DockLine.PositionIcon(this, true);
                if (DockTimeChanged != null)
                    DockTimeChanged.Invoke(this, new EventArgs());
            }
            else if (e.PropertyName == "Duration" && !App.Dragging)
            {
                UpdateDurationIndicator();
            }
            else if (e.PropertyName == "LocalDockTimeUnreachable")
            {
                VisualStateManager.GoToState(this as Control, DockableVM.LocalDockTimeUnreachable ? "WarnLocalDockTime" : "NoWarnLocalDockTime", true);
            }
        }

        public DockableControlBase()
        {
            this.DefaultStyleKey = typeof(DockableControlBase);

            App.SelectionMgr.AddToSelectionGroup("IconControls", this as ISelectable);

            VisualStateManager.GoToState(this as Control, "VSNotVisible", true);

            // wire up our events.
            MouseLeftButtonDown += new MouseButtonEventHandler(Icon_MouseLeftButtonDown);
            MouseLeftButtonUp += new MouseButtonEventHandler(Icon_MouseLeftButtonUp);
            MouseMove += new MouseEventHandler(Icon_MouseMove);
            MouseEnter += new MouseEventHandler(IconControl_MouseEnter);
            MouseLeave += new MouseEventHandler(IconControl_MouseLeave);

            HiddenChanged += new EventHandler(IconControl_HiddenChanged);
            Loaded += new RoutedEventHandler(IconControl_Loaded);

            IsTabStop = true;
        }

        protected void UpdateDurationIndicator()
        {
            if (Duration.DurationEnabled && DockLine != null)
            {
                IDurationIconViewModel idvm = DockableVM as IDurationIconViewModel;
                Duration.DurationWidth = DockLine.TimeToPosition(idvm.EndTime) - DockLine.TimeToPosition(idvm.DockTime);
            }
        }

        protected void UpdateDurationIndicator(double in_dDockX)        // where the X of the connector is explicitly set, not taken from the docktime
        {
            if (Duration.DurationEnabled && DockLine != null)
            {
                IDurationIconViewModel idvm = DockableVM as IDurationIconViewModel;
                if (idvm.StickyEndTime)
                    Duration.DurationWidth = DockLine.TimeToPosition(idvm.EndTime) - in_dDockX;
                else
                    UpdateDurationIndicator();
            }
        }

        protected void IconControl_HiddenChanged(object sender, EventArgs e)
        {
            if (!Hidden)
            {
                VisualStateManager.GoToState(this as Control, "VSVisible", true);
            }
            else
            {
                VisualStateManager.GoToState(HoverText as Control, "VSHidden", true);
                VisualStateManager.GoToState(this as Control, "VSNotVisible", true);
            }
        }

        protected void IconControl_Loaded(object sender, RoutedEventArgs e)
        {
            GravityWellStrength = IconWidth / 2.0;

            // seems there's a bug with silverlight - this gets called more than once.
            Loaded -= new RoutedEventHandler(IconControl_Loaded);
        }

        protected void VisibilityStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            if (Hidden)
            {
                //
                // ok, we've transitioned into the 'fully hidden' visual state.
                // that means it's time to disable ourself and ask our parent to 
                // destroy us.
                //
                IsEnabled = false;
                RemoveIcon();
                if (DestroyMe != null)
                {
                    App.SelectionMgr.RemoveFromSelectionGroup("IconControls", this as ISelectable);
                    DestroyMe(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// This is called by the view.
        /// </summary>
        public void RemoveIcon()
        {
            using (new AutoComplexUndo())
            {
                if (DockableVM != null && DockableVM.ParentViewModel != null)
                    DockableVM.ParentViewModel.RemoveChild(DockableVM as IHierarchicalViewModel);
            }
        }


        protected void IconControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Visibility == Visibility.Collapsed)
                return;

            CaptureMouse();
            if (!Selected)
            {
                if (Docked && !Dragging)
                {
                    //
                    // display the hover text. 
                    //
                    VisualStateManager.GoToState(HoverText as Control, "VSVisible", true);
                }
            }
            // else we keep our state
        }

        protected void IconControl_MouseLeave(object sender, MouseEventArgs e)
        {

            if (Visibility == Visibility.Collapsed)
                return;

            if (!Dragging)
            {
                ReleaseMouseCapture();
                if (!Selected)
                {
                    // hide the hover text
                    VisualStateManager.GoToState(HoverText as Control, "VSHidden", true);
                }
                // else we keep our state
            }
        }

        protected virtual void IconControl_LayoutUpdated(object sender, EventArgs e)
        {
            PositionDateTimeText();
            UpdateDurationIndicator();
            PositionDuration();
        }

        protected void PositionDateTimeText()
        {
            double dThisMidPoint = IconWidth / 2.0;
            double dTextMidPoint = HoverText.ActualHeight / 2.0;

            Canvas.SetLeft(HoverText, (m_dtDateTimeText.ActualWidth + 5.0) * -1.0);
            Canvas.SetTop(HoverText, dThisMidPoint - dTextMidPoint);
        }

        protected void PositionDuration()
        {
            if (!Docked || Stacked || !(this is IIcon) || BaseCanvas == null)
            {
                Duration.Visibility = Visibility.Collapsed;
                return;
            }

            //
            // the BOTTOM of our child is above the timeline.
            // we need to draw starting at the bottom and reaching down
            // to the timeline.
            //

            // ok, we need to get our BOTTOM value relative to the top of the timeline ribbon.
            GeneralTransform objGeneralTransform = TransformToVisual(((App.Timeline as Timeline).Ribbon as UIElement));
            Point ptThis = objGeneralTransform.Transform(new Point(Canvas.GetLeft(this), Canvas.GetTop(this)));

            Canvas.SetLeft(Duration, IconWidth / 2.0);
            Canvas.SetTop(Duration, IconHeight / 2.0 - 15.0);
            if (Duration.Visibility != Visibility.Visible)
                Duration.Visibility = Visibility.Visible;
        }

        protected void PositionTimelineConnector()
        {
            if (!Docked || Stacked || !(this is IIcon) || BaseCanvas == null)
            {
                TimelineConnector.Visibility = Visibility.Collapsed;
                return;
            }
            //
            // the BOTTOM of our child is above the timeline.
            // we need to draw starting at the bottom and reaching down
            // to the timeline.
            //

            // ok, we need to get our BOTTOM value relative to the top of the timeline ribbon.
            GeneralTransform objGeneralTransform = TransformToVisual(((App.Timeline as Timeline).Ribbon as UIElement));
            Point ptThis = objGeneralTransform.Transform(new Point(Canvas.GetLeft(this), Canvas.GetTop(this)));

            Canvas.SetLeft(TimelineConnector, IconWidth / 2.0);

            TimelineConnector.Height = Math.Abs(ptThis.Y + IconHeight);
            Canvas.SetTop(TimelineConnector, IconHeight);
            if (TimelineConnector.Visibility != Visibility.Visible)
                TimelineConnector.Visibility = Visibility.Visible;
        }

        public Point DragPosition()
        {
            // account for the mouse offset -- we return the topleft corner of the child
            return new Point(m_ptLastDragPosition.X - m_ptMouseOffset.X, m_ptLastDragPosition.Y - m_ptMouseOffset.Y);
        }

        #region Dragging events
        private void Icon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Hidden = false;     // this will cancel any disappearing child.
            if (DraggingEnabled)
            {
                // Set dragging to true
                Dragging = true;
                m_bDragged = false;
                m_ptMouseOffset = e.GetPosition(this as UIElement);

                // Store the start position
                m_ptLastDragPosition = e.GetPosition(App.AppRootVisual as UIElement);

                // Capture the mouse
                ((FrameworkElement)sender).CaptureMouse();

                if (!Selected)
                    App.SelectionMgr.ClearSelection();

                // Fire the drag started event
                if (DragStarted != null)
                {
                    DragStarted(this, new EventArgs());
                }
            }
            e.Handled = true;
        }

        private void Icon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DraggingEnabled && Dragging)
            {
                ((FrameworkElement)sender).ReleaseMouseCapture();

                if (!Selected)
                {
                    //VisualStateManager.GoToState(IconRoot as Control, "VSNormal", true);
                    VisualStateManager.GoToState(HoverText as Control, "VSHidden", true);
                }

                Point position = e.GetPosition(App.AppRootVisual as UIElement);
                // Fire the drag finished event
                if (DragFinished != null)
                {
                    DragFinished(this, new EventArgs());
                }

                m_ptLastDragPosition = position;
            }

            if (Dragging && !m_bDragged && DockableVM != null && DockableVM.Loaded)   // only select/click IF the model is loaded and ready.
            {
                Selected = true;
                Click();
            }

            m_bDragged = false;
            Dragging = false;
            m_bInGravityWell = true;
            //e.Handled = true;
        }

        private void Icon_MouseMove(object sender, MouseEventArgs e)
        {
            if (Dragging && (VM.WritePermitted || !VM.HasModel) )
            {

                Point position = e.GetPosition(App.AppRootVisual as UIElement);

                if (m_bInGravityWell)
                {
                    //
                    // we haven't moved far enough to break out of the
                    // gravity well.  lets check, though - maybe this is it!
                    //

                    if (Math.Abs(position.X - m_ptLastDragPosition.X) > GravityWellStrength ||
                        Math.Abs(position.Y - m_ptLastDragPosition.Y) > GravityWellStrength )
                    {
                        m_bInGravityWell = false;
                    }
                }

                Point positionRelParent = e.GetPosition(this.Parent as UIElement);

                if (!m_bDragged && IconWidth == App.StaticIconWidthHeight)
                    AnimateToSize(App.IconWidthHeight, App.IconWidthHeight);

                m_bDragged = true;

                //
                // HANDLE Y AXIS
                //
                if (DockLine != null && m_sbDockTimeAnimation == null)
                {
                    // we're docked.
                    Canvas.SetTop(this, 0.0);
                }
                else
                {
                    // we're not docked, so set the top freely, following the mousemove.
                    Canvas.SetTop(this, positionRelParent.Y - (IconHeight / 2.0));
                }

                //
                // HANDLE X AXIS
                //
                if (!m_bInGravityWell)
                {
                    double dLeft = Canvas.GetLeft(this);
                    // Move the panel
                    Canvas.SetLeft(
                        this,
                        dLeft + position.X - m_ptLastDragPosition.X);

                    // we need to explicitly set the durationindicator's left side, so that we don't get weird jumpiness due
                    // to time rounding.  (the docktime is always rounded to the nearest half hour or 15 mins or whatever - 
                    // but the actual xAuth location while we're dragging could map to something like 12:37pm.  if we allow
                    // updatedurationindicator to calculate itself using the docktime, then we'll get an offset).

                    UpdateDurationIndicator(dLeft + position.X - m_ptLastDragPosition.X + (IconWidth / 2.0));

                    // Fire the drag moved event
                    if (DragMoved != null)
                        DragMoved(this, new EventArgs());

                    // Update the last mouse position
                    m_ptLastDragPosition = position;
                }
            }
        }
        #endregion

        void DockTimeAnimation_Completed(object sender, EventArgs e)
        {
            TimelineConnector.Opacity = 100;
            if (m_sbDockTimeAnimation == null)
                return;

            m_sbDockTimeAnimation.Completed -= new EventHandler(DockTimeAnimation_Completed);
            m_sbDockTimeAnimation = null;
            
            PositionTimelineConnector();

            if (MoveCompleted != null)
                MoveCompleted.Invoke(this, new EventArgs());
        }

        private object AnimLock = new object();
        public void MoveToTime(DateTime in_dtTime, bool in_bAnimate)
        {
            if (!Docked)
                return;

            Point ptDest = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
            ptDest.X = DockLine.TimeToPosition(in_dtTime) - (IconWidth / 2.0);
            ptDest.Y = 0.0;

            MoveToPos(ptDest, in_bAnimate);
        }

        public void MoveToPos(Point in_pt, bool in_bAnimate)
        {
            if (in_bAnimate)
            {
                AnimateToPos(in_pt);
            }
            else
            {
                if (m_sbDockTimeAnimation != null)
                    m_sbDockTimeAnimation.SkipToFill();
                Canvas.SetTop(this, in_pt.Y);
                Canvas.SetLeft(this, in_pt.X);

                if (MoveCompleted != null)
                    MoveCompleted.Invoke(this, new EventArgs());
            }
        }

        public event EventHandler MoveCompleted;
        private void AnimateToPos(Point in_pt)
        {

            if (m_sbDockTimeAnimation != null)
                m_sbDockTimeAnimation.SkipToFill();

            TimelineConnector.Opacity = 0;

            Point ptCur = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));

            double dCurY = ptCur.Y;
            double dDestY = in_pt.Y;
            double dDiffY = dCurY - dDestY;

            double dCurX = ptCur.X;
            double dDestX = in_pt.X;
            double dDiffX = dCurX - dDestX;

            if (Math.Abs(dDiffX) < 0.00001 && Math.Abs(dDiffY) < 0.00001)
                return;
            //
            // create the position animation.
            //
            m_sbDockTimeAnimation = new Storyboard();
            if (Math.Abs(dDiffY) > 0.00001)
            {
                
                DoubleAnimation ptY = new DoubleAnimation();
                ptY.Duration = TimeSpan.FromSeconds(0.20);
                ptY.From = dCurY;
                ptY.To = dDestY;
                Storyboard.SetTarget(ptY, this);
                PropertyPath p = new PropertyPath("(Canvas.Top)");
                Storyboard.SetTargetProperty(ptY, p);
                m_sbDockTimeAnimation.Children.Add(ptY);
            }

            if (Math.Abs(dDiffX) > 0.00001)
            {
                DoubleAnimation ptX = new DoubleAnimation();
                ptX.Duration = TimeSpan.FromSeconds(0.20);
                ptX.From = dCurX;
                ptX.To = dDestX;
                Storyboard.SetTarget(ptX, this);
                PropertyPath pX = new PropertyPath("(Canvas.Left)");
                Storyboard.SetTargetProperty(ptX, pX);
                m_sbDockTimeAnimation.Children.Add(ptX);
            }

            if (TimelineConnector.Visibility == Visibility.Visible && TimelineConnector.ActualHeight > 0)
            {
                //
                // we must also animate the timeline connector's height.
                //
                DoubleAnimation pt2 = new DoubleAnimation();
                pt2.Duration = TimeSpan.FromSeconds(0.20);
                pt2.From = TimelineConnector.ActualHeight;
                pt2.To = Math.Abs(TimelineConnector.ActualHeight + dDiffY);//
                Storyboard.SetTarget(pt2, TimelineConnector);
                PropertyPath p2 = new PropertyPath("(Height)");
                Storyboard.SetTargetProperty(pt2, p2);
                m_sbDockTimeAnimation.Children.Add(pt2);
            }

            m_sbDockTimeAnimation.Completed += new EventHandler(DockTimeAnimation_Completed);
            m_sbDockTimeAnimation.Begin();
        }

        private Storyboard m_sbSelectedAnimation = null;
        private void AnimateToSize(double dWidth, double dHeight)
        {
            if (m_sbSelectedAnimation != null)
                m_sbSelectedAnimation.SkipToFill();

            m_sbSelectedAnimation = new Storyboard();

            DoubleAnimation ptW = new DoubleAnimation();
            ptW.Duration = TimeSpan.FromSeconds(0.20);
            ptW.From = IconWidth;
            ptW.To = dWidth;
            Storyboard.SetTarget(ptW, this);
            PropertyPath pW = new PropertyPath("IconWidth");
            Storyboard.SetTargetProperty(ptW, pW);
            m_sbSelectedAnimation.Children.Add(ptW);


            DoubleAnimation ptH = new DoubleAnimation();
            ptH.Duration = TimeSpan.FromSeconds(0.20);
            ptH.From = IconHeight;
            ptH.To = dHeight;
            Storyboard.SetTarget(ptH, this);
            PropertyPath pH = new PropertyPath("IconHeight");
            Storyboard.SetTargetProperty(ptH, pH);
            m_sbSelectedAnimation.Children.Add(ptH);

            m_sbSelectedAnimation.Begin();
        }

        protected virtual void SelectionChanged()
        {
            if (Selected)
            {
                // we're selecting this guy - therefore, 
                // bump up his zindex way above everythign else.
                SetZIndexFromState();
            }
            else
            {
                VisualStateManager.GoToState(this as Control, "VSNormal", true);

                SetZIndexFromState();
                //
                // if we're not selected, we should go back to our normal state
                // and hide our date/time text.
                //
                VisualStateManager.GoToState(HoverText as Control, "VSHidden", true);

                if (DetailsEditor != null)
                    DetailsEditor.Hide();
            }
        }

        // required by our interface.
        public Control Control
        {
            get
            {
                return this as Control;
            }
        }

        public void Disconnect()
        {
            //
            // this icon is being removed from the project, but it has 
            // a viewmodel.  tell the parent to remove its reference to
            // us, and then clear our own connection to the parent.
            //
            if (DockLine != null)
                DockLine = null;

            if (DockableVM.ParentViewModel != null)
                DockableVM.ParentViewModel = null;

            if (DockableVM != null)
                DockableVM = null;

            App.SelectionMgr.RemoveFromSelectionGroup("IconControls", this as ISelectable);
            DestroyMe(this, new EventArgs());
        }

        #region ISelectable Members

        public event SelectionEventHandler SelectionStateChanged;

        private bool m_bSelected = false;
        public bool Selected                // have we been selected?
        {
            get
            {
                return m_bSelected;
            }
            set
            {
                if (m_bSelected != value)
                {
                    m_bSelected = value;

                    SelectionChanged();

                    if (SelectionStateChanged != null)
                        SelectionStateChanged.Invoke(this as ISelectable, new SelectionEventArgs(m_bSelected));
                }
            }
        }

        #endregion
    }
}
