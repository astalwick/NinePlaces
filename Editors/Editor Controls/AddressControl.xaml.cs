using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Resources;
using System.Reflection;
using Common.Interfaces;
using Editors;
using Common;
using System.Net;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
namespace BasicEditors
{
	public partial class AddressControl : UserControl, INotifyPropertyChanged
	{
        ResourceManager m_oResources;
        double m_dDisplayHeight = 0;
        double m_dEditHeight = 0;
		public AddressControl()
		{
			// Required to initialize variables
			InitializeComponent();
            m_oResources = new ResourceManager("Editors.Localization.StringLibrary", Assembly.GetExecutingAssembly());

            tbDisplayTabStop.GotFocus += new RoutedEventHandler(ControlGotFocus);
            MouseLeftButtonDown += new MouseButtonEventHandler(LayoutRoot_MouseLeftButtonDown);
            MouseLeftButtonUp += new MouseButtonEventHandler(LayoutRoot_MouseLeftButtonUp);
            LayoutRoot.MouseLeftButtonDown += new MouseButtonEventHandler(LayoutRoot_MouseLeftButtonDown);
            LayoutRoot.MouseLeftButtonUp += new MouseButtonEventHandler(LayoutRoot_MouseLeftButtonUp);
            tbDisplay.MouseLeftButtonDown += new MouseButtonEventHandler(tbDisplay_MouseLeftButtonDown);

            //
            // set up the bindings manually.  seems setting DataContext = this gives
            // a sketchy stack overflow.
            //
            Binding bStreet = new Binding("Street") { Source = this, Mode = BindingMode.TwoWay };
            tbStreet.SetBinding(TextBox.TextProperty, bStreet);

            Binding bCity = new Binding("SelectedLocation") { Source = this, Mode = BindingMode.TwoWay };
            tbCity.SetBinding(AutoCompleteBox.SelectedItemProperty, bCity);
            
            Binding bCityName = new Binding("City") { Source = this, Mode = BindingMode.TwoWay};
            tbCity.SetBinding(AutoCompleteBox.TextProperty, bCityName);

            Binding bProvince = new Binding("Province") { Source = this, Mode = BindingMode.TwoWay };
            tbProvince.SetBinding(TextBox.TextProperty, bProvince);

            Binding bCountry = new Binding("Country") { Source = this, Mode = BindingMode.TwoWay };
            tbCountry.SetBinding(TextBox.TextProperty, bCountry);

            Binding bPostalCode = new Binding("PostalCode") { Source = this, Mode = BindingMode.TwoWay };
            tbPostal.SetBinding(TextBox.TextProperty, bPostalCode);

            Location = new LocationObject();
            SelectedLocation = new LocationObject();
            tbCity.Populating += new PopulatingEventHandler(tbCity_Populating);
            Location.PropertyChanged += new PropertyChangedEventHandler(Location_PropertyChanged);
            
            Loaded += new RoutedEventHandler(AddressControl_Loaded);
        }

        void Location_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CityName":
                    City = Location.CityName;
                    break;
                case "Country":
                    Country = Location.Country;
                    break;
                case "Province":
                    Province = Location.Province;
                    break;
                case "PostalCode":
                    PostalCode = Location.PostalCode;
                    break;
                case "StreetAddress":
                    Street = Location.StreetAddress;
                    break;

                case "TimeZoneID":
                    TimeZoneID = Location.TimeZoneID;
                    break;
            }
            UpdateDisplayText();
        }
        
        ILocationObject m_oLocationObject = null;
        public ILocationObject Location
        {
            get
            {
                return m_oLocationObject;
            }
            set
            {
                m_oLocationObject = value;
            }
        }

        ILocationObject m_oSelectedLocation = null;
        public ILocationObject SelectedLocation
        {
            get
            {
                return m_oSelectedLocation;
            }
            set
            {
                m_oSelectedLocation = value;
                if (m_oSelectedLocation != null)
                {
                    Location.Copy(m_oSelectedLocation);
                }
                else
                {
                    m_oSelectedLocation = new LocationObject();
                    m_oSelectedLocation.Copy(Location);
                }
            }
        }

        void AddressControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDisplayText();
        }
        
        #region Properties
        public static readonly DependencyProperty StreetProperty = DependencyProperty.Register("Street", typeof(string), typeof(AddressControl), new PropertyMetadata(new PropertyChangedCallback(StreetChangedCallback)));
        public string Street
        {
            get { return (string)this.GetValue(StreetProperty); }
            set { base.SetValue(StreetProperty, value); }
        }
        private static void StreetChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;

            AddressControl r = d as AddressControl;
            r.Location.StreetAddress = e.NewValue as string;
        }


        public static readonly DependencyProperty TimeZoneIDProperty = DependencyProperty.Register("TimeZoneID", typeof(string), typeof(AddressControl), new PropertyMetadata(new PropertyChangedCallback(TimeZoneChangedCallback)));
        public string TimeZoneID
        {
            get { return (string)this.GetValue(TimeZoneIDProperty); }
            set { base.SetValue(TimeZoneIDProperty, value); }
        }
        private static void TimeZoneChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;
            AddressControl r = d as AddressControl;
            r.Location.TimeZoneID = e.NewValue as string;
        }


        public static readonly DependencyProperty CityProperty = DependencyProperty.Register("City", typeof(string), typeof(AddressControl), new PropertyMetadata(new PropertyChangedCallback(CityChangedCallback)));
        public string City
        {
            get { return (string)this.GetValue(CityProperty); }
            set { base.SetValue(CityProperty, value); }
        }
        private static void CityChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;
            AddressControl r = d as AddressControl;
            r.Location.CityName = e.NewValue as string;
        }

        public static readonly DependencyProperty ProvinceProperty = DependencyProperty.Register("Province", typeof(string), typeof(AddressControl), new PropertyMetadata(new PropertyChangedCallback(ProvinceChangedCallback)));
        public string Province
        {
            get { return (string)this.GetValue(ProvinceProperty); }
            set { this.SetValue(ProvinceProperty, value); }
        }
        private static void ProvinceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;
            AddressControl r = d as AddressControl;
            r.Location.Province = e.NewValue as string;
        }

        public static readonly DependencyProperty CountryProperty = DependencyProperty.Register("Country", typeof(string), typeof(AddressControl), new PropertyMetadata(new PropertyChangedCallback(CountryChangedCallback)));
        public string Country
        {
            get { return (string)this.GetValue(CountryProperty); }
            set { this.SetValue(CountryProperty, value); }
        }
        private static void CountryChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;
            AddressControl r = d as AddressControl;
            r.Location.Country = e.NewValue as string;
        }

        public static readonly DependencyProperty PostalCodeProperty = DependencyProperty.Register("PostalCode", typeof(string), typeof(AddressControl), new PropertyMetadata(new PropertyChangedCallback(PostalCodeChangedCallback)));
        public string PostalCode
        {
            get { return (string)this.GetValue(PostalCodeProperty); }
            set { this.SetValue(PostalCodeProperty, value); }
        }
        private static void PostalCodeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;
            AddressControl r = d as AddressControl;
            r.Location.PostalCode = e.NewValue as string;
        }

        #endregion

        void UpdateDisplayText()
        {
            
            // this is a bit of a hack and should probably be cleaned up
            // basically jsut takes the text from the textboxes (should be from
            // the properties?) and tosses them into our display textbox.
            tbDisplay.Text = "";

            if( tbStreet.Text != "" )
                tbDisplay.Text = tbStreet.Text + "\r\n";

            bool bComma = false;
            if (tbCity.Text != null && tbCity.Text.Length > 0)
            {
                tbDisplay.Text += tbCity.Text;
                bComma = true;
            }

            if (tbProvince.Text.Length > 0)
            {
                if (bComma)
                    tbDisplay.Text += ", ";
                tbDisplay.Text += tbProvince.Text;
                bComma = true;
            }

            if (tbPostal.Text.Length > 0)
            {
                if (bComma)
                    tbDisplay.Text += ", ";
                tbDisplay.Text += tbPostal.Text;
                bComma = true;
            }
            
            if (tbCountry.Text != "")
            {
                if (tbDisplay.Text != "")
                    tbDisplay.Text += "\r\n";
                tbDisplay.Text += tbCountry.Text;
            }

            if (tbDisplay.Text == "")
            {
                tbDisplay.Opacity = .5;

                tbDisplay.Text = "[" + m_oResources.GetString("ClickToEnterAddress") + "]";
            }
            else
            {
                tbDisplay.Opacity = 1.0;
            }
        }

        void ToEditMode()
        { 
            // REMEMBER our display height so that we
            // can go back to the right height later.
            if (m_dDisplayHeight == 0)
                m_dDisplayHeight = LayoutRoot.ActualHeight;

            // we HIDE the display editbox and textblock.
            // they can't take mouseclicks and shouldn't be
            // visible!
            tbDisplay.Opacity = 0;
            tbDisplayTabStop.Opacity = 0;
            tbDisplay.Visibility = Visibility.Collapsed;
            tbDisplayTabStop.Visibility = Visibility.Collapsed;

            //
            // show all of the edit controls
            //
            lblStreet.Opacity = 100;
            tbStreet.Opacity = 100;
            lblCity.Opacity = 100;
            tbCity.Opacity = 100;
            lblProvince.Opacity = 100;
            tbProvince.Opacity = 100;
            lblPostal.Opacity = 100;
            tbPostal.Opacity = 100;
            lblCountry.Opacity = 100;
            tbCountry.Opacity = 100;

            lblStreet.Visibility = Visibility.Visible;
            tbStreet.Visibility = Visibility.Visible;
            lblCity.Visibility = Visibility.Visible;
            tbCity.Visibility = Visibility.Visible;
            lblProvince.Visibility = Visibility.Visible;
            tbProvince.Visibility = Visibility.Visible;
            lblPostal.Visibility = Visibility.Visible;
            tbPostal.Visibility = Visibility.Visible;
            lblCountry.Visibility = Visibility.Visible;
            tbCountry.Visibility = Visibility.Visible;

            //
            // ok, we haven't done this before, so we need to figure
            // out the EDITING MODE height.
            //
            if (m_dEditHeight == 0)
            {
                LayoutRoot.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                m_dEditHeight = LayoutRoot.DesiredSize.Height;
            }

            // 
            // begin the animation to the new height.
            //
            AnimateToHeight(m_dDisplayHeight, m_dEditHeight, new EventHandler(ToEdit_Completed));

            //
            // focus our street textbox so that we can start typing right away.
            //
            tbStreet.Focus();

            //
            // we don't want to know about focus changes for the display controls.
            //
            tbDisplay.GotFocus -= new RoutedEventHandler(ControlGotFocus);
            tbDisplayTabStop.GotFocus -= new RoutedEventHandler(ControlGotFocus);

            //
            // we DO need to know about focus changes for the edit controls though.
            //
            tbStreet.LostFocus += new RoutedEventHandler(ControlLostFocus);
            tbCity.LostFocus += new RoutedEventHandler(ControlLostFocus);
            tbProvince.LostFocus += new RoutedEventHandler(ControlLostFocus);
            tbPostal.LostFocus += new RoutedEventHandler(ControlLostFocus);
            tbCountry.LostFocus += new RoutedEventHandler(ControlLostFocus);
        }

        public virtual void tbCity_Populating(object sender, PopulatingEventArgs e)
        {
            e.Cancel = true;
            CallToWebService(sender as AutoCompleteBox);
        }

        public virtual void CallToWebService(AutoCompleteBox in_acb)
        {
            WebClient wc = new WebClient();

            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(LocationQueryComplete);
            wc.DownloadStringAsync(new Uri(Helpers.GetAPIURL() + "Cities?q=" + in_acb.SearchText), in_acb);
        }

        public virtual void LocationQueryComplete(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                return;
            }

            AutoCompleteBox acb = e.UserState as AutoCompleteBox;

            List<string> data = new List<string>();
            try
            {
                XElement xCities = XElement.Parse(e.Result);

                var x = from value in xCities.Elements()
                        select new LocationObject()
                        {
                            CityName = value.Element("UnicodeName").Value,
                            Country = value.Element("CountryCode").Value,
                            Province = value.Element("AdminRegion").Value,
                            TimeZoneID = value.Element("TimeZone").Element("Identifier").Value
                        };


                List<LocationObject> l = new List<LocationObject>();
                if (x.Count() != 0)
                {
                    l.AddRange(x);
                    acb.ItemsSource = l;
                }
                else
                {
                    
                    l.Add(new LocationObject()
                    {
                        CityName = acb.SearchText
                    });

                    acb.ItemsSource = l;
                }

                acb.PopulateComplete();
            }
            catch { }
        }

        void ToDisplayMode()
        {
            if (m_sbPositionAnim != null)
                return;

            //
            // this is called whenn one of the edit textboxes has 
            // lost focus.  we need to check - is the currently focused
            // control one of our edit controls?  if so, then we
            // don't go to display mode - we're still editing.
            //
            object tbFocused = FocusManager.GetFocusedElement() as object;
            if (tbFocused == null ||
                !(
                tbFocused == tbStreet ||
                tbFocused == tbCity ||
                tbFocused == tbProvince ||
                tbFocused == tbPostal ||
                tbFocused == tbCountry ||
                Helpers.IsAncestorControl(tbCity as FrameworkElement, tbFocused as FrameworkElement)))
            {

                //
                // we're no longer focused in the control textboxes.  
                // back to display mode.
                //

                //
                // hide our edit controls
                //
                lblStreet.Opacity = 0;
                tbStreet.Opacity = 0;
                lblCity.Opacity = 0;
                tbCity.Opacity = 0;
                lblProvince.Opacity = 0;
                tbProvince.Opacity = 0;
                lblPostal.Opacity = 0;
                tbPostal.Opacity = 0;
                lblCountry.Opacity = 0;
                tbCountry.Opacity = 0;

                //
                // show our display controls
                //
                tbDisplay.Visibility = Visibility.Visible;
                tbDisplay.Opacity = 100;
                tbDisplayTabStop.Visibility = Visibility.Visible;

                //
                // no more lost focus notifications from our edit controls!
                //
                tbStreet.LostFocus -= new RoutedEventHandler(ControlLostFocus);
                tbCity.LostFocus -= new RoutedEventHandler(ControlLostFocus);
                tbProvince.LostFocus -= new RoutedEventHandler(ControlLostFocus);
                tbPostal.LostFocus -= new RoutedEventHandler(ControlLostFocus);
                tbCountry.LostFocus -= new RoutedEventHandler(ControlLostFocus);

                //
                // lets update the text we display in the display textblock.
                //
                UpdateDisplayText();

                //
                // and finally, begin the animation to the correct display height.
                //
                AnimateToHeight(m_dEditHeight, m_dDisplayHeight, new EventHandler(ToDisplay_Completed));
            }
        }

        void LayoutRoot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // we do not want our parent control getting this mouseup!
            e.Handled = true;
        }

        void LayoutRoot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // we do not want our parent control getting this mouseup!
            e.Handled = true;
        }

        void ControlLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == tbCity)
            {
                List<LocationObject> aPossibleItems = (List<LocationObject>)tbCity.ItemsSource;
                if (tbCity.SelectedItem == null && aPossibleItems != null && aPossibleItems.Count > 0)
                {
                    tbCity.SelectedItem = aPossibleItems[0];
                }
            }
            else
            {
                ToDisplayMode();
            }
        }

        void ControlGotFocus(object sender, RoutedEventArgs e)
        {
            ToEditMode();
        }

        void tbDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToEditMode();
        }

        private Storyboard m_sbPositionAnim = null;
        public void AnimateToHeight(double in_dOldHeight, double in_dNewHeight, EventHandler in_eEventHandler)
        {
            if (m_sbPositionAnim != null)
            {
                m_sbPositionAnim.SkipToFill();
                m_sbPositionAnim = null;
            }

            //
            // create the position animation.
            //
            m_sbPositionAnim = new Storyboard();

            // our Height animation
            DoubleAnimation ptY = new DoubleAnimation();
            ptY.Duration = TimeSpan.FromSeconds(0.10);
            ptY.From = in_dOldHeight;
            ptY.To = in_dNewHeight;
            Storyboard.SetTarget(ptY, LayoutRoot);
            PropertyPath p = new PropertyPath("Height");
            Storyboard.SetTargetProperty(ptY, p);
            m_sbPositionAnim.Children.Add(ptY);

            m_sbPositionAnim.Begin();

            m_sbPositionAnim.Completed += in_eEventHandler; 
        }

        void ToEdit_Completed(object sender, EventArgs e)
        {
            //
            // we're in edit mode now
            //
            m_sbPositionAnim = null;
            
            //
            // make sure our display controls do NOT get in our way.
            //
            tbDisplay.IsHitTestVisible = false;
            tbDisplay.Visibility = Visibility.Collapsed;
            tbDisplayTabStop.IsHitTestVisible = false;
            tbDisplayTabStop.Visibility = Visibility.Collapsed;

            //
            // focus up the street textbox.
            //
            tbStreet.Focus();
        }

        void ToDisplay_Completed(object sender, EventArgs e)
        {
            //
            // we're in display mode now!
            //
            m_sbPositionAnim = null;

            //
            // the display controls are visible and are
            // now tabstops again!
            //
            tbDisplay.IsHitTestVisible = true;
            tbDisplayTabStop.IsHitTestVisible = true;

            //
            // make sure our edit controls do not get in our way.
            //
            lblStreet.Visibility = Visibility.Collapsed;
            tbStreet.Visibility = Visibility.Collapsed;
            lblCity.Visibility = Visibility.Collapsed;
            tbCity.Visibility = Visibility.Collapsed;
            lblProvince.Visibility = Visibility.Collapsed;
            tbProvince.Visibility = Visibility.Collapsed;
            lblPostal.Visibility = Visibility.Collapsed;
            tbPostal.Visibility = Visibility.Collapsed;
            lblCountry.Visibility = Visibility.Collapsed;
            tbCountry.Visibility = Visibility.Collapsed;

            //
            // reattach the display-gotfocus events, so that 
            // we can go back to edit mode later.
            //
            tbDisplay.GotFocus += new RoutedEventHandler(ControlGotFocus);
            tbDisplayTabStop.GotFocus += new RoutedEventHandler(ControlGotFocus);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}