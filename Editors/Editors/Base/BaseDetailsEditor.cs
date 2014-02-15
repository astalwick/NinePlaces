using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common.Interfaces;
using System.Xml.Linq;
using System.Windows.Data;
using Common;

namespace Editors
{
    [TemplatePart(Name = "Content", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "BackgroundGradient", Type = typeof(LinearGradientBrush))]
    [TemplatePart(Name = "ShadingGradient", Type = typeof(LinearGradientBrush))]
    [TemplatePart(Name = "TravelEditorTopVisibility", Type = typeof(Visibility))]
    [TemplatePart(Name = "ActivityEditorTopVisibility", Type = typeof(Visibility))]
    [TemplatePart(Name = "LodgingEditorTopVisibility", Type = typeof(Visibility))]
    public class BaseDetailsEditor : ContentControl, IDetailsEditor
    {
        public BaseDetailsEditor()
        {
            this.DefaultStyleKey = typeof(BaseDetailsEditor);
            MouseLeftButtonDown += new MouseButtonEventHandler(BaseDetailsEditor_MouseLeftButtonDown);
            MouseLeftButtonUp += new MouseButtonEventHandler(BaseDetailsEditor_MouseLeftButtonUp);
            TravelEditorTopVisibility = Visibility.Collapsed;
        }

        private IconClass m_oIconClass = IconClass.Undefined;
        public IconClass IconClass
        {
            get
            {
                return m_oIconClass;
            }
            set
            {
                m_oIconClass = value;
                switch (m_oIconClass)
                {
                    case Common.Interfaces.IconClass.Activity:
                        TravelEditorTopVisibility = Visibility.Collapsed;
                        ActivityEditorTopVisibility = Visibility.Visible;
                        LodgingEditorTopVisibility = Visibility.Collapsed;
                        break;
                    case Common.Interfaces.IconClass.Lodging:
                        TravelEditorTopVisibility = Visibility.Collapsed;
                        ActivityEditorTopVisibility = Visibility.Collapsed;
                        LodgingEditorTopVisibility = Visibility.Visible;
                        break;
                    case Common.Interfaces.IconClass.Transportation:
                        TravelEditorTopVisibility = Visibility.Visible;
                        ActivityEditorTopVisibility = Visibility.Collapsed;
                        LodgingEditorTopVisibility = Visibility.Collapsed;
                        break;
                    default:
                        TravelEditorTopVisibility = Visibility.Collapsed;
                        ActivityEditorTopVisibility = Visibility.Collapsed;
                        LodgingEditorTopVisibility = Visibility.Collapsed;
                        break;
                }
            }
        }

        void BaseDetailsEditor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        void BaseDetailsEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Focus();
            e.Handled = true;
        }

        public virtual void LocationAutoCompleteBox_Populating(object sender, PopulatingEventArgs e)
        {
            FrameworkElement fe = FocusManager.GetFocusedElement() as FrameworkElement;
            e.Cancel = true;
            //
            // Here's the problem - when you set the TEXT property on a autocompletebox,
            // that causes it to fire off its goofy notifcations (this populating notification).
            //
            // However, in our case, we sometimes set the text property of the departure field
            // based on the value selected in the ARRIVAL field of the PREVIOUS icon.
            //
            // in that case, we DON'T want the autocomplete to fire. 
            //
            if (Helpers.IsAncestorControl(sender as FrameworkElement, fe))
            {
                CallToWebService(sender as AutoCompleteBox);
            }
        }


        public virtual void LocationAutoCompleteBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AutoCompleteBox acb = sender as AutoCompleteBox;
            List<LocationObject> aPossibleItems = (List<LocationObject>)acb.ItemsSource;
            if (acb.SelectedItem == null && aPossibleItems != null && aPossibleItems.Count > 0)
            {
                acb.SelectedItem = aPossibleItems[0];
            }
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

        /// <summary>
        /// Show is going to display the details editor.
        /// It will position it relative to the icon.
        /// </summary>
        //
        private const double m_dSpacing = 16.0;
        private const double m_dCenteringOffset = -100.0;
        private const double m_dDesiredWidth = 300.0;
        public void Show()
        {
            // determine where to show ourselveseasure

            GeneralTransform aobjGeneralTransform = ((UIElement)Parent).TransformToVisual(Helpers.App.PageRoot);
            Point ptParentTopLeft = aobjGeneralTransform.Transform(new Point(0,0));

            Point pt = aobjGeneralTransform.Transform(new Point(ParentWidth + m_dSpacing + m_dDesiredWidth, m_dCenteringOffset));

            Size sz = Helpers.App.PageRoot.RenderSize;
            if (sz.Width - ptParentTopLeft.X - ParentWidth - m_dSpacing - m_dDesiredWidth > 0)
            {
                Canvas.SetLeft(this, ParentWidth + m_dSpacing);
            }
            else
            {
                // position on left - we don't have space to the right.
                Canvas.SetLeft(this, (-1.0 * +m_dDesiredWidth) - m_dSpacing);
            }

            double dY = 0;
            if (pt.Y > -m_dCenteringOffset)
                dY = m_dCenteringOffset;

            Canvas.SetTop(this, dY);

            Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            this.Focus();
            Visibility = Visibility.Collapsed;
        }

        //
        private double m_dParentWidth = 0.0;
        /// <summary>
        /// Sets the parent icon width.  This is done so that we
        /// can position ourselves relative to the icon, even if the icon
        /// is a strange size.  (see: photo icon).
        /// 
        /// Not terribly good code.  When the editor does a better
        /// job of positioning itself, this can be removed.
        /// </summary>
        public double ParentWidth
        {
            get
            {
                return m_dParentWidth;
            }
            set
            {
                if (m_dParentWidth != value)
                    m_dParentWidth = value;
            }
        }

        public LinearGradientBrush ShadingGradient
        {
            get { return (LinearGradientBrush)GetValue(ShadingGradientProperty); }
            set { SetValue(ShadingGradientProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShadingGradient.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShadingGradientProperty =
            DependencyProperty.Register("ShadingGradient", typeof(LinearGradientBrush), typeof(BaseDetailsEditor), null);

        public LinearGradientBrush BackgroundGradient
        {
            get { return (LinearGradientBrush)GetValue(BackgroundGradientProperty); }
            set { SetValue(BackgroundGradientProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackgroundGradient.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundGradientProperty =
            DependencyProperty.Register("BackgroundGradient", typeof(LinearGradientBrush), typeof(BaseDetailsEditor), null);


        public Visibility TravelEditorTopVisibility
        {
            get { return (Visibility)GetValue(TravelEditorTopVisibilityProperty); }
            set { SetValue(TravelEditorTopVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShadingGradient.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TravelEditorTopVisibilityProperty =
            DependencyProperty.Register("TravelEditorTopVisibility", typeof(Visibility), typeof(BaseDetailsEditor), null);



        public Visibility LodgingEditorTopVisibility
        {
            get { return (Visibility)GetValue(LodgingEditorTopVisibilityProperty); }
            set { SetValue(LodgingEditorTopVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShadingGradient.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LodgingEditorTopVisibilityProperty =
            DependencyProperty.Register("LodgingEditorTopVisibility", typeof(Visibility), typeof(BaseDetailsEditor), null);


        public Visibility ActivityEditorTopVisibility
        {
            get { return (Visibility)GetValue(ActivityEditorTopVisibilityProperty); }
            set { SetValue(ActivityEditorTopVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShadingGradient.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActivityEditorTopVisibilityProperty =
            DependencyProperty.Register("ActivityEditorTopVisibility", typeof(Visibility), typeof(BaseDetailsEditor), null);

    }
}
