using System.Windows.Controls;
using Common.Interfaces;
using Common;
using System.Windows;

namespace Editors
{
    public partial class FlightDetailsEditor : BaseDetailsEditor, IInterfaceElement
	{
        public FlightDetailsEditor(IIconViewModel in_oVM)
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