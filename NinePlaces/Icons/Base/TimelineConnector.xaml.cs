using System.Windows.Controls;
using System.Windows.Media;
using Common.Interfaces;
using System.Windows;

namespace NinePlaces
{
	public partial class TimelineConnector : UserControl
	{
        public TimelineConnector()
        {
            // Required to initialize variables
            InitializeComponent();
            SizeChanged += new SizeChangedEventHandler(LayoutRoot_SizeChanged);
        }

        public Color Color
        {
            get
            {
                return ((SolidColorBrush)ConnectorPath.Stroke).Color;
            }
            set
            {
                ConnectorPath.Stroke = new SolidColorBrush(value);
                DurationIndicator.Fill = new SolidColorBrush(value);
            }
        }
        
        void LayoutRoot_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            ConnectorPath.Height = ActualHeight;
        }
	}
}