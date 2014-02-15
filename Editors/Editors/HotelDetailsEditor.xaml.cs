using System.Windows.Controls;
using Common.Interfaces;
using System.Windows;

namespace Editors
{
    public partial class HotelDetailsEditor : BaseDetailsEditor, IInterfaceElement
    {
        public HotelDetailsEditor(IIconViewModel in_oIconVM)
        {
            // Required to initialize variables
            InitializeComponent();
            DataContext = in_oIconVM;
            IconClass = Common.Interfaces.IconClass.Lodging;
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