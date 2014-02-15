using System.Windows.Controls;

using NinePlaces.ViewModels.IconViewModels;

namespace NinePlaces
{
    public partial class PhotosDetailsEditor : UserControl, IDetailsEditor
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