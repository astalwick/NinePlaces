using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common.Interfaces;
using NinePlaces.ViewModels;
using System.Collections.Generic;
using Common;

namespace NinePlaces
{


	public partial class WhereAmIControl : UserControl
	{
        private Dictionary<int, Button> m_dictIDToButton = new Dictionary<int, Button>();
		public WhereAmIControl()
		{
            App.WhereAmI = this;
			// Required to initialize variables
			InitializeComponent();
            Loaded += new RoutedEventHandler(WhereAmIControl_Loaded);
		}

        void WhereAmIControl_Loaded(object sender, RoutedEventArgs e)
        {
            //
            // ok, we're loaded and we SHOULD have a VacationID. 
            // lets go get the model.
            //
            m_oVM = new WhereAmIViewModel(VacationID);
            m_oVM.WhereAmIChanged += new EventHandler(m_oVM_WhereAmIChanged);
        }

        public void UpdateWidths()
        {
            if (m_oVM == null)
                return;

            int n = 0;
            List<IWhereAmIElement> arElements = m_oVM.Elements;
            if (arElements.Count < 2)       // no point in doing anything if we don't have a couple travel icons!
                return;

            Line lPrecedingLine = null;
            bool bPrevButtonVisible = false;

            foreach( UIElement oChild in WhereAmIPanel.Children )
            {
                if (oChild is Line)
                {
                    // 
                    // ok, lets remember the preceding line, so that when we evaluate the next buttono's
                    // hidden state, if IT is hidden, then maybe the line should be hidden too.
                    //
                    lPrecedingLine = oChild as Line;
                    lPrecedingLine.Opacity = 100;       // we assume it'll be visible.  we'll update if not.
                    continue;
                }

                //
                // get the start and end point (pixel pos) for this button.
                //
                double dEnd = App.Timeline.TimeToPosition(arElements[n+1].Start);
                double dStart = App.Timeline.TimeToPosition(arElements[n].Start);

                Button b = oChild as Button;
                b.Width = dEnd - dStart;
                if (string.IsNullOrEmpty(b.Content as string) || MeasureButtonTextWidth(b) > b.Width)
                {
                    //
                    // ok, THIS BUTTON is going to be hidden.
                    // IF the previous button was hidden as well, then we shouldn't display
                    // the previous dash.  both sides of the dash are hidden!
                    //
                    if (lPrecedingLine != null && !bPrevButtonVisible)
                        lPrecedingLine.Opacity = 0;

                    bPrevButtonVisible = false;
                    b.Opacity = 0;
                }
                else
                {
                    // ah we're visible.  great.
                    bPrevButtonVisible = true;
                    b.Opacity = 100;
                }

                n++;
            }
        }

        private TextBlock m_bTempTextBlock = new TextBlock();
        double MeasureButtonTextWidth(Button in_bButton)
        {
            // bit of a hack to figure out the width that our text will take up.
            m_bTempTextBlock.Text = in_bButton.Content as string;
            m_bTempTextBlock.FontSize = in_bButton.FontSize;
            m_bTempTextBlock.FontFamily = in_bButton.FontFamily;

            return m_bTempTextBlock.ActualWidth + in_bButton.Padding.Left + in_bButton.Padding.Right;
        }

        void RepopulateChildren()
        {
            DateTime dtNextElem = DateTime.MinValue;
            // we need to sort out all of the buttons right here!
            List<IWhereAmIElement> arElements = m_oVM.Elements;
            for (int i = 0; i < arElements.Count - 1; i++)
            {
                IWhereAmIElement elem = arElements[i];
                Button b = null;
                if (!m_dictIDToButton.ContainsKey(elem.UniqueID))
                {
                    // create the button, and toss it into the list so that
                    // we can look it up later
                    b = new Button();
                    b.Style = this.Resources["NoStyleButton"] as Style;
                    m_dictIDToButton.Add(elem.UniqueID, b);
                }
                else
                {
                    b = m_dictIDToButton[elem.UniqueID];
                }
                dtNextElem = arElements[i + 1].Start;
                b.Content = elem.Location;

                WhereAmIPanel.Children.Add(b);

                if (i + 2 < arElements.Count)
                {
                    // create and add the line.
                    Line l = new Line();
                    l.Style = this.Resources["WhereAmIDivider"] as Style;
                    WhereAmIPanel.Children.Add(l);
                }
            }
        }

        void m_oVM_WhereAmIChanged(object sender, EventArgs e)
        {
            WhereAmIPanel.Children.Clear();
            RepopulateChildren();
            UpdateWidths();
        }

        public int VacationID { get; set; }

        private IWhereAmIViewModel m_oVM = null;
        public IWhereAmIViewModel VM
        {
            get
            {
                return m_oVM as IWhereAmIViewModel;
            }
            set
            {
                if (value == null)
                    m_oVM = null;
                else
                {
                    System.Diagnostics.Debug.Assert(value is IWhereAmIViewModel);
                    m_oVM = value as IWhereAmIViewModel;
                }
            }
        }
	}
}