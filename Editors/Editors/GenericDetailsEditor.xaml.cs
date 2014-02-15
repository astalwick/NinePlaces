using System.Windows.Controls;
using Common.Interfaces;
using System.Windows;
using System.Windows.Media;

namespace Editors
{
    public partial class GenericDetailsEditor : BaseDetailsEditor, IInterfaceElement
    {
        public GenericDetailsEditor(IIconViewModel in_oIconVM)
        {
            // Required to initialize variables
            InitializeComponent();
            DataContext = in_oIconVM;
            Address.SizeChanged += new SizeChangedEventHandler(Address_SizeChanged);

            
            if ((in_oIconVM as IMutableIconProperties) != null)
            {
                in_oIconVM.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(in_oIconVM_PropertyChanged);
                Loaded += new RoutedEventHandler(GenericDetailsEditor_Loaded);
            }

        }

        void GenericDetailsEditor_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBrushes();
        }

        void in_oIconVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentClass")
            {
                UpdateBrushes();
            }
        }

        void Address_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //LayoutRoot.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            //this.Height = LayoutRoot.DesiredSize.Height;
        }

        void UpdateBrushes()
        {
            IMutableIconProperties iMut = DataContext as IMutableIconProperties;
            LinearGradientBrush oIconBrush = Application.Current.Resources["GenericIconGradient"] as LinearGradientBrush;
            LinearGradientBrush oIconEditorBrush = Application.Current.Resources["GenericIconEditorGradient"] as LinearGradientBrush;
            LinearGradientBrush oIconTabBrush = Application.Current.Resources["GenericEditorTabItemGradient"] as LinearGradientBrush;

            switch (iMut.CurrentClass)
            {
                case IconClass.Activity:
                    oIconBrush = Application.Current.Resources["ActivityIconGradient"] as LinearGradientBrush;
                    oIconEditorBrush = Application.Current.Resources["ActivityEditorIconGradient"] as LinearGradientBrush;
                    oIconTabBrush = Application.Current.Resources["ActivityEditorTabItemGradient"] as LinearGradientBrush;
                    break;
                case IconClass.Transportation:
                    oIconBrush = Application.Current.Resources["TravelIconGradient"] as LinearGradientBrush;
                    oIconEditorBrush = Application.Current.Resources["TravelEditorIconGradient"] as LinearGradientBrush;
                    oIconTabBrush = Application.Current.Resources["TravelEditorTabItemGradient"] as LinearGradientBrush;
                    break;
                case IconClass.Lodging:
                    oIconBrush = Application.Current.Resources["LodgingIconGradient"] as LinearGradientBrush;
                    oIconEditorBrush = Application.Current.Resources["LodgingEditorIconGradient"] as LinearGradientBrush;
                    oIconTabBrush = Application.Current.Resources["TravelEditorTabItemGradient"] as LinearGradientBrush;
                    break;
            }

            IconClass = iMut.CurrentClass;

            BackgroundGradient = oIconBrush;
            ShadingGradient = oIconEditorBrush;

            foreach (object Child in TabControl.Items)
            {
                if (Child is MyTabItem)
                {
                    (Child as MyTabItem).Background = oIconTabBrush;
                }
            }
        }

        public Control Control
        {
            get
            {
                return this as Control;
            }
        }
    }
}