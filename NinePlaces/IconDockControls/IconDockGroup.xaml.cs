using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using NinePlaces;
using System.Collections.Generic;
using Common.Interfaces;
using System.ComponentModel;

namespace NinePlaces.IconDockControls
{
    public partial class IconDockGroup 
	{
        private bool m_bInitialized = false;
		public IconDockGroup()
		{
			// Required to initialize variables
			InitializeComponent();
            LayoutUpdated += new EventHandler(IconDockGroup_LayoutUpdated);
		}

        void IconDockGroup_LayoutUpdated(object sender, EventArgs e)
        {
            if (!m_bInitialized)
            {
                // Ok, lets construct one of each of our different icon types!
                foreach (IconTypes t in Icons)
                {
                    IconContainer.Children.Add(CreateIcon(t));
                }
                m_bInitialized = true;
            }
        }

        // event by which we can notify out that a new icon has been created (and
        // needs to be watched for any drag or click events).
        public event IconDockEventHandler IconCreated;

        private List<IconTypes> m_arIconTypes = new List<IconTypes>();
        /// <summary>
        /// Icon types that make up this particular icon group.
        /// </summary>
        public List<IconTypes> Icons
        {
            get
            {
                return m_arIconTypes;
            }
            set
            {
                if (m_arIconTypes != value)
                    m_arIconTypes = value;
            }
        }

        private void Icon_DragStarted(object sender, EventArgs args)
        {
            IIcon ic = sender as IIcon;
            if (ic != null && IconContainer.Children.Contains(ic.Control))
            {
                VisualStateManager.GoToState(this, "GroupTitleVisible", true);

                // stop listening to this icon.  it is no longer our concern.
                (ic as DockableControlBase).MouseEnter -= new MouseEventHandler(icB_MouseEnter);
                (ic as DockableControlBase).MouseLeave -= new MouseEventHandler(icB_MouseLeave);
                (ic as DockableControlBase).DragStarted -= new EventHandler(Icon_DragStarted);

                //clear the thickness.  we only need it on the dock.
                (ic as DockableControlBase).Margin = new Thickness(0);

                // we're dragging a new icon.  lets clear the selection so that
                // we don't conflict anywhere.
                App.SelectionMgr.ClearSelection();

                // the icon that we're dragging needs to be removed and replaced
                // in the icon dock.
                int nIndex = IconContainer.Children.IndexOf(ic.Control);
                IconContainer.Children.Remove(ic.Control);
                DockableControlBase icb = CreateIcon(ic.IconType);
                // insert the replacement icon.
                IconContainer.Children.Insert(nIndex, icb);
            }
        }

        private DockableControlBase CreateIcon(IconTypes in_nIconType)
        {
            IconControlBase icB = IconRegistry.NewIcon(in_nIconType);
            
            icB.Margin = new Thickness(5, 0, 5, 0);
            icB.IsEnabled = true;
            icB.Hidden = false;
            icB.MouseEnter += new MouseEventHandler(icB_MouseEnter);
            icB.MouseLeave += new MouseEventHandler(icB_MouseLeave);
            icB.DragStarted += new EventHandler(Icon_DragStarted);
            VisualStateManager.GoToState(icB.Control, "VSVisible", true);
            if (IconCreated != null)
                IconCreated.Invoke(this, new IconDockEventArgs(icB));

            return icB;
        }

        void icB_MouseLeave(object sender, MouseEventArgs e)
        {
            if (App.Dragging)
                return;

            VisualStateManager.GoToState(this, "GroupTitleVisible", true);
        }
        
        void icB_MouseEnter(object sender, MouseEventArgs e)
        {
            if (App.Dragging)
                return;

            IIcon ic = sender as IIcon;

            GeneralTransform aobjGeneralTransform = ic.Control.TransformToVisual(IconTitleCanvas as UIElement);
            Point pt = aobjGeneralTransform.Transform(new Point(App.StaticIconWidthHeight / 2, 0));

            IconTitle.Text = App.Resource.GetString( ic.IconType.ToString() );
            double dX = pt.X - IconTitle.ActualWidth / 2;

            //
            // now, we need to worry about the right edge.  we must adjust the icon so that
            // it remains within the bounds of our icongroup!
            //
            if (dX + IconTitle.ActualWidth > LayoutRoot.ActualWidth)
            {
                // ok, we're too far right.

                dX -= (dX + IconTitle.ActualWidth - LayoutRoot.ActualWidth);
            }

            Canvas.SetLeft(IconTitle, dX);

            VisualStateManager.GoToState(this, "IconTitleVisible", true);
        }
	}
}