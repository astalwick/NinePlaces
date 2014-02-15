using System.Windows.Controls;
using NinePlaces.ViewModels.IconViewModels;

namespace NinePlaces
{
    public partial class NotesDetailsEditor : UserControl, IDetailsEditor
    {
        public NotesDetailsEditor(IIconViewModel in_oIconVM)
        {
            // Required to initialize variables
            InitializeComponent();
            DataContext = in_oIconVM;
            SizeChanged += new System.Windows.SizeChangedEventHandler(NotesDetailsEditor_SizeChanged);
        }

        void NotesDetailsEditor_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("SizeChanged: " + LayoutRoot.ActualHeight);
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