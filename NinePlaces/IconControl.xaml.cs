using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections;
using System.Collections.Generic;

namespace NinePlaces
{
	public partial class IconControl : UserControl
    {
        #region Public members

        private Storyboard m_sbDockLineAnimation = null;
        private bool m_bDragged = false;
        private bool m_bAboveTimeline = true;
        private Timeline m_oTimelineDockParent = null;              // who we're docked to.
        private TimelineConnector m_oTC = new TimelineConnector();  // this is our dock line.
        private TimelineConnector TimelineConnector
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
        private DateTime m_oDockTime;      // a date time that represents the time at which we've docked.
        public DateTime DockTime
        {
            get
            {
                return m_oDockTime;
            }
            set
            {
                m_oDockTime = value;
                m_dtDateTimeText.DateTimeValue = m_oDockTime;
            }
        }
        private int m_nDockLine = 0;
        public int DockLine 
        { 
            get
            {
                return m_nDockLine;
            } 
            set
            {
                if (m_nDockLine != value)
                {
                    m_nDockLine = value;
                    // when we set the dockline, we necessarily must make temporary dock line the same.
                    m_nTemporaryDockLine = m_nDockLine;
                    // zindex is relative to dock line.
                    Canvas.SetZIndex(this as UIElement, -1 * Math.Abs(m_nDockLine));
                    // finally, animate ourselves to our new position.
                    AnimateToDockline(m_nDockLine);
                } 
            } 
        }
        public bool OnTempDockLine
        {
            get
            {
                return TemporaryDockLine != DockLine;
            }
        }
        private int m_nTemporaryDockLine = 0;
        public int TemporaryDockLine
        {
            get
            {
                return m_nTemporaryDockLine;
            }
            set
            {
                if (m_nTemporaryDockLine != value)
                {
                    m_nTemporaryDockLine = value;
                    Canvas.SetZIndex(this as UIElement, -1 * Math.Abs(m_nTemporaryDockLine));
                    AnimateToDockline(m_nTemporaryDockLine);
                }
            }
        }
        private IconDateText m_dtDateTimeText = new IconDateText(); // this is the date time text that floats above the icon.
        private bool m_bDragging = false;   // are we dragging?
        public bool Dragging
        {
            get
            {
                return m_bDragging;
            }
        }
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
                    if( HiddenChanged != null )
                        HiddenChanged(this, new EventArgs());
                }
            }
        }
        public bool DockedToTimeline { get; set; }      // are we docked?
        private bool m_bDraggingEnabled = true;
        private bool m_bSelected = false;
        public bool Selected                // have we been selected?
        {
            get
            {
                return m_bSelected;
            }
            set
            {
                if (value != m_bSelected)
                {
                    m_bSelected = value;
                    if (SelectionChanged != null)
                    {   
                        // notify whoever is interested that we have been selected/deselected
                        SelectionChanged( this as UIElement, new SelChangedEventArgs(m_bSelected) );
                    }
                }
            }
        }
        private Point m_ptLastDragPosition;
        private Point m_ptMouseOffset;
        public bool DraggingEnabled
        {
            get { return m_bDraggingEnabled; }
            set { m_bDraggingEnabled = value; }
        }

        public string IconType
        {
            get
            {
                return IconRoot.IconType;
            }
        }

        #endregion


        #region Public events

        public event SelChangedEventHandler SelectionChanged;
        public event DragEventHandler DragStarted;
        public event EventHandler DestroyMe;
        public event EventHandler HiddenChanged;
        public event DragEventHandler DragMoved;
        public event DragEventHandler DragFinished;

        #endregion

        public IconControl(string in_strType)
        {

            // Required to initialize variables
            InitializeComponent();
            IconRoot.IconType = in_strType;

            // hide our timeline connector, but add it as a child.
            TimelineConnector.IsHitTestVisible = false;
            TimelineConnector.Visibility = Visibility.Collapsed;
            LayoutCanvas.Children.Add(TimelineConnector);

            // ditto, our date time.
            m_dtDateTimeText.IsHitTestVisible = false;
            LayoutCanvas.Children.Add(m_dtDateTimeText);
            VisualStateManager.GoToState(m_dtDateTimeText as Control, "VSHidden", false);

            // start out as notvisible
            VisualStateManager.GoToState(IconRoot as Control, "VSNotVisible", false);

            // wire up our events.
            HiddenChanged += new EventHandler(IconControl_HiddenChanged);
            MouseLeftButtonDown += new MouseButtonEventHandler(Icon_MouseLeftButtonDown);
            MouseMove += new MouseEventHandler(Icon_MouseMove);
            MouseLeftButtonUp += new MouseButtonEventHandler(Icon_MouseLeftButtonUp);
            MouseEnter += new MouseEventHandler(IconControl_MouseEnter);
            MouseLeave += new MouseEventHandler(IconControl_MouseLeave);
            Loaded += new RoutedEventHandler(IconControl_Loaded);
            SelectionChanged += new SelChangedEventHandler(IconControl_SelectionChanged);
            LayoutUpdated += new EventHandler(IconControl_LayoutUpdated);
        }

        void IconControl_SelectionChanged(object sender, SelChangedEventArgs args)
        {
            if (args.Selected)
            {
                //
                // if we're selected, we should display the selected state
                // and show our date/time text.
                //
                if (!IconRoot.GetAttention)
                {
                    VisualStateManager.GoToState(IconRoot as Control, "VSSelected", true);
                }

                VisualStateManager.GoToState(m_dtDateTimeText as Control, "VSVisible", true);

                // we're selecting this guy - therefore, 
                // bump up his zindex way above everythign else.
                if( IconRoot.ShowEditor( true ) )
                    Canvas.SetZIndex(this as UIElement, 1000);

            }
            else
            {
                if( IconRoot.ShowEditor( false ) )
                    Canvas.SetZIndex(this as UIElement, -1 * Math.Abs(m_nTemporaryDockLine));

                //
                // if we're not selected, we should go back to our normal state
                // and hide our date/time text.
                //
                if (!IconRoot.GetAttention)
                {
                    VisualStateManager.GoToState(IconRoot as Control, "VSNormal", true);
                }
                VisualStateManager.GoToState(m_dtDateTimeText as Control, "VSHidden", true);
            }
        }

        void IconControl_HiddenChanged(object sender, EventArgs e)
        {
            if (!Hidden)
            {
                VisualStateManager.GoToState(IconRoot as Control, "VSVisible", true);
            }
            else
            {
                VisualStateManager.GoToState(m_dtDateTimeText as Control, "VSHidden", true);
                VisualStateManager.GoToState(IconRoot as Control, "VSNotVisible", true);
            }
        }

        void IconControl_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (VisualStateGroup v in VisualStateManager.GetVisualStateGroups(LayoutCanvas as FrameworkElement))
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

        void VisibilityStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            if (Hidden)
            {
                //
                // ok, we've transitioned into the 'fully hidden' visual state.
                // that means it's time to disable ourself and ask our parent to 
                // destroy us.
                //
                IsEnabled = false;
                if( DestroyMe != null )
                    DestroyMe(this, new EventArgs());
            }
        }

        void IconControl_MouseEnter(object sender, MouseEventArgs e)
        {
            CaptureMouse();
            if (!Selected)
            {
                VisualStateManager.GoToState(IconRoot as Control, "VSHover", true);
                if( DockedToTimeline )
                    VisualStateManager.GoToState(m_dtDateTimeText as Control, "VSVisible", true);
            }
            // else we keep our state
        }

        void IconControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!m_bDragging)
            {
                ReleaseMouseCapture();
                if (!Selected)
                {
                    VisualStateManager.GoToState(IconRoot as Control, "VSNormal", true);
                    VisualStateManager.GoToState(m_dtDateTimeText as Control, "VSHidden", true);
                }
                // else we keep our state
            }
        }

        void IconControl_LayoutUpdated(object sender, EventArgs e)
        {
            LayoutCanvas.Width = ActualWidth;
            LayoutCanvas.Height = ActualHeight;
            IconRoot.Width = ActualWidth;
            IconRoot.Height = ActualHeight;

            PositionDateTimeText();
            PositionTimelineConnector();
        }


        private void PositionDateTimeText()
        {
            double dThisMidPoint = ActualWidth / 2;
            double dTextMidPoint = m_dtDateTimeText.ActualWidth / 2;
            Canvas.SetLeft(m_dtDateTimeText, dThisMidPoint - dTextMidPoint);
            if( m_bAboveTimeline )
                Canvas.SetTop(m_dtDateTimeText, -(m_dtDateTimeText.ActualHeight + 5));
            else
                Canvas.SetTop(m_dtDateTimeText, ActualHeight + 5 );
        }

        private void PositionTimelineConnector()
        {
            if (!DockedToTimeline)
            {
                return;
            }

            Canvas.SetLeft(TimelineConnector, ActualWidth / 2);
            double dTop = Canvas.GetTop(this as UIElement);
            if (m_bAboveTimeline && (dTop + ActualHeight) < 0)  // we're above
            {
                //
                // the BOTTOM of our icon is above the timeline.
                // we need to draw starting at the bottom and reaching down
                // to the timeline.
                //

                TimelineConnector.Height = Math.Abs(dTop + ActualHeight);
                Canvas.SetTop(TimelineConnector, ActualHeight);
                if (TimelineConnector.Visibility != Visibility.Visible)
                    TimelineConnector.Visibility = Visibility.Visible;
            }
            else if (!m_bAboveTimeline)
            {
                //
                // we're below the timeline, so we need to draw the connector
                // starting at the timeline and reaching down to our top.
                //
                TimelineConnector.Height = Math.Abs(dTop);
                Canvas.SetTop(TimelineConnector, -TimelineConnector.Height);
                if (TimelineConnector.Visibility != Visibility.Visible)
                    TimelineConnector.Visibility = Visibility.Visible;
            }
            else if( TimelineConnector.Visibility == Visibility.Visible )
            {
                //
                // it seems we're DIRECTLY above the timeline.  
                // make the connector invisible.
                //
                TimelineConnector.Visibility = Visibility.Collapsed;
            }
        }

        public Point DragPosition()
        {
            // account for the mouse offset -- we return the topleft corner of the icon
            return new Point(m_ptLastDragPosition.X - m_ptMouseOffset.X, m_ptLastDragPosition.Y - m_ptMouseOffset.Y); 
        }

        #region Dragging events
        private void Icon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Hidden = false;     // this will cancel any disappearing icon.
            if (m_bDraggingEnabled)
            {
                m_bDragged = false;
                m_ptMouseOffset = e.GetPosition(IconRoot as UIElement);

                // Store the start position
                m_ptLastDragPosition = e.GetPosition(Application.Current.RootVisual as UIElement);

                // Capture the mouse
                ((FrameworkElement)sender).CaptureMouse();

                // Set dragging to true
                m_bDragging = true;

                // Fire the drag started event
                if (DragStarted != null)
                {
                    DragStarted(this, new DragEventArgs(0, 0, e));
                }

                e.Handled = true;
            }
        }

        private void Icon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!m_bDragged)
            {
                (Application.Current as App).Selection.Select(this);
                IconRoot.Click();
            }

            if (m_bDraggingEnabled)
            {
                m_bDragged = false;
                // Capture the mouse
                ((FrameworkElement)sender).ReleaseMouseCapture();

                // Set dragging to true
                m_bDragging = false;

                Point position = e.GetPosition(Application.Current.RootVisual as UIElement);

                // Fire the drag finished event
                if (DragFinished != null)
                {
                    DragFinished(this, new DragEventArgs(position.X - m_ptLastDragPosition.X, position.Y - m_ptLastDragPosition.Y, e));
                }
            }
            

            e.Handled = true;
        }

        private void Icon_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_bDragging)
            {
                m_bDragged = true;
                Point position = e.GetPosition(Application.Current.RootVisual as UIElement);

                double dLeft = Canvas.GetLeft(this);
                // Move the panel
                Canvas.SetLeft(
                    this,
                    dLeft + position.X - m_ptLastDragPosition.X);


                if (!DockedToTimeline)
                {
                    // we're not docked, so set the top freely, following the mousemove.
                    Canvas.SetTop( this, Canvas.GetTop(this) + position.Y - m_ptLastDragPosition.Y);
                }
                else
                {
                    // we're docked - our Y position must remain constant.
                    Canvas.SetTop(this, m_oTimelineDockParent.GetDockLineYPosition(TemporaryDockLine));
                }

                // Fire the drag moved event
                if (DragMoved != null)
                {
                    DragMoved(this, new DragEventArgs(position.X - m_ptLastDragPosition.X, position.Y - m_ptLastDragPosition.Y, e));
                }

                // Update the last mouse position
                m_ptLastDragPosition = position;
                double dTop = Canvas.GetTop(this as UIElement);

                if (dTop > 0 && m_bAboveTimeline)  // we're below
                {
                    m_bAboveTimeline = false;
                }
                else if (dTop < 0 && !m_bAboveTimeline)
                {
                    m_bAboveTimeline = true;
                }
            }
        }
        #endregion

        public void Dock(Timeline in_tTimeline)
        {
            VisualStateManager.GoToState(IconRoot as Control, "VSDocked", true);
            m_oTimelineDockParent = in_tTimeline;
            DockedToTimeline = true;

            TimelineConnector.Visibility = Visibility.Visible;
            VisualStateManager.GoToState(m_dtDateTimeText as Control, "VSVisible", true);
        }

        public void Undock(Timeline in_tTimeline)
        {
            if (m_sbDockLineAnimation != null)
                m_sbDockLineAnimation.SkipToFill();

            VisualStateManager.GoToState(IconRoot as Control, "VSUndocked", true);
            DockedToTimeline = false;

            TimelineConnector.Visibility = Visibility.Collapsed;
            m_oTimelineDockParent = null;
            m_nDockLine = 0;
            m_nTemporaryDockLine = 0;
            VisualStateManager.GoToState(m_dtDateTimeText as Control, "VSHidden", true);
        }

        private void AnimateToDockline(int in_nDockLine)
        {
            if (!DockedToTimeline)
                return;

            if (m_sbDockLineAnimation != null)
            {
                m_sbDockLineAnimation.SkipToFill();
                m_sbDockLineAnimation = null;
            }

            double dCurTop = Canvas.GetTop(this);
            double dCurHeight = ActualWidth;
            double dDestTop = m_oTimelineDockParent.GetDockLineYPosition(in_nDockLine);
            double dDiff = dCurTop - dDestTop;

            //Canvas.SetTop(this, dDestTop);
            //return;

            //
            // create the position animation.
            //
            m_sbDockLineAnimation = new Storyboard();
            DoubleAnimation pt = new DoubleAnimation();
            pt.Duration = TimeSpan.FromSeconds(0.20);
            pt.From = dCurTop;
            pt.To = dDestTop;
            Storyboard.SetTarget(pt, this);
            PropertyPath p = new PropertyPath("(Canvas.Top)");
            Storyboard.SetTargetProperty(pt, p);
            m_sbDockLineAnimation.Children.Add(pt);

            if (TimelineConnector.Visibility == Visibility.Visible && TimelineConnector.ActualHeight > 0)
            {
                //
                // we must also animate the timeline connector's height.
                //
                DoubleAnimation pt2 = new DoubleAnimation();
                pt2.Duration = TimeSpan.FromSeconds(0.20);
                pt2.From = TimelineConnector.Height;
                pt2.To = Math.Abs( TimelineConnector.Height + dDiff );//
                Storyboard.SetTarget(pt2, TimelineConnector);
                PropertyPath p2 = new PropertyPath("(Height)");
                Storyboard.SetTargetProperty(pt2, p2);
                m_sbDockLineAnimation.Children.Add(pt2);
            }

            m_sbDockLineAnimation.Begin();
        }
        //
        // this is just some sample code to demonstrate 
        // for myself how to programmatically create an 
        // animation of a control's size
        //
        private void BeginAnimation()
        {
            //
            // make it bigger!  100x100.
            //
            double dCurLeft = Canvas.GetLeft(this);
            double dCurTop = Canvas.GetTop(this);
            double dCurHeight = ActualHeight;
            double dCurWidth = ActualWidth;

            Storyboard sb = new Storyboard();
            DoubleAnimation pt = new DoubleAnimation();
            pt.Duration = TimeSpan.FromSeconds(0.5);
            pt.From = dCurLeft;
            pt.To = dCurLeft - ((100 - dCurWidth) / 2);// Canvas.GetLeft(this) + 200;
            Storyboard.SetTarget(pt, this);
            PropertyPath p = new PropertyPath("(Canvas.Left)");
            Storyboard.SetTargetProperty(pt, p);
            sb.Children.Add(pt);

            DoubleAnimation pt2 = new DoubleAnimation();
            pt2.Duration = TimeSpan.FromSeconds(0.5);
            pt2.From = dCurTop;
            pt2.To = dCurTop - (100 - dCurHeight);
            Storyboard.SetTarget(pt2, this);
            p = new PropertyPath("(Canvas.Top)");
            Storyboard.SetTargetProperty(pt2, p);
            sb.Children.Add(pt2);

            DoubleAnimation pt3 = new DoubleAnimation();
            pt3.Duration = TimeSpan.FromSeconds(0.5);
            pt3.From = dCurWidth;
            pt3.To = 100;
            Storyboard.SetTarget(pt3, this);
            p = new PropertyPath("(FrameworkElement.Width)");
            Storyboard.SetTargetProperty(pt3, p);
            sb.Children.Add(pt3);

            DoubleAnimation pt4 = new DoubleAnimation();
            pt4.Duration = TimeSpan.FromSeconds(0.5);
            pt4.From = dCurHeight;
            pt4.To = 100;
            Storyboard.SetTarget(pt4, this);
            p = new PropertyPath("(FrameworkElement.Height)");
            Storyboard.SetTargetProperty(pt4, p);
            sb.Children.Add(pt4);

            sb.Begin();
        }
	}
}