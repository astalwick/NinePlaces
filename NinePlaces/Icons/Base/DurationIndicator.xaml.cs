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

namespace NinePlaces
{
	public partial class DurationIndicator : UserControl
	{
		public DurationIndicator()
		{
			// Required to initialize variables
			InitializeComponent();
            DurationEnabled = false;
		}

        private Color m_oColor;
        public Color Color
        {
            get
            {
                return m_oColor;
            }
            set
            {
                if (m_oColor != value)
                {
                    m_oColor = value;

                    LayoutRoot.Background = new SolidColorBrush(m_oColor);
                }
            }
        }

        private Color m_oDarkColor;
        public Color DarkColor
        {
            get
            {
                return m_oDarkColor;
            }
            set
            {
                if (m_oColor != value)
                {
                    m_oDarkColor = value;

                    DurationEllipse.Fill = new SolidColorBrush(m_oDarkColor);
                }
            }
        }
        bool m_bDurationEnabled = false;
        public bool DurationEnabled
        {
            get
            {
                return m_bDurationEnabled;
            }
            set
            {
                m_bDurationEnabled = value;
                LayoutRoot.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public double DurationWidth
        {
            get
            {
                return LayoutRoot.Width;
            }
            set
            {

                if (value > 50.0)
                {
                    LayoutRoot.Width = value - 15.0;
                    if (DurationEnabled)
                        LayoutRoot.Visibility = Visibility.Visible;
                }
                else
                {
                    LayoutRoot.Visibility = Visibility.Collapsed;
                }
            }
        }
	}
}