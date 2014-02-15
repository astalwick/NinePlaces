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
using Common.Interfaces;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Collections.ObjectModel;

namespace NinePlaces
{
	public partial class FirstVisitDialog : UserControl
	{

		public FirstVisitDialog()
		{
            InitializeComponent();
            Loaded += new RoutedEventHandler(FirstVisitDialog_Loaded);
            
            DataContext = this;

            CloseButton.Click += new RoutedEventHandler(CloseButton_Click);
		}

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Close != null)
            {
                Close.Invoke(this, new EventArgs());
            }
        }

        void FirstVisitDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Log.TrackPageView("/NinePlaces/FirstVisit"); 
        }


        public event EventHandler Close;
	}
}