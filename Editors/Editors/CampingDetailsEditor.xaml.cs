using System.Windows.Controls;

using Common.Interfaces;

namespace Editors
{
    public partial class CampingDetailsEditor : BaseDetailsEditor, IInterfaceElement
    {
        public CampingDetailsEditor(IIconViewModel in_oIconVM)
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