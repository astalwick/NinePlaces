using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BasicEditors;

namespace NinePlaces.Managers
{
    public interface IOrbMgr : ISelectable
    {
        List<IOrbControl> Orb {get;}
        int Count { get; }
        bool HideOnMouseLeave { get; set; }
        bool SelectOrbOnShow { get; set; }
        bool Shown { get; set; }
        IOrbControl CreateOrb();
        IOrbControl CreateOrb(Color in_OrbColor);
    }

    public class IconOrbMgr : IOrbMgr
    {
        private List<IOrbControl> m_arOrbs = new List<IOrbControl>();

        private DispatcherTimer m_timerHideOrbs = new DispatcherTimer();

        public List<IOrbControl> Orb
        {
            get
            {
                return m_arOrbs;
            }
        }   

        private Control m_oParentControl = null;
        private Canvas m_oPositioningCanvas = null;

        public int Count
        {
            get
            {
                return m_arOrbs.Count;
            }
        }

        public bool SelectOrbOnShow
        { 
            get; set; 
        }

        public bool HideOnMouseLeave
        {
            get;
            set;
        }

        public bool Shown
        {
            get
            {
                return Selected;
            }
            set
            {
                Selected = value;
            }
        }

        public IconOrbMgr(Control in_oParentControl, Canvas in_oPositioningCanvas)
        {
            SelectOrbOnShow = false;
            HideOnMouseLeave = true;

            m_oParentControl = in_oParentControl;
            m_oPositioningCanvas = in_oPositioningCanvas;

            m_timerHideOrbs.Tick += new EventHandler(m_timerHideOrbs_Tick);
            App.SelectionMgr.AddToSelectionGroup("IconOrbMgr", this as ISelectable);
        }

        private void m_timerHideOrbs_Tick(object sender, EventArgs e)
        {
            DeselectAllOrbs();
        }

        ~IconOrbMgr()
        {
            App.SelectionMgr.RemoveFromSelectionGroup("IconOrbMgr", this as ISelectable);
        }

        private void HideOrbs()
        {
            // focus the parent child!
            // this is done because if you DON'T focus the parent, and one of the textboxes has focus when
            // its state is set to visibilitycollapsed, then that textbox will pass it's focus to the next,
            // until the very last textbox has focus and is set to collapsed - that last textbox will never
            // again be focusable.  it will gobble up it's focus.  
            // microsoft bug.
            m_oParentControl.Focus();

            foreach (IOrbControl o in m_arOrbs)
            {
                o.IsSlideOutOpen = false;
                o.Control.Visibility = Visibility.Collapsed;
            }
        }

        private void PositionOrbs( bool in_bLeftOrbs )
        {
            int nCount = 0;
            for (int i = 0; i < Count; i++)
            {
                if (in_bLeftOrbs && Orb[i].PositionOrbOnLeft)
                    nCount++;
                else if (!in_bLeftOrbs && !Orb[i].PositionOrbOnLeft)
                    nCount++;
            }

            double dOrbHeight = 24;
            double dOrbSpace = 6;

            double dOrbsAbove = Math.Floor(((double)nCount) / 2);
            double dMod = Math.IEEERemainder(((double)nCount), 2);
            double dStart = -1 * (dOrbHeight + dOrbSpace) * dOrbsAbove;

            double dLeft = 0;
            double dLeftMult = 0;

            if (dMod == 0)      // even number.  offset the orbs a bit.
            {
                dStart += (dOrbHeight + dOrbSpace) / 2;
                dOrbsAbove -= .5;
            }

            int nOrb = 0;
            for (int i = 0; i < Count; i++)
            {
                if ( (Orb[i].PositionOrbOnLeft && !in_bLeftOrbs) || 
                    (!Orb[i].PositionOrbOnLeft && in_bLeftOrbs) )
                    continue;

                if (Orb[i].Control.Visibility == Visibility.Collapsed)
                {


                    Orb[i].Control.Visibility = Visibility.Visible;

                    if (!Orb[i].PositionOrbOnLeft)
                    {
                        dLeft = m_oPositioningCanvas.ActualWidth + 2 + (5 * nCount - 1);
                        dLeftMult = 1.0;
                    }
                    else
                    {
                        dLeft = (dOrbHeight + 2 + (5 * nCount - 1)) * -1;
                        dLeftMult = -1.0;
                    }

                    Canvas.SetLeft(Orb[i].Control, dLeft - Math.Abs(dOrbsAbove - nOrb) * 30.0 * (Math.Abs(dOrbsAbove - nOrb) / 5) * dLeftMult);
                    Canvas.SetTop(Orb[i].Control, (m_oPositioningCanvas.ActualHeight / 2) - (dOrbHeight / 2) + dStart);
                    Canvas.SetZIndex(Orb[i].Control, nCount - i);

                    dStart += dOrbHeight + dOrbSpace;
                    nOrb++;
                }
            }
        }

        private void ShowOrbs()
        {
            App.AssemblyLoadManager.Requires("Editors.xap");
            //
            // this is going ot use some trick bad math
            // to try to draw out orbs around the child.
            //
            PositionOrbs(true);
            PositionOrbs(false);

            if( SelectOrbOnShow )
                SelectOrb(Orb[0]);
        }

       private void OrbMouseEnter(object sender, MouseEventArgs e)
        {
            m_timerHideOrbs.Stop();
            SelectOrb(sender as IOrbControl);
        }

        private void DeselectAllOrbs()
        {
            foreach (IOrbControl o in m_arOrbs)
            {
                o.IsSlideOutOpen = false;
                Canvas.SetZIndex(o.Control, 0);
            }
        }

        private void SelectOrb(IOrbControl in_oToSelect)
        {
            App.AssemblyLoadManager.Requires("Editors.xap");
            // we loop and CLOSE all other orbs,
            // and OPEN our selected orb.
            foreach (IOrbControl o in m_arOrbs)
            {
                if (o != in_oToSelect)
                {
                    o.IsSlideOutOpen = false;
                    Canvas.SetZIndex(o.Control, 0);
                }
                else
                {
                    Canvas.SetZIndex(o.Control, 1);
                    o.IsSlideOutOpen = true;
                }
            }
        }

        public IOrbControl CreateOrb(Color in_OrbColor)
        {
            IOrbControl o = CreateOrb();
            o.Color = in_OrbColor;
            return o;
        }

        public IOrbControl CreateOrb()
        {
            IOrbControl o = new OrbControl() as IOrbControl;

            // we need to mark it as a tabstop so that it can 
            // accept focus (clicks) which defocus (and cause a
            // property change in) the textboxes.
            o.Control.IsTabStop = true;
            
            // start collapseD!
            o.Control.Visibility = Visibility.Collapsed;
            m_oPositioningCanvas.Children.Add(o.Control);
            m_arOrbs.Add(o);

            o.Control.MouseEnter += new MouseEventHandler(OrbMouseEnter);
            o.Control.MouseLeave += new MouseEventHandler(OrbMouseLeave);
            return o;
        }

        private void OrbMouseLeave(object sender, MouseEventArgs e)
        {
            if (HideOnMouseLeave)
            {
                m_timerHideOrbs.Interval = new TimeSpan(0, 0, 0, 1);
                m_timerHideOrbs.Start();
            }
        }

        #region ISelectable Members

        public event SelectionEventHandler SelectionStateChanged;

        private bool m_bSelected = false;
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
                    if (value)
                    {
                        ShowOrbs();
                    }
                    else
                    {
                        HideOrbs();
                    }

                    m_bSelected = value;
                    if (SelectionStateChanged != null)
                        SelectionStateChanged.Invoke(this as ISelectable, new SelectionEventArgs(m_bSelected));
                }
            }
        }

        #endregion
    }
}
