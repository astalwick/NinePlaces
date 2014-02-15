using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using NinePlaces.ViewModels;
using Common.Interfaces;
using System.Linq;

namespace NinePlaces
{
	public partial class DockLine : UserControl, ISelectable
	{
        /// <summary>
        /// Sort comparer. 
        /// </summary>
        private DockTimeComparer m_oElementComparer = new DockTimeComparer();

        /// <summary>
        /// All docked icons.  Does NOT include stacks.
        /// </summary>
        private List<IDockable> m_arDockedElements = new List<IDockable>();
        /// <summary>
        /// List of all STACKS.  
        /// </summary>
        private List<IDockable> m_arStacks = new List<IDockable>();

        /// <summary>
        /// Given the current zoom and width, how many pixels to represent an hour?
        /// </summary>
        private double m_dPixelsPerHour = 0.0;

        /// <summary>
        /// Is this dockline (or one of its icons) selected?
        /// </summary>
        private bool m_bSelected = false;

        
        /// <summary>
        /// Distance, in pixels, below which two icons will stack.
        /// This should be dynamic - should be automatically generated based on widths of elements.
        /// </summary>
        public double StackDistance
        {
            get
            {
                return 35.0;
            }
        }

        private DateTime m_dtStartTime = DateTime.MinValue;
        /// <summary>
        /// StartTime of the timeline.
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                return m_dtStartTime;
            }
            set
            {
                if (m_dtStartTime != value)
                {
                    m_dtStartTime = value;

                    // if start tiem has changed, we need to update the pixelsperhour and
                    // reposition the icons on the dockline to reflect the change.

                    PixelsPerHour = ((double)LayoutRoot.ActualWidth) / Duration.TotalHours;
                    PositionIcons();
                }
            }
        }

        private double PixelsPerHour
        {
            get
            {
                return m_dPixelsPerHour;
            }
            set
            {
                if (m_dPixelsPerHour != value)
                {
                    m_dPixelsPerHour = value;
                    
                    EvaluateStacking();
                }
            }
        }

        /// <summary>
        /// EndTime of the timeline.
        /// </summary>
        public DateTime EndTime
        {
            get
            {
                return StartTime + Duration;
            }
        }

        private TimeSpan m_tsDuration = TimeSpan.MinValue;
        /// <summary>
        /// Duration of the timeline.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return m_tsDuration;
            }
            set
            {
                if (m_tsDuration != value)
                {
                    m_tsDuration = value;
                }
            }
        }

		public DockLine()
		{
			// Required to initialize variables
			InitializeComponent();
            Loaded += new RoutedEventHandler(DockLine_Loaded);

            MouseLeftButtonUp += new MouseButtonEventHandler(DockLine_MouseLeftButtonUp);

            App.SelectionMgr.AddToSelectionGroup("DockLines", this);
		}


        /// <summary>
        /// Positions all icons on the dockline to their
        /// correct location on the timeline.
        /// </summary>
        private void PositionIcons()
        {
            foreach (IDockable i in DockedIcons.Children)
            {
                PositionIcon(i, false);
            }
        }

        /// <summary>
        /// Positions a given icon to its spot on the timeline.
        /// </summary>
        /// <param name="i"></param>
        public void PositionIcon(IDockable i, bool in_bAnimated)
        {
            if (i.Stacked)      // stack has control of positioning.
                return;

            if (i.DockTime > StartTime.AddDays(-1) && i.DockTime < EndTime.AddDays(1))
            {
                if (i.Control.Visibility == Visibility.Collapsed)
                    i.Control.Visibility = Visibility.Visible;

                i.MoveToTime(i.DockTime, in_bAnimated);
            }
            else
            {
                // we're out of the viewport,
                // so no need to worry about the icon anymore.  collapse it,
                i.Control.Visibility = Visibility.Collapsed;
            }

        }


        /// <summary>
        /// Converts a datetime to a X pixel coordinate,
        /// relative to the dockline.
        /// </summary>
        /// <param name="in_dtToCheck"></param>
        /// <returns></returns>
        public double TimeToPosition(DateTime in_dtToCheck)
        {
            //
            // now, figure out exactly where the child is supposed to be relative
            // to the start time of the timeline
            //

            TimeSpan ts = in_dtToCheck - StartTime;
            return ts.TotalHours * PixelsPerHour;
        }
        
        void DockLine_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // this is sort of a hack.
            // if someone selects an icon, the icon will let its mouseleftbuttonup event
            // fall through.  the underlying interface element will receive the click.
            // for a docked icon, that is the dockline.
            //
            // by paying attention to mouseleftbuttonup, we can set selected = true,
            // which will cause our z-order to get bumped, relative to other docklines.
            //
            // this prevents bug 253.
            Selected = true;
        }

        void DockLine_Loaded(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Normal", false);
        }

        public ITimeline ParentTimeline 
        {
            set
            {
                value.StartTimeChanged += new EventHandler(TimelineStartTimeChanged);
                value.DurationChanged += new EventHandler(TimelineDurationChanged);
                value.TimelineSizeChanged += new SizeChangedEventHandler(TimelineSizeChanged);
            }
        }

        void TimelineSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // the timeline size has changed - that means our 'PixelsPerHour' count 
            // is off.  recalc.
            PixelsPerHour = ((double)LayoutRoot.ActualWidth) / Duration.TotalHours;
            PositionIcons();
        }

        void TimelineDurationChanged(object sender, EventArgs e)
        {
            // the duration of the timeline has changed.  
            // update both the duration and starttime.
            Duration = (sender as ITimeline).Duration;
            StartTime = (sender as ITimeline).StartTime;

            // the timeline duration has changed.  we should evaluate
            // whether or not it is necessary to form new stacks.
            EvaluateStacking();
        }

        void TimelineStartTimeChanged(object sender, EventArgs e)
        {
            // the starttime has changed.
            StartTime = (sender as ITimeline).StartTime;
        }

        public SolidColorBrush Color
        {
            get { return (SolidColorBrush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Color.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(SolidColorBrush), typeof(DockLine), new PropertyMetadata(OnColorPropertyChanged));

        private static void OnColorPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            DockLine dl = sender as DockLine;
            dl.DisplayText.Foreground = args.NewValue as SolidColorBrush;
            dl.rectangle.Fill = args.NewValue as SolidColorBrush;
        }

        public IconClass Class
        {
            get { return (IconClass)GetValue(ClassProperty); }
            set { SetValue(ClassProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Class.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ClassProperty =
            DependencyProperty.Register("Class", typeof(IconClass), typeof(DockLine), new PropertyMetadata(IconClass.Activity));

        public double DockRegionMargin
        {
            get { return (double)GetValue(DockRegionMarginProperty); }
            set { SetValue(DockRegionMarginProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DockRegionMargin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DockRegionMarginProperty =
            DependencyProperty.Register("DockRegionMargin", typeof(double), typeof(DockLine), new PropertyMetadata(0.0));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(DockLine), new PropertyMetadata(OnTextPropertyChanged));

        private static void OnTextPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            DockLine dl = sender as DockLine;
            dl.DisplayText.Text = args.NewValue as string;
        }

        public bool AcceptsDock(IDockable in_oToDock)
        {

            return ((int)in_oToDock.IconClass & (int)Class) == (int)Class;
        }

        public bool IsOverDockLine(IDockable in_oToDock)
        {
            if (in_oToDock is IDraggable && (in_oToDock as IDraggable).Dragging)
            {
                GeneralTransform objGeneralTransform = App.AppRootVisual.TransformToVisual(LayoutRoot);
                Point ptDockDrag = objGeneralTransform.Transform((in_oToDock as IDraggable).DragPosition());

                //
                // we use -16.0 as a bit of a buffer,
                // and we make sure that compare at the MIDDLE OF THE ICON, not the top (ptDockDrag is the position
                // of the top left of the icon, so we need to adjust)
                //
                if (ptDockDrag.Y + App.IconWidthHeight / 2.0 >= -1.0 * DockRegionMargin && ptDockDrag.Y + App.IconWidthHeight / 2.0 < LayoutRoot.ActualHeight + DockRegionMargin)
                {
                    return true;
                }
            }
            return false;
        }


        public bool Dock(IDockable in_oToDock)
        {
            if (!AcceptsDock(in_oToDock))
                return false;

            if ((in_oToDock as DockableControlBase).DockLine != this)
            {
                (in_oToDock as DockableControlBase).DockLine = this;
                m_arDockedElements.Add(in_oToDock);

                in_oToDock.DockTimeChanged += new EventHandler(in_oToDock_DockTimeChanged);

                if (!(in_oToDock is IDraggable) || !((in_oToDock as IDraggable).Dragging) )
                    PositionIcon(in_oToDock, false);
            }
            

            if( !App.Dragging )
                EvaluateStacking();

            return true;
        }

        void in_oToDock_DockTimeChanged(object sender, EventArgs e)
        {
            IDockable dToUnstack = sender as IDockable ;
            if (dToUnstack.Stacked)
            {
                Unstack(dToUnstack, dToUnstack.Stack);
            }
        }

        private void EvaluateStacking()
        {
            IDockable dToStackWith = null;

            // sort all of the docked elements by time.  this allows us
            // to do a series of (much, much faster) binary searches.
            m_arDockedElements.Sort(m_oElementComparer);
            int nCount = 0;

            foreach (IDockable d in m_arDockedElements)
            {
                if (!d.Stacked && ShouldStack(d, out dToStackWith))
                {
                    //
                    // ok, we need to add this guy to a stack.
                    //
                    Stack(d, dToStackWith);
                }
                else if (d.Stacked && ShouldUnstack(d))
                {
                    //
                    // we need to unstack this icon.
                    //
                    Unstack(d, d.Stack);
                }

                if (!d.Stacked)
                {
                    d.StackOrder = nCount;
                    nCount++;
                }
            }
            
            // now, determine whether there are STACKS that need merging together!
            if (m_arStacks.Count > 1)
            {
                List<IDockable> arMergedToRemove = new List<IDockable>();
                IDockable dStackToMerge = null;

                // sort the stacks by time.  allows us to binarysearch.
                m_arStacks.Sort(m_oElementComparer);
                for( int i = m_arStacks.Count - 1; i >= 0; i-- )
                {
                    IDockable s = m_arStacks[i];
                    if (ShouldStack(s, out dStackToMerge, m_arStacks))
                    {
                        // ooh, good times, merging stacks.
                        // do the merge
                        (dStackToMerge as IStack).MergeStacks(s as IStack);
                        // remove the stack that got merged
                        m_arStacks.Remove(s);
                        DockedIcons.Children.Remove(s.Control);

                        // doublecheck that it has no children....
                        if ((s as IStack).StackedChildren.Count > 0)
                            throw new Exception("StackedChildren > 0");
                    }
                }
            }
        }

        private bool ShouldUnstack(IDockable in_oDockableElement)
        {
            if (!in_oDockableElement.Stacked)
                return false;

            double dMe = TimeToPosition(in_oDockableElement.DockTime);
            foreach (IDockable iChild in in_oDockableElement.Stack.StackedChildren)
            {
                double dChild = TimeToPosition(iChild.DockTime);
                if (Math.Abs(dMe - dChild) < StackDistance && iChild != in_oDockableElement)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ShouldStack(IDockable in_oDockableElement, out IDockable out_dToStackWith, List<IDockable> in_arArrayToCheck)
        {
            out_dToStackWith = null;
            // ok, in order to determine stackableness, we need to figure out who the nearest neighbour is.
            int nIndex = in_arArrayToCheck.BinarySearch(in_oDockableElement, m_oElementComparer);

            // now.. it's possible that this didn't actually *hit*.  maybe two of our list elements share the exact same docktime.  (weird, but not impossible).
            // this could probably be improved... we're quite likely double-checking a few elements...
            if (in_arArrayToCheck[nIndex] != in_oDockableElement)
            {
                // loop BACKWARDS until we find the element we're looking for.
                while (nIndex > 0 && in_arArrayToCheck[nIndex - 1].DockTime == in_oDockableElement.DockTime)
                {
                    nIndex--;
                    if (in_arArrayToCheck[nIndex] == in_oDockableElement)
                        break;
                }
            }

            if (in_arArrayToCheck[nIndex] != in_oDockableElement)
            {
                // we didn't find the element we need by going backwards in the array - try goind forwards!
                while (nIndex < in_arArrayToCheck.Count - 1 && in_arArrayToCheck[nIndex + 1].DockTime == in_oDockableElement.DockTime)
                {
                    nIndex++;
                    if (in_arArrayToCheck[nIndex] == in_oDockableElement)
                        break;
                }
            }

            if (in_arArrayToCheck[nIndex] != in_oDockableElement)
            {
                // ohhh noooewssss!
                throw new Exception("NO MATCH!");
            }

            // ok, we've got *our* index.
            // now, lets see how far away our neighbours are.
            DateTime dtLeft = DateTime.MinValue;
            DateTime dtRight = DateTime.MaxValue;
            double dMe = TimeToPosition(in_oDockableElement.DockTime);

            if (nIndex > 0)
            {
                double dLeft = TimeToPosition(in_arArrayToCheck[nIndex - 1].DockTime);
                if (Math.Abs(dMe - dLeft) < StackDistance)
                {
                    // oh joy, we get to stack!
                    out_dToStackWith = in_arArrayToCheck[nIndex - 1];
                    return true;
                }
            }

            if (nIndex < in_arArrayToCheck.Count - 1)
            {
                double dRight = TimeToPosition(in_arArrayToCheck[nIndex + 1].DockTime);
                if (Math.Abs(dRight - dMe) < StackDistance)
                {
                    // oh joy, we get to stack!
                    out_dToStackWith = in_arArrayToCheck[nIndex + 1];
                    return true;
                }
            }
            return false;
        }

        private bool ShouldStack(IDockable in_oDockableElement, out IDockable out_dToStackWith)
        {
            return ShouldStack(in_oDockableElement, out out_dToStackWith, m_arDockedElements);

        }

        private bool Unstack(IDockable in_oDockableElement, IStack in_oParentStack)
        {
            // first, obviously, remove the element from its stack.
            in_oParentStack.RemoveFromStack(in_oDockableElement);

            // if we're removing the *second* last element (only 1 element left),
            // then we should get rid of the stack altogether.
            if (in_oParentStack.StackedChildren.Count == 1)     // we only have one element in the stack!  might as well blow up the stack altogether
            {
                // unstack the last element.
                Unstack(in_oParentStack.StackedChildren[0], in_oParentStack);
            }
            else if( in_oParentStack.StackedChildren.Count == 0 )
            {
                // aha, our stack has NO more elements.
                // we should remove it.
                m_arStacks.Remove(in_oParentStack as IDockable);
                DockedIcons.Children.Remove((in_oParentStack as IDockable).Control);

                // double-check that it has no children.  (if we get here, uh, something's probably fucked with threading or other weirdness like that!?)
                if (in_oParentStack.StackedChildren.Count > 0)
                    throw new Exception("StackedChildren > 0 (unstack)");
            }

            PositionIcon(in_oDockableElement, false);
            return true;
        }

        private bool Stack(IDockable in_oDockableElement, IDockable in_oDockableElement2)
        {
            if (in_oDockableElement2.Stacked)
            {
                in_oDockableElement2.Stack.AddToStack(in_oDockableElement);
            }
            else
            {
                // neither is a stack - we need to construct a new stack, then,
                // for both dockableelements.
                Stacker oStack = new Stacker(in_oDockableElement, in_oDockableElement2);

                // for photos, tell the stack to behave a bit differently.
                if (((int)in_oDockableElement.IconClass & (int)IconClass.Photo) == (int)IconClass.Photo)
                    oStack.Style = StackStyle.Jumble;

                oStack.ExpandedStateChanged += new EventHandler(s_ExpandedStateChanged);
                Canvas.SetZIndex(oStack, 0);
                DockedIcons.Children.Add(oStack);
                m_arStacks.Add(oStack as IDockable);
            }

            return true;
        }

        void s_ExpandedStateChanged(object sender, EventArgs e)
        {
            if(( sender as IStack ).IsOpened )
            {
                // we're selecting this guy - therefore, 
                // bump up his zindex way above everythign else.
                int nZIndex = Canvas.GetZIndex(this as UIElement);
                Canvas.SetZIndex(this as UIElement, nZIndex + 2);
            }
            else
            {
                int nZIndex = Canvas.GetZIndex(this as UIElement);
                Canvas.SetZIndex(this as UIElement, nZIndex - 2);
            }
        }

        /// <summary>
        /// Undocks an icon
        /// </summary>
        public bool Undock(IDockable in_oToDock)
        {
            if (((int)in_oToDock.IconClass & (int)Class) == (int)Class)
            {

                if (in_oToDock.Stacked)
                {
                    Unstack(in_oToDock, in_oToDock.Stack);
                }

                (in_oToDock as DockableControlBase).DockLine = null;
                in_oToDock.DockTimeChanged -= new EventHandler(in_oToDock_DockTimeChanged);
                m_arDockedElements.Remove(in_oToDock);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Undocks an icon given only the ID.
        /// </summary>
        public bool Undock(int in_nUniqueID)
        {

            for (int i = DockedIcons.Children.Count - 1; i >= 0; i--)
            {
                if ((DockedIcons.Children[i] as DockableControlBase).VM.UniqueID == in_nUniqueID)
                {
                    IDockable dToUndock = DockedIcons.Children[i] as IDockable;
                    if (dToUndock.Stacked)
                    {
                        Unstack(dToUndock, dToUndock.Stack);
                    }

                    dToUndock.DockTimeChanged -= new EventHandler(in_oToDock_DockTimeChanged);
                    dToUndock.DockLine = null;
                    m_arDockedElements.Remove(dToUndock);
                    return true;
                }

            }

            return false;
        }

        /// <summary>
        /// Clears the dockline entirely.
        /// Undocks all children.
        /// </summary>
        public void Clear()
        {
            var Children = from a in DockedIcons.Children where a is IDockable select a as IDockable;

            List<IDockable> arToUndock = new List<IDockable>(Children);
            foreach (IDockable dToUndock in arToUndock)
            {
                if (dToUndock.Stacked)
                {
                    Unstack(dToUndock, dToUndock.Stack);
                }

                dToUndock.DockTimeChanged -= new EventHandler(in_oToDock_DockTimeChanged);
                m_arDockedElements.Remove(dToUndock);
                dToUndock.DockLine = null;
            }

            m_arStacks.Clear();
            DockedIcons.Children.Clear();
        }

        internal void DragStart(IDockable ic)
        {
            if (((int)ic.IconClass & (int)Class) == (int)Class)
            {
                VisualStateManager.GoToState(this, "Highlighted", true);
                VisualStateManager.GoToState(this, "Large", true);
                CaptureMouse();
            }
        }

        internal void DragEnd(IDockable ic)
        {
            if (((int)ic.IconClass & (int)Class) == (int)Class)
            {
                ReleaseMouseCapture();
                VisualStateManager.GoToState(this, "NotHighlighted", true);
                VisualStateManager.GoToState(this, "Normal", true);
            }

            EvaluateStacking();
        }

        #region ISelectable Members

        public event SelectionEventHandler SelectionStateChanged;
        public bool Selected
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

                    // this fixes bug 253
                    if (m_bSelected)
                    {
                        // we're selecting this guy - therefore, 
                        // bump up his zindex way above everythign else.
                        int nZIndex = Canvas.GetZIndex(this as UIElement);
                        Canvas.SetZIndex(this as UIElement, nZIndex + 1);
                    }
                    else
                    {
                        int nZIndex = Canvas.GetZIndex(this as UIElement);
                        Canvas.SetZIndex(this as UIElement, nZIndex - 1);
                    }

                    if (SelectionStateChanged != null)
                        SelectionStateChanged.Invoke(this as ISelectable, new SelectionEventArgs(m_bSelected));
                }
            }
        }

        #endregion
    }


    public class DockTimeComparer : IComparer<IDockable>
    {
        public int Compare(IDockable x, IDockable y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    return x.DockTime.CompareTo(y.DockTime);
                }
            }
        }
    }
}
