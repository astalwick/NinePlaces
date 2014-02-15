using System.Windows.Controls;

using Common.Interfaces;

namespace BasicEditors
{
    public partial class PhotosDetailsEditor : UserControl, IInterfaceElement
    {
        public PhotosDetailsEditor(IIconViewModel in_oIconVM)
        {
            // Required to initialize variables
            InitializeComponent();
            DataContext = in_oIconVM;
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