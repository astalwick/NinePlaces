using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Common.Interfaces;

namespace NinePlaces
{
    public enum StackStyle
    {
        Stack,
        Jumble
    }
    public interface IStack
    {
        bool AddToStack(IDockable in_oDockableElement2);
        void MergeStacks(IStack in_oStackToMerge);
        bool RemoveFromStack(IDockable in_oDockableElement);

        List<IDockable> StackedChildren { get; }

        bool IsOpened { get; }
        event EventHandler ExpandedStateChanged;

        StackStyle Style { get; set; }
    }

    public partial class Stacker : DockableControlBase, IDockable, IStack
    {
        // delay timers for opening/closing the stack.
        private DispatcherTimer m_tCloseTimer = new DispatcherTimer();
        private DispatcherTimer m_tOpenTimer = new DispatcherTimer();

        // Animation storyboard for open/close of stack.
        private Storyboard m_sbSelectedAnimation = null;
        
        private double m_dRandomJumbleAmount1 = 0.0;
        private double m_dRandomJumbleAmount2 = 0.0;

        private StackStyle m_sStyle = StackStyle.Stack;
        public StackStyle Style
        {
            get
            {
                return m_sStyle;
            }
            set
            {
                if (m_sStyle != value)
                    m_sStyle = value;
            }
        }
        private List<IDockable> m_arStacked = new List<IDockable>();
        public List<IDockable> StackedChildren
        {
            get
            {
                return m_arStacked;
            }
        }

        private bool m_bIsExpanded = false;
        public bool IsOpened
        {
            get
            {
                return m_bIsExpanded;
            }
            protected set
            {
                if (m_bIsExpanded != value)
                {
                    m_bIsExpanded = value;
                    if (ExpandedStateChanged != null)
                        ExpandedStateChanged.Invoke(this, new EventArgs());
                }
            }
        }

        private DateTime m_dtDockTime = DateTime.MinValue;
        public override DateTime DockTime
        {
            get
            {
                return m_dtDockTime;
            }
            set
            {
                m_dtDockTime = value;

                double dDest = App.Timeline.TimeToPosition(m_dtDockTime);
                Canvas.SetLeft(this, dDest);

                OpenCloseStack(false, false);
            }
        }

        public override IconClass IconClass
        {
            get
            {
                return IconClass.IconStack;
            }
        }

        public new Control Control
        {
            get { return this as Control; }
        }

        public event EventHandler ExpandedStateChanged;
        
        /// <summary>
        /// Default constructor.  A stack with no children.
        /// </summary>
        public Stacker()
        {
            InitializeComponent();

            App.Timeline.DurationChanged += new EventHandler(Timeline_DurationChanged);
            App.Timeline.StartTimeChanged += new EventHandler(Timeline_StartTimeChanged);
            App.Timeline.TimelineSizeChanged += new SizeChangedEventHandler(Timeline_TimelineSizeChanged);

            m_tCloseTimer.Interval = new TimeSpan(0, 0, 0, 0, 300); 
            m_tCloseTimer.Tick += new EventHandler(m_tCloseTimer_Tick);

            m_tOpenTimer.Interval = new TimeSpan(0, 0, 0, 0, 75);
            m_tOpenTimer.Tick += new EventHandler(m_tStartTimer_Tick);

            Random r = new Random();
            m_dRandomJumbleAmount1 = 0.0;// r.NextDouble() * 10.0 - 5.0;
            m_dRandomJumbleAmount2 = 0.0;// r.NextDouble() * 10.0 - 5.0;

            DraggingEnabled = false;

            StackCanvas.MouseEnter += new MouseEventHandler(Control_MouseEnter);
            StackCanvas.MouseLeave += new MouseEventHandler(Control_MouseLeave);
            ClickAbsorbCanvas.MouseLeftButtonDown += new MouseButtonEventHandler(ClickAbsorbCanvas_MouseLeftButtonDown);
        }



        void ClickAbsorbCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenCloseStack(false, true);
        }

        /// <summary>
        /// Constructor for creating a new, populated stack.
        /// </summary>
        /// <param name="in_oChild"></param>
        /// <param name="in_oChild2"></param>
        public Stacker(IDockable in_oChild, IDockable in_oChild2)
            : this()
        {
            AddToStack(in_oChild);
            AddToStack(in_oChild2);
        }
        
        /// <summary>
        /// Merge the contents of another stack into this stack.
        /// </summary>
        /// <param name="in_oStackToMerge"></param>
        public void MergeStacks(IStack in_oStackToMerge)
        {
            List<IDockable> arL = new List<IDockable>(in_oStackToMerge.StackedChildren);
            foreach (IDockable iChild in arL)
            {
                in_oStackToMerge.RemoveFromStack(iChild);
                AddToStack(iChild);
            }
        }

        /// <summary>
        /// Add a new child to this stack.  
        /// Doing this gives control of the x/y pos of the child to the stack.
        /// </summary>
        /// <param name="in_oChild"></param>
        /// <returns></returns>
        public bool AddToStack(IDockable in_oChild)
        {
            if (in_oChild.Stacked)
                return false;

            in_oChild.Stacked = true;
            in_oChild.Stack = this;

            in_oChild.Control.MouseEnter += new MouseEventHandler(Control_MouseEnter);
            in_oChild.Control.MouseLeave += new MouseEventHandler(Control_MouseLeave);
            
            m_arStacked.Add(in_oChild);

            m_arStacked.Sort(new DockTimeComparer());

            UpdateDisplay();
            return true;
        }

        /// <summary>
        /// Remove a child from the stack.
        /// </summary>
        /// <param name="in_oChild"></param>
        /// <returns></returns>
        public bool RemoveFromStack(IDockable in_oChild)
        {
            if (in_oChild.Control.RenderTransform != null && in_oChild.Control.RenderTransform is RotateTransform)
                (in_oChild.Control.RenderTransform as RotateTransform).Angle = 0;

            
            in_oChild.Stacked = false;
            in_oChild.Stack = null;
            in_oChild.Control.MouseEnter -= new MouseEventHandler(Control_MouseEnter);
            in_oChild.Control.MouseLeave -= new MouseEventHandler(Control_MouseLeave);
            m_arStacked.Remove(in_oChild);
            m_arStacked.Sort(new DockTimeComparer());
            in_oChild.Control.Visibility = Visibility.Visible;
            Canvas.SetTop(in_oChild.Control, Canvas.GetTop(this));
            in_oChild.StackOrder = 0;

            if (m_arStacked.Count == 0)
                Visibility = Visibility.Collapsed;
            
            UpdateDisplay();
            
            return true;
        }

        private void m_tCloseTimer_Tick(object sender, EventArgs e)
        {
            m_tCloseTimer.Stop();
            OpenCloseStack(false, true);
        }

        private void m_tStartTimer_Tick(object sender, EventArgs e)
        {
            m_tOpenTimer.Stop();
            OpenCloseStack(true, true);
        }

        private void Timeline_StartTimeChanged(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void Timeline_DurationChanged(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void Timeline_TimelineSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDisplay();
        }

        private void Control_MouseLeave(object sender, MouseEventArgs e)
        {
            m_tOpenTimer.Stop();
            if (IsOpened && !IsChildSelected())
            {
                m_tCloseTimer.Start();
            }
        }

        private void Control_MouseEnter(object sender, MouseEventArgs e)
        {
            m_tCloseTimer.Stop();
            if (!IsOpened)
            {
                m_tOpenTimer.Start();
            }
        }
        private void m_sbSelectedAnimation_Completed(object sender, EventArgs e)
        {
            m_sbSelectedAnimation = null;
            UpdateClickAbsorbCanvas();
        }
        
        /// <summary>
        /// This animates a control (basically a stack child) from a point, to another point.
        /// </summary>
        /// <param name="in_oToAnimate"></param>
        /// <param name="in_ptCurrent"></param>
        /// <param name="in_ptDest"></param>
        private void AddControlAnimation(Control in_oToAnimate, Point in_ptCurrent, Point in_ptDest)
        {
            if (m_sbSelectedAnimation == null)
                m_sbSelectedAnimation = new Storyboard();

            TimeSpan tsDuration = TimeSpan.FromSeconds(0.10);
            m_sbSelectedAnimation.Children.Add(DoubleAnim(in_oToAnimate, "(Canvas.Top)", in_ptCurrent.Y, in_ptDest.Y, tsDuration));
            m_sbSelectedAnimation.Children.Add(DoubleAnim(in_oToAnimate, "(Canvas.Left)", in_ptCurrent.X, in_ptDest.X, tsDuration));
        }

        /// <summary>
        /// This animates the background rect.
        /// </summary>
        /// <param name="in_rcCurrent"></param>
        /// <param name="in_rcDest"></param>
        /// <param name="in_dCurOpacity"></param>
        /// <param name="in_dDestOpacity"></param>
        private void AddBackgroundAnimation(Rect in_rcCurrent, Rect in_rcDest, double in_dCurOpacity, double in_dDestOpacity)
        {
            if (m_sbSelectedAnimation == null)
                m_sbSelectedAnimation = new Storyboard();

            TimeSpan tsDuration = TimeSpan.FromSeconds( 0.10 );
            m_sbSelectedAnimation.Children.Add(DoubleAnim(StackCanvas, "(Canvas.Top)", in_rcCurrent.Top, in_rcDest.Top, tsDuration));
            m_sbSelectedAnimation.Children.Add(DoubleAnim(StackCanvas, "(Canvas.Left)", in_rcCurrent.Left, in_rcDest.Left, tsDuration));
            m_sbSelectedAnimation.Children.Add(DoubleAnim(StackCanvas, "Width", in_rcCurrent.Width, in_rcDest.Width, tsDuration));
            m_sbSelectedAnimation.Children.Add(DoubleAnim(StackCanvas, "Height", in_rcCurrent.Height, in_rcDest.Height, tsDuration));
            m_sbSelectedAnimation.Children.Add(DoubleAnim(StackCanvas, "Opacity", in_dCurOpacity, in_dDestOpacity, tsDuration));
        }


        private DoubleAnimation DoubleAnim(DependencyObject in_oTargetObject, string in_strPropertyPath, double in_dFrom, double in_dTo, TimeSpan in_tsDuration)
        {
            DoubleAnimation oAnimation = new DoubleAnimation();
            oAnimation.Duration = in_tsDuration; 
            oAnimation.From = in_dFrom;
            oAnimation.To = in_dTo;
            Storyboard.SetTarget(oAnimation, in_oTargetObject);
            Storyboard.SetTargetProperty(oAnimation, new PropertyPath(in_strPropertyPath));
            return oAnimation;
        }

        
        private void OpenCloseStack(bool in_bOpen, bool in_bAnimate)
        {
            if (m_arStacked.Count == 0)
                return;

            if (m_sbSelectedAnimation != null)
            {
                m_sbSelectedAnimation.SkipToFill();
                m_sbSelectedAnimation = new Storyboard();
            }

            double dLargestWidth = m_arStacked.Max(w => w.IconWidth);
            double dLargestHeight = m_arStacked.Max(w => w.IconHeight);

            if (!in_bOpen)
            {
                IsOpened = false;

                int offset = -6;
                int nCount = 0;

                int nTotal = m_arStacked.Count > 3 ? 3 : m_arStacked.Count;

                for (int i = 0; i < m_arStacked.Count; i++)
                {
                    IDockable child = m_arStacked[i];
                    if (m_arStacked.Count - i <= 3)
                    {
                        child.Control.Visibility = Visibility.Visible;

                        double dWidthOffset = (dLargestWidth - child.IconWidth) / 2.0;
                        double dHeightOffset = (dLargestHeight - child.IconHeight) / 2.0;

                        Point ptCur = new Point(Canvas.GetLeft(child.Control), Canvas.GetTop(child.Control));
                        Point ptDest = new Point(Canvas.GetLeft(this) + dWidthOffset, Canvas.GetTop(this) + dHeightOffset);
                        child.StackOrder = nCount + 1;

                        if (Style == StackStyle.Stack)
                        {
                            // we're stacking, so we need to offset the elements of the stack by a small amount.
                            double dOffset = ((nTotal - 1 - nCount) * offset);
                            ptDest = new Point(Canvas.GetLeft(this) + dWidthOffset + dOffset, Canvas.GetTop(this) + dHeightOffset+ dOffset);
                        }
                        else if (Style == StackStyle.Jumble)
                        {
                            // we're jumbling, so we need to rotate.
                            
                            // we use a precalculated random amount to make the jumble
                            // look a little bit less canned.
                            double dAngle = 0;
                            if (nCount == 0)
                                dAngle = -15 + m_dRandomJumbleAmount1;
                            else if (nCount == 1 && nTotal >= 3)
                                dAngle = 15 + m_dRandomJumbleAmount2;

#warning Replacing the rendertransform could be dangerous.  What if someone else has applied a transform?
                            RotateTransform rt = null;
                            if( child.Control.RenderTransform == null || !(child.Control.RenderTransform is RotateTransform ) )
                                child.Control.RenderTransform = new RotateTransform();
                            rt = child.Control.RenderTransform as RotateTransform;

                            // note that we're not animating.  
                            // no particular reason, except that it doesn't seem necessary.
                            rt.Angle = dAngle;
                            // rotate around the center of the icon.
                            rt.CenterX = child.IconWidth / 2.0;
                            rt.CenterY = child.IconHeight / 2.0;
                        }

                        if (in_bAnimate)
                            AddControlAnimation(child.Control, ptCur, ptDest);
                        else
                        {
                            Canvas.SetLeft(child.Control, ptDest.X);
                            Canvas.SetTop(child.Control, ptDest.Y);
                        }
                        nCount++;
                    }
                    else
                    {
                        child.Control.Visibility = Visibility.Collapsed;
                    }

                }

                Rect rcDest = new Rect(0, 0, dLargestWidth + 16, dLargestHeight + 16);
                if (in_bAnimate)
                {
                    AddBackgroundAnimation(new Rect(Canvas.GetLeft(StackCanvas),
                        Canvas.GetTop(StackCanvas),
                        StackCanvas.ActualWidth,
                        StackCanvas.ActualHeight),
                        rcDest, 0.5, 0.0);
                }
                else
                {
                    StackCanvas.Opacity = 0.0;
                    Canvas.SetTop(StackCanvas, rcDest.Y);
                    Canvas.SetLeft(StackCanvas, rcDest.X);
                    StackCanvas.Width = rcDest.Width;
                    StackCanvas.Height = rcDest.Height;
                }
            }
            else
            {
                IsOpened = true;

                // these offsets represent the 
                double dCellHeight = (dLargestHeight + 16);
                double dCellWidth = (dLargestWidth + 16);

                // we're expanding out all of the icons.
                // first, how much vertical/horizontal space do we have?
                double dHorizontalSpace = App.Timeline.Control.ActualWidth;
                double dVerticalSpace = App.Timeline.Control.ActualHeight;

                // what's our location in timeline space.
                GeneralTransform aobjGeneralTransform = TransformToVisual(App.Timeline.Control as UIElement);
                Point pt = aobjGeneralTransform.Transform(new Point(0,0));

                // figure out how many icons we can display above and below the selected icon
                int nMaxAbove = (int)Math.Floor(pt.Y / dCellHeight);
                int nMaxBelow = (int)Math.Floor((dVerticalSpace - pt.Y - dCellHeight) / dCellHeight);

                int nMaxPerColumn = nMaxAbove + nMaxBelow + 1;

                // how many columns, how many icons per column?
                int nMinimumColumns = (int)Math.Ceiling((double)m_arStacked.Count / (double)nMaxPerColumn);

                // now, we've decided to use nMinimumColumns. 
                // we should evenly distribute our icons across those columns, if we can!
                int nIconsPerColumn = (int)Math.Ceiling((double)m_arStacked.Count / (double)nMinimumColumns);

                // take the number of icons per column.
                // subtract 1 (for the dockline icon).
                // divide by 2.
                // that's how many icons should ideally be drawn ABOVE the selected icon.
                int nIconsAbove = (int)Math.Ceiling((double)((nIconsPerColumn - 1)/2.0)) > nMaxAbove ? nMaxAbove : (int)Math.Ceiling((double)((nIconsPerColumn - 1)/2.0));

                // however... if nIconsPerColumn - nIconsAbove > nMaxBelow, we have to adjust.
                while (nIconsPerColumn - (nIconsAbove + 1) > nMaxBelow)
                {
                    nIconsAbove++;
                }

                // ok, so now, figure out where we're actually going to start laying out the icons.
                // we populate from the upper left corner of our 'fake grid'.
                double dYStartPos = 0.0;
                double dXStartPos = 0.0;
                dYStartPos = Canvas.GetTop(this) - dCellHeight * nIconsAbove;
                dXStartPos = Canvas.GetLeft(this) - dCellWidth * (nMinimumColumns-1);

                int nCount = 0;
                int nThisColumnCount = 0;
                double dYPos = dYStartPos;
                double dXPos = dXStartPos;
                int nActualIconsPerColumn = 0;
                foreach (IDockable child in m_arStacked)
                {
                    // if we had a rotation due to jumble, undo it now.
                    child.Control.RenderTransform = new RotateTransform();
                    (child.Control.RenderTransform as RotateTransform).Angle = 0;

                    // better be visible, and have a zindex relative to the icon's order in the array
                    child.Control.Visibility = Visibility.Visible;
                    child.StackOrder = nCount + 10;

                    // differently sized icons should be CENTERED in their 'grid' cell.
                    // lets calculate an offset to accomplish that.
                    double dWidthOffset = (dLargestWidth - child.IconWidth) / 2.0;
                    double dHeightOffset = (dLargestHeight - child.IconHeight) / 2.0;

                    Point ptCur = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));    
                    Point ptDest = new Point(dXPos + dWidthOffset, dYPos + dHeightOffset);

                    if (in_bAnimate)
                        AddControlAnimation(child.Control, ptCur, ptDest);
                    else
                    {
                        Canvas.SetLeft(child.Control, ptDest.X);
                        Canvas.SetTop(child.Control, ptDest.Y);
                    }

                    nCount++;
                    nThisColumnCount++;
                    if (nActualIconsPerColumn < nThisColumnCount)
                        nActualIconsPerColumn = nThisColumnCount;

                    if (nThisColumnCount >= nIconsPerColumn)
                    {
                        dYPos = dYStartPos;
                        dXPos += dCellWidth;
                        nThisColumnCount = 0;
                    }
                    else
                    {
                        dYPos += dCellHeight;
                    }
                }

                Rect rcDest = new Rect(-1.0 * dCellWidth * (nMinimumColumns - 1) - 8, -dCellHeight * nIconsAbove - 8, (nMinimumColumns) * dCellWidth, nActualIconsPerColumn * dCellHeight);
                if (in_bAnimate)
                {
                    AddBackgroundAnimation(new Rect(Canvas.GetLeft(StackCanvas),
                        Canvas.GetTop(StackCanvas),
                        StackCanvas.ActualWidth,
                        StackCanvas.ActualHeight),
                        rcDest, 0.0, 0.5);
                }
                else
                {
                    StackCanvas.Opacity = 0.5;
                    Canvas.SetTop(StackCanvas, rcDest.Y);
                    Canvas.SetLeft(StackCanvas, rcDest.X);
                    StackCanvas.Width = rcDest.Width;
                    StackCanvas.Height = rcDest.Height;
                }
            }

            if (in_bAnimate)
            {
                m_sbSelectedAnimation.Begin();
                m_sbSelectedAnimation.Completed += new EventHandler(m_sbSelectedAnimation_Completed);
            }
            else
            {
                UpdateClickAbsorbCanvas();
            }
        }

        private void UpdateClickAbsorbCanvas()
        {
            if (IsOpened)
            {
                ClickAbsorbCanvas.IsHitTestVisible = true;
                ClickAbsorbCanvas.Visibility = Visibility.Visible;

                GeneralTransform objGeneralTransform = App.Timeline.Control.TransformToVisual(LayoutRoot as UIElement);
                Point ptXY = objGeneralTransform.Transform(new Point(0, 0));
                Canvas.SetLeft(ClickAbsorbCanvas, ptXY.X);
                Canvas.SetTop(ClickAbsorbCanvas, ptXY.Y);
                ClickAbsorbCanvas.Width = App.Timeline.Control.ActualWidth;
                ClickAbsorbCanvas.Height = App.Timeline.Control.ActualHeight;
            }
            else
            {
                ClickAbsorbCanvas.IsHitTestVisible = false;
                ClickAbsorbCanvas.Visibility = Visibility.Collapsed;
            }
        }
        private bool IsChildSelected()
        {
            foreach (IDockable i in m_arStacked)
            {
                if ((i as ISelectable).Selected)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateDisplay()
        {
            if (m_arStacked.Count == 0)
                return;
            //
            // ok, we need to evaluate the position of the stack.
            // we have new children, so we need to find the middle point.
            //
            double dPixel = 0.0;
            foreach (IDockable child in m_arStacked)
            {
                dPixel += App.Timeline.TimeToPosition(child.DockTime);
            }
            dPixel = dPixel / m_arStacked.Count;

            DockTime = App.Timeline.PositionToTime(dPixel);

            OpenCloseStack(false, false);
        }
    }
}
