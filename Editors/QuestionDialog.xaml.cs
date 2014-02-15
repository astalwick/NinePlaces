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

namespace Editors
{
	public partial class AreYouSureDialog : UserControl
	{

        public string Consequence { get; set; }
        public bool UserSaidYes { get; private set; }
		public AreYouSureDialog(string in_strConsequenceDesc)
		{
            InitializeComponent();

            ResourceManager m = new ResourceManager("Editors.Localization.StringLibrary", Assembly.GetExecutingAssembly());
            Consequence = m.GetString(in_strConsequenceDesc);

            Loaded += new RoutedEventHandler(QuestionDialog_Loaded);

            UserSaidYes = false;

            NoButton.Click += new RoutedEventHandler(NoButton_Click);
            YesButton.Click += new RoutedEventHandler(YesButton_Click);
            DataContext = this;
		}

        void YesButton_Click(object sender, RoutedEventArgs e)
        {
            UserSaidYes = true;
            if (Close != null)
                Close.Invoke(this, new EventArgs());
        }

        void NoButton_Click(object sender, RoutedEventArgs e)
        {
            UserSaidYes = false;
            if (Close != null)
                Close.Invoke(this, new EventArgs());
        }

        public event EventHandler Close;
    
        void QuestionDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Log.TrackPageView("/NinePlaces/Question"); 
            NoButton.Focus();

        }
	}
}