using System.Windows;
using System.Windows.Controls;
using Common.Interfaces;

namespace Editors
{
    public partial class BusDetailsEditor : BaseDetailsEditor, IInterfaceElement
	{
        public BusDetailsEditor(IIconViewModel in_oVM)
		{
			// Required to initialize variables
			InitializeComponent();

            DataContext = in_oVM;
            tbDepartureCity.Populating += new PopulatingEventHandler(LocationAutoCompleteBox_Populating);
            tbDepartureCity.LostFocus += new RoutedEventHandler(LocationAutoCompleteBox_LostFocus);
            tbDestinationCity.Populating += new PopulatingEventHandler(LocationAutoCompleteBox_Populating);
            tbDestinationCity.LostFocus += new RoutedEventHandler(LocationAutoCompleteBox_LostFocus);
            IconClass = Common.Interfaces.IconClass.Transportation;
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