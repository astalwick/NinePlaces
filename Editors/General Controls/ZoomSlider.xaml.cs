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

namespace Editors.General_Controls
{
    public partial class ZoomSlider : UserControl
    {
        public event RoutedPropertyChangedEventHandler<double> SliderValueChanged;
        public ZoomSlider()
        {
            InitializeComponent();
            Slider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
        }

        void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderValueChanged != null)
                SliderValueChanged.Invoke(sender, e);
        }

        public double Value
        {
            get
            {
                return Slider.Value;
            }
            set
            {
                if (Slider.Value != value)
                    Slider.Value = value;
            }
        }

    }
}
