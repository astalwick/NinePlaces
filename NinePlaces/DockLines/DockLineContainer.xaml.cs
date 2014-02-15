using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using NinePlaces.Localization;

namespace NinePlaces
{
	public partial class DockLineContainer : UserControl
	{
		public DockLineContainer()
		{
			// Required to initialize variables
			InitializeComponent();
		}

        public bool Dock(IDockable in_oToDock)
        {
            foreach (DockLine line in LayoutRoot.Children)
            {
                if (line.Dock(in_oToDock))
                    return true;
            }
                     
            return false;
        }

        public bool Undock(IDockable in_oToUndock)
        {
            foreach (DockLine line in LayoutRoot.Children)
            {
                if (line.Undock(in_oToUndock))
                    return true;
            }
            return false;
        }


        public bool Undock(int in_nUniqueID)
        {
            foreach (DockLine line in LayoutRoot.Children)
            {
                if (line.Undock(in_nUniqueID))
                    return true;
            }
            return false;
        }


        public void Clear()
        {
            foreach (DockLine line in LayoutRoot.Children)
            {
                line.Clear();
            }
        }


        internal void DragEnd(IDockable ic)
        {
            foreach (DockLine line in LayoutRoot.Children)
            {
                line.DragEnd(ic);
            }
        }

        internal void DragStart(IDockable ic)
        {
            foreach (DockLine line in LayoutRoot.Children)
            {
                line.DragStart(ic);
            }
        }
    }
}