using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common;

namespace Editors
{
	public partial class GetStartedPopup : UserControl
	{
		public GetStartedPopup()
		{
			// Required to initialize variables
			InitializeComponent();
            WhatIsItButton.MouseLeftButtonUp += new MouseButtonEventHandler(WhatIsItButton_MouseLeftButtonUp);
            HowDidItStartButton.MouseLeftButtonUp += new MouseButtonEventHandler(HowDidItStartButton_MouseLeftButtonUp);
            HowDoesItWorkButton.MouseLeftButtonUp += new MouseButtonEventHandler(HowDoesItWork_MouseLeftButtonUp);
            Loaded += new RoutedEventHandler(GetStartedPopup_Loaded);
            CloseButton.Click += new RoutedEventHandler(CloseButton_Click);
		}

        void GetStartedPopup_Loaded(object sender, RoutedEventArgs e)
        {
            Log.TrackPageView("/NinePlaces/GetStarted/WhatIsIt"); 
        }
        public event EventHandler Close;

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Close != null)
                Close.Invoke(this, new EventArgs());
        }

        void HowDoesItWork_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Log.TrackPageView("/NinePlaces/GetStarted/HowDoesItWork"); 
            VisualStateManager.GoToState(this, "HowDoesItWorkSelected", true);
        }

        void HowDidItStartButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Log.TrackPageView("/NinePlaces/GetStarted/HowDidItStart"); 
            VisualStateManager.GoToState(this, "HowDidItStartSelected", true);
        }

        void WhatIsItButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Log.TrackPageView("/NinePlaces/GetStarted/WhatIsIt"); 
            VisualStateManager.GoToState(this, "WhatIsItSelected", true);
        }
	}
}