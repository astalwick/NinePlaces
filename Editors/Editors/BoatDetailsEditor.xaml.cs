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
using BasicEditors;
using Common.Interfaces;

namespace Editors
{
    public partial class BoatDetailsEditor : BaseDetailsEditor, IInterfaceElement
    {
        public BoatDetailsEditor(IIconViewModel in_oIconVM)
        {
            // Required to initialize variables
            InitializeComponent();
            DataContext = in_oIconVM;

            tbDestinationCity.Populating += new PopulatingEventHandler(LocationAutoCompleteBox_Populating);
            tbDestinationCity.LostFocus += new RoutedEventHandler(LocationAutoCompleteBox_LostFocus);
            tbDepartureCity.Populating += new PopulatingEventHandler(LocationAutoCompleteBox_Populating);
            tbDepartureCity.LostFocus += new RoutedEventHandler(LocationAutoCompleteBox_LostFocus);
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
