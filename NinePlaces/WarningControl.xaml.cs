using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace NinePlaces
{
	public partial class WarningControl : UserControl
	{
		public WarningControl()
		{
			// Required to initialize variables
			InitializeComponent();

            MouseEnter += new MouseEventHandler(WarningControl_MouseEnter);
            MouseLeave += new MouseEventHandler(WarningControl_MouseLeave);

		}

        void WarningControl_MouseLeave(object sender, MouseEventArgs e)
        {
            ReleaseMouseCapture();
            VisualStateManager.GoToState(this as Control, "NoHover", true);
        }

        void WarningControl_MouseEnter(object sender, MouseEventArgs e)
        {
            CaptureMouse();
            VisualStateManager.GoToState(this as Control, "Hover", true);
        }
	}
}