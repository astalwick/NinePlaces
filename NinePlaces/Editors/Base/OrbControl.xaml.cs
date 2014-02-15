using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NinePlaces
{
    public interface IDetailsEditor : IInterfaceElement
    {
    }

    public interface IOrbControl : IInterfaceElement
    {
        bool PositionOrbOnLeft { get; set; }
        bool IsSlideOutOpen { get; set; }
        UserControl SlideOutTitleControl { get; set; }
        string SlideOutTitleText { set; }
        IDetailsEditor SlideOutEditor { get; set; }
        Color Color { get; set; }
    }

	public partial class OrbControl : UserControl, IOrbControl
	{
        private double m_dTop = 0;                  // the adjusted top - set when a user drags the editor.
        private double m_dHeight = 0;               // the adjusted height - set when a user resizes the editor.
        private double m_dWidth = 250;              // the adjusted width - set when a u ser resizes the editor.
        private double m_dKeepOnscreenAdjustY = 0;  // the keep-on-screen adjustment.  set when an editor is about to pop-up partially offscreen.


        private bool m_bMouseDown = false;              // bool to help us keep track of our state.
        private Point m_ptMouseDown;                            // this point represents the LOCATION of the original mousedown, relative to this.
        private ResizeSide m_rsResizing = ResizeSide.None;      // keep track of which edge we've grabbed.
        private double m_dMouseDownWidth = 0.0;         // keep track of the width and height of the editor when we began the drag.
        private double m_dMouseDownHeight = 0.0;        // keep track of the width and height of the editor when we began the drag.
        private bool m_bDragged = false;                // did we drag the control while in the mousedown operation?

        private enum ResizeSide
        {
            None,
            Top = 1,
            Left = 2,
            Right = 4,
            Bottom = 8
        };

        private Storyboard m_sbSlideOut = new Storyboard();
        private Storyboard m_sbSlideIn = new Storyboard();
        private bool m_bAlignLeft = false;
        public bool PositionOrbOnLeft
        {
            get
            {
                return m_bAlignLeft;
            }
            set
            {
                if (m_bAlignLeft == value)
                    return;

                m_bAlignLeft = value;
                if (PositionOrbOnLeft)
                {
                    SlideOutGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    SlideOutGrid.ColumnDefinitions[1].Width = new GridLength(OrbBorder.Width);

                    SlideOutOuterBorder.HorizontalAlignment = HorizontalAlignment.Right;
                    OrbBorder.HorizontalAlignment = HorizontalAlignment.Right;

                    if( SlideOutTitleControl != null )
                        SlideOutTitleControl.SetValue(Grid.ColumnProperty, 0);


                }
                else
                {
                    SlideOutGrid.ColumnDefinitions[0].Width = new GridLength(OrbBorder.Width);
                    SlideOutGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

                    SlideOutOuterBorder.HorizontalAlignment = HorizontalAlignment.Left;
                    OrbBorder.HorizontalAlignment = HorizontalAlignment.Left;

                    if (SlideOutTitleControl != null)
                        SlideOutTitleControl.SetValue(Grid.ColumnProperty, 1);
                }

                //
                // update the storyboards
                //

                if (m_sbSlideOut != null)
                {
                    m_sbSlideOut.SkipToFill();
                    m_sbSlideOut = new Storyboard();
                }
                if (m_sbSlideIn != null)
                {
                    m_sbSlideIn.SkipToFill();
                    m_sbSlideIn = new Storyboard();
                }

                PropertyPath p = new PropertyPath("(Canvas.Left)");

                DoubleAnimation ptSlideOut = new DoubleAnimation();
                ptSlideOut.Duration = TimeSpan.FromSeconds(0.10);
                ptSlideOut.From = Canvas.GetLeft(SlideOutOuterBorder);
                ptSlideOut.To = PositionOrbOnLeft ? -1 * 200 + OrbBorder.Width : 0;     // we hardcode a width of 200 for now.
                Storyboard.SetTarget(ptSlideOut, SlideOutOuterBorder);
                Storyboard.SetTargetProperty(ptSlideOut, p);
                m_sbSlideOut.Children.Add(ptSlideOut);
                m_sbSlideOut.Begin();


                DoubleAnimation ptSlideIn = new DoubleAnimation();
                ptSlideIn.Duration = TimeSpan.FromSeconds(0.10);
                ptSlideIn.From = Canvas.GetLeft(SlideOutOuterBorder);
                ptSlideIn.To = 0;
                Storyboard.SetTarget(ptSlideIn, SlideOutOuterBorder);
                Storyboard.SetTargetProperty(ptSlideIn, p);
                m_sbSlideIn.Children.Add(ptSlideIn);
                m_sbSlideIn.Begin();

            }
        }

        private bool m_bIsSlideOutOpen = false;
        public bool IsSlideOutOpen
        {
            get
            {
                return m_bIsSlideOutOpen;
            }
            set
            {
                if (m_bIsSlideOutOpen != value)
                {
                    IsEditorVisible = false;
                    m_bIsSlideOutOpen = value;

                    if (m_bIsSlideOutOpen)
                    {
                        SlideOutOuterBorder.Height = OrbBorder.Height;
                        Focus();

                        VisualStateManager.GoToState(this, "VSSelected", true);

                        if (m_sbSlideOut.Children.Count > 0)
                        {
                            (m_sbSlideOut.Children[0] as DoubleAnimation).From = Canvas.GetLeft(SlideOutOuterBorder);
                            m_sbSlideOut.Begin();
                        }

                        SlideOutTitleControl.Focus();
                    }
                    else
                    {
                        SlideOutOuterBorder.Height = OrbBorder.Height;
                        Canvas.SetTop(SlideOutOuterBorder, 0);
                        VisualStateManager.GoToState(this, "VSNormal", true);
                        if (m_sbSlideIn.Children.Count > 0)
                        {
                            (m_sbSlideIn.Children[0] as DoubleAnimation).From = Canvas.GetLeft(SlideOutOuterBorder);
                            m_sbSlideIn.Begin();
                        }
                    }
                }
            }
        }

        private bool IsEditorVisible
        {
            get;
            set;
        }

        private UserControl m_oSlideOutTitleControl = null;
        public UserControl SlideOutTitleControl
        {
            get
            {
                return m_oSlideOutTitleControl;
            }
            set
            {
                if (SlideOutTitleControl != null)
                {
                    SlideOutGrid.Children.Remove(SlideOutTitleControl);
                }
                m_oSlideOutTitleControl = value;
                SlideOutGrid.Children.Add(m_oSlideOutTitleControl);
                m_oSlideOutTitleControl.SetValue(Grid.ColumnProperty, PositionOrbOnLeft ? 0 : 1);
                m_oSlideOutTitleControl.SetValue(Grid.RowProperty, 0);

                SlideOutOuterBorder.Height = OrbBorder.ActualHeight;
            }
        }

        public string SlideOutTitleText
        {
            set
            {
                if (SlideOutTitleControl != null)
                {
                    SlideOutGrid.Children.Remove(SlideOutTitleControl);
                }
                SlideOutTitleControl = new QuickEditControl(value);
            }
        }

        private IDetailsEditor m_oSlideOutEditor = null;
        public IDetailsEditor SlideOutEditor
        {
            get
            {
                return m_oSlideOutEditor;
            }
            set
            {
                if (SlideOutEditor != null)
                {
                    SlideOutGrid.Children.Remove(SlideOutEditor.Control);
                    SlideOutEditor.Control.SizeChanged -= new SizeChangedEventHandler(m_oSlideOutEditor_SizeChanged);
                }
                m_oSlideOutEditor = value;
                SlideOutGrid.Children.Add(m_oSlideOutEditor.Control);
                m_oSlideOutEditor.Control.SetValue(Grid.ColumnProperty, 0);
                m_oSlideOutEditor.Control.SetValue(Grid.RowProperty, 1);
                m_oSlideOutEditor.Control.SetValue(Grid.ColumnSpanProperty, 2);

                m_oSlideOutEditor.Control.SizeChanged += new SizeChangedEventHandler(m_oSlideOutEditor_SizeChanged);

                SlideOutOuterBorder.Height = OrbBorder.ActualHeight;
            }
        }

        private void m_oSlideOutEditor_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (m_dHeight == SlideOutEditor.Control.ActualHeight)
                return;
            
            if (IsSlideOutOpen && IsEditorVisible)
            {
                m_dHeight = SlideOutEditor.Control.ActualHeight;
                if (m_sbPositionAnim != null && !m_bMouseDown)
                {
                    // get our keep-on-screen adjustment.
                    double dThrowAway = 0;
                    double dCurTop = Canvas.GetTop(SlideOutOuterBorder);
                    KeepOnScreen(SlideOutEditor.Control, dCurTop, 0, m_dHeight + OrbBorder.ActualHeight, m_dWidth, ref dThrowAway, ref m_dKeepOnscreenAdjustY);

                    double dOrbTop = Canvas.GetTop(OrbBorder);
                    double dOrbBottom = dOrbTop + OrbBorder.ActualHeight;
                    if (dCurTop + m_dKeepOnscreenAdjustY + m_dHeight + OrbBorder.ActualHeight < dOrbBottom)
                    {
                        // no deal - dragged too far, so constrain.
                        m_dKeepOnscreenAdjustY = dCurTop + dOrbBottom + m_dHeight + OrbBorder.ActualHeight;
                    }

                    double dNewTop = dCurTop + m_dKeepOnscreenAdjustY;
                    System.Diagnostics.Debug.WriteLine("Current top: " + dCurTop.ToString() + " New Top: " + dNewTop.ToString());
                    Canvas.SetTop(SlideOutOuterBorder, dNewTop);

                    m_dKeepOnscreenAdjustY = m_dTop + dNewTop;
                }

                SlideOutOuterBorder.Height = m_dHeight + OrbBorder.ActualHeight;
            }
        }

        public Color Color
        {
            get
            {
                return ((SolidColorBrush)FillGradientStop.Fill).Color;
            }
            set
            {
                FillGradientStop.Fill = new SolidColorBrush(value);
                SlideOutBorder.Fill = new SolidColorBrush(value);
                //SlideOutBorder.Background = new SolidColorBrush( value );
            }
        }

        public OrbControl()
        {
            // Required to initialize variables
            InitializeComponent();
            MouseLeftButtonDown += new MouseButtonEventHandler(OrbControl_MouseLeftButtonDown);
            MouseLeftButtonUp += new MouseButtonEventHandler(OrbControl_MouseLeftButtonUp);
            MouseMove += new MouseEventHandler(OrbControl_MouseMove);
        }

        public OrbControl(string in_strLabel)
        {
            // Required to initialize variables
            InitializeComponent();
            MouseLeftButtonDown += new MouseButtonEventHandler(OrbControl_MouseLeftButtonDown);
            MouseLeftButtonUp += new MouseButtonEventHandler(OrbControl_MouseLeftButtonUp);
            MouseMove += new MouseEventHandler(OrbControl_MouseMove);
            SlideOutTitleControl = new QuickEditControl(in_strLabel);
        }

        private void OrbControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsEditorVisible)
            {
                // this only matters for the editor.  the editor can be
                // resized and dragged around.
                return;
            }

            if (m_bMouseDown)       // we're dragging something!
            {
                if (!m_bDragged)
                {
                    m_dKeepOnscreenAdjustY = 0;
                    m_dTop = Canvas.GetTop(SlideOutOuterBorder);
                }
                m_bDragged = true;

                if (m_rsResizing == ResizeSide.None)
                {
                    //
                    // ok, we're not resizing, we're actually dragging.
                    // note: we constrain the drag to the Y axis, and we DO NOT allow
                    // the user to detach the editor from its orb.
                    //

                    // where are we.
                    Point ptThis = e.GetPosition(this as UIElement);
                    double dDiffY = (ptThis.Y - m_ptMouseDown.Y) + m_dTop;
                    

                    // get the orbtop and orbbottom so we know the max we can 
                    // allow the user to drag up or down.
                    double dOrbTop = Canvas.GetTop(OrbBorder);
                    double dOrbBottom = dOrbTop + OrbBorder.ActualHeight;

                    if (dDiffY  > dOrbTop)
                    {
                        // no deal - dragged too far, so constrain.
                        dDiffY = dOrbTop;
                    }
                    else if (dDiffY + SlideOutOuterBorder.ActualHeight < dOrbBottom)
                    {
                        // no deal - dragged too far, so constrain.
                        System.Diagnostics.Debug.WriteLine("constraining to bottom, ddiffy = " + dDiffY.ToString() + " ptThis.Y = " + ptThis.Y.ToString());
                        System.Diagnostics.Debug.WriteLine("constraining to bottom, SlideOutOuterBorder = " + SlideOutOuterBorder.ActualHeight.ToString() + " dOrbBottom = " + dOrbBottom.ToString());
                        dDiffY = dOrbBottom - SlideOutOuterBorder.ActualHeight;
                    }

                    // ok, lets adjust our top!
                    Canvas.SetTop(SlideOutOuterBorder, dDiffY);
                }
                else
                {
                    //
                    // ok, we're resizing.
                    //
                    Point ptThis = e.GetPosition(this as UIElement);
                    if ((m_rsResizing & ResizeSide.Right) == ResizeSide.Right)
                    {
                        // we're adjusting the WIDTH.
                        if (m_dMouseDownWidth + (ptThis.X - m_ptMouseDown.X) > OrbBorder.ActualWidth)
                        {
                            SlideOutOuterBorder.Width = m_dMouseDownWidth + (ptThis.X - m_ptMouseDown.X);
                            m_dWidth = SlideOutOuterBorder.Width;
                        }
                    }

                    if ((m_rsResizing & ResizeSide.Bottom) == ResizeSide.Bottom)
                    {
                        // we're adjusting the HEIGHT
                        if (m_dMouseDownHeight + (ptThis.Y - m_ptMouseDown.Y) > OrbBorder.ActualHeight)
                        {
                            SlideOutOuterBorder.Height = m_dMouseDownHeight + (ptThis.Y - m_ptMouseDown.Y);
                            
                            m_dHeight = SlideOutOuterBorder.Height;
                        }
                    }
                }
                return;
            }
            //
            // ok, we need to track the mouse move, so that we can determine
            // whether or not the user is dragging the control or resizing the
            // orb.
            //
            UpdateMouseCursor(e.GetPosition(SlideOutBorder));
        }

        private ResizeSide GetMouseDownResizeSide(Point in_ptMousePos)
        {
            ResizeSide r = ResizeSide.None;
            if (in_ptMousePos.X > SlideOutBorder.ActualWidth - 8)
                r |= ResizeSide.Right;
            if (in_ptMousePos.Y > SlideOutBorder.ActualHeight - 8)
                r |= ResizeSide.Bottom;

            return r;
        }

        private bool UpdateMouseCursor(Point in_ptMousePos)
        {
            ResizeSide rs = GetMouseDownResizeSide(in_ptMousePos);
            if ((rs & ResizeSide.Right) == ResizeSide.Right)
            {
                SlideOutOuterBorder.Cursor = Cursors.SizeWE;
            }
            else if ((rs & ResizeSide.Bottom) == ResizeSide.Bottom)
            {
                SlideOutOuterBorder.Cursor = Cursors.SizeNS;
            }
            else 
            {
                SlideOutOuterBorder.Cursor = Cursors.Arrow;
            }

            return true;
        }

        private void OrbControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            // reset the drag tracker
            m_bDragged = false;

            // remember the mousedown location
            m_ptMouseDown = e.GetPosition(this as UIElement); 
      
            // remember that our mnouse IS down.
            m_bMouseDown = true;     

            // remember which side of the editor we're dragging, if any.
            m_rsResizing = GetMouseDownResizeSide(e.GetPosition(SlideOutBorder));

            // remember our width and height at the start of the drag operation.
            m_dMouseDownHeight = SlideOutOuterBorder.ActualHeight;
            m_dMouseDownWidth = SlideOutOuterBorder.ActualWidth;

            // finally, make sure we don't lose the mouse!
            Focus();
            CaptureMouse();
        }

        private void OrbControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (m_bDragged)
            {
                // ok, if we've dragged the editor, then we should keep track
                // of our new top, so that we can re-expand out to the same place
                // later.
                m_dTop = Canvas.GetTop(SlideOutOuterBorder);
                m_dKeepOnscreenAdjustY = 0;
            }

            if (SlideOutEditor != null && !m_bDragged)
            {
                // we're going to need the position of the orb, so that we
                // can tell if the user has clicked inside the orb (this matters - 
                // if the user clicks within an orb, we should collapse).
                Point ptOrb = e.GetPosition(OrbBorder);

                if (m_dHeight == 0)
                {
                    SlideOutEditor.Control.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    m_dHeight = SlideOutEditor.Control.DesiredSize.Height;
                }

                //
                // ok, here's the deal.  we collapse IF:
                // the editor is currently visible,
                // the height of the editor is greater than OrbBorder.ActualHeight,
                // and we're either clicking in the editor title area OR we're clicking in the orb's region.
                //
                if (IsEditorVisible && SlideOutOuterBorder.Height > OrbBorder.ActualHeight && (e.GetPosition(SlideOutEditor.Control).Y < 5 ||
                    (ptOrb.X > 0 && ptOrb.X <= OrbBorder.ActualHeight && ptOrb.Y > 0 && ptOrb.Y <= OrbBorder.ActualHeight)))
                {
                    IsEditorVisible = false;
                    AnimateToPosition(OrbBorder.ActualHeight, m_dWidth, 0);
                }
                //
                // ok if the editor is NOT visible, and our desired height is greater than 20 (the size
                // of a normal slide-out), then we should expand!
                //
                else if (!IsEditorVisible && m_dHeight >= OrbBorder.ActualHeight)
                {
                    double dThrowAway = 0;
                    // get our keep-on-screen adjustment.
                    KeepOnScreen(SlideOutEditor.Control, m_dTop, 0, m_dHeight + OrbBorder.ActualHeight, m_dWidth, ref dThrowAway, ref m_dKeepOnscreenAdjustY);
                    // animate ourself to the right place.
                    AnimateToPosition(m_dHeight + OrbBorder.ActualHeight, m_dWidth, m_dTop + m_dKeepOnscreenAdjustY);
                    IsEditorVisible = true;
                }
            }

            // we're done.  reset the variables that we no longer need.
            m_bDragged = false;
            m_bMouseDown = false;
            
            ReleaseMouseCapture();
            e.Handled = true;
        }

        private bool KeepOnScreen(UIElement in_oElementToCheck, double in_dTop, double in_dLeft, double in_dDesiredHeight, double in_dDesiredWidth, ref double out_nFixOffsetX, ref double out_nFixOffsetY)
        {
            // get the timeline element.  we need this to tell exactly where our bounds are.
            UIElement oTimeline = ((Application.Current.RootVisual as FrameworkElement).FindName("Timeline") as UIElement);
            GeneralTransform aobjGeneralTransform = in_oElementToCheck.TransformToVisual(oTimeline);
            Point pt = aobjGeneralTransform.Transform(new Point(0, 0));

            out_nFixOffsetX = 0;
            out_nFixOffsetY = 0;
            if (pt.X < 0)
            {
                out_nFixOffsetX = pt.X ;
            }
            if (pt.Y < 0)
            {
                out_nFixOffsetY = pt.Y ;
            }
            
            if (pt.X + in_dDesiredWidth + in_dLeft > oTimeline.DesiredSize.Width)
            {
                out_nFixOffsetX = -1.0 * (pt.X + in_dDesiredWidth + in_dLeft - oTimeline.DesiredSize.Width);
            }
            if (pt.Y + in_dDesiredHeight > oTimeline.DesiredSize.Height)
            {
                out_nFixOffsetY = -1.0 * (pt.Y + in_dDesiredHeight - oTimeline.DesiredSize.Height);
            }

            return true;
        }


        private Storyboard m_sbPositionAnim = null;
        private void AnimateToPosition(double in_dNewHeight, double in_dNewWidth, double in_dTop)
        {
            if (m_sbPositionAnim != null)
            {
                m_sbPositionAnim.SkipToFill();
                m_sbPositionAnim = null;
            }

            //
            // create the position animation.
            //
            m_sbPositionAnim = new Storyboard();

            // our Height animation
            DoubleAnimation ptY = new DoubleAnimation();
            ptY.Duration = TimeSpan.FromSeconds(0.10);
            ptY.From = SlideOutOuterBorder.ActualHeight;
            ptY.To = in_dNewHeight;
            Storyboard.SetTarget(ptY, SlideOutOuterBorder);
            PropertyPath p = new PropertyPath("Height");
            Storyboard.SetTargetProperty(ptY, p);
            m_sbPositionAnim.Children.Add(ptY);

            // our width animation
            DoubleAnimation ptX = new DoubleAnimation();
            ptX.Duration = TimeSpan.FromSeconds(0.10);
            ptX.From = SlideOutOuterBorder.ActualWidth;
            ptX.To = in_dNewWidth;
            Storyboard.SetTarget(ptX, SlideOutOuterBorder);
            PropertyPath pX = new PropertyPath("Width");
            Storyboard.SetTargetProperty(ptX, pX);
            m_sbPositionAnim.Children.Add(ptX);

            // our top animation
            DoubleAnimation ptTop = new DoubleAnimation();
            ptTop.Duration = TimeSpan.FromSeconds(0.10);
            ptTop.From = Canvas.GetTop(SlideOutOuterBorder);
            ptTop.To = in_dTop;
            Storyboard.SetTarget(ptTop, SlideOutOuterBorder);
            PropertyPath pTop = new PropertyPath("(Canvas.Top)");
            Storyboard.SetTargetProperty(ptTop, pTop);
            m_sbPositionAnim.Children.Add(ptTop);

            m_sbPositionAnim.Begin();

        }

        public Control Control
        {
            get
            {
                return this as Control;
            }
        }
	}
}