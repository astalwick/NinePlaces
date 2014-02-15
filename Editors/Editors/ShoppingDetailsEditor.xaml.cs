using System.Windows.Controls;
using Common.Interfaces;

namespace Editors
{
    public partial class ShoppingDetailsEditor : BaseDetailsEditor, IInterfaceElement
    {
        public ShoppingDetailsEditor(IIconViewModel in_oIconVM)
        {
            // Required to initialize variables
            InitializeComponent();
            DataContext = in_oIconVM;
            IconClass = Common.Interfaces.IconClass.Activity;
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