using System.Windows;
using System.Windows.Controls;
using NinePlaces.ViewModels.IconViewModels;

namespace NinePlaces
{
	public partial class QuickEditControl : UserControl
	{
		public QuickEditControl()
		{
			// Required to initialize variables
			InitializeComponent();
		}
        public QuickEditControl(string in_strLabel)
        {
            // Required to initialize variables
            InitializeComponent();
            tbQuickEditTextBox.Visibility = Visibility.Collapsed;

            lblEditLabel.Text = in_strLabel;
        }

        public QuickEditControl(string in_strLabel, string in_strDefault)
        {
            // Required to initialize variables
            InitializeComponent();
            tbQuickEditTextBox.Text = in_strDefault;
            lblEditLabel.Text = in_strLabel;
        }


        public QuickEditControl(string in_strLabel, IIconViewModel in_oVM, System.Windows.Data.Binding in_oBinding )
        {
            // Required to initialize variables
            InitializeComponent();
            lblEditLabel.Text = in_strLabel;
            DataContext = in_oVM;
            in_oBinding.Mode = System.Windows.Data.BindingMode.TwoWay;
            tbQuickEditTextBox.SetBinding(TextBox.TextProperty, in_oBinding);
        }
	}
}