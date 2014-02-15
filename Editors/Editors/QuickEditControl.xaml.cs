using System.Windows;
using System.Windows.Controls;
using Common.Interfaces;
using System.Windows.Data;

namespace BasicEditors
{
	public partial class QuickEditControl : UserControl
	{
		public QuickEditControl()
		{
			// Required to initialize variables
			InitializeComponent();
		}
        public QuickEditControl(Binding in_oLabelBinding)
        {
            // Required to initialize variables
            InitializeComponent();
            tbQuickEditTextBox.Visibility = Visibility.Collapsed;

            in_oLabelBinding.Mode = BindingMode.OneWay;
            lblEditLabel.SetBinding(TextBlock.TextProperty, in_oLabelBinding);
        }

        public QuickEditControl(Binding in_oLabelBinding, string in_strDefault)
        {
            // Required to initialize variables
            InitializeComponent();
            tbQuickEditTextBox.Text = in_strDefault;
            in_oLabelBinding.Mode = BindingMode.OneWay;
            lblEditLabel.SetBinding(TextBlock.TextProperty, in_oLabelBinding);
        }

        public QuickEditControl(Binding in_oLabelBinding, IIconViewModel in_oVM, Binding in_oPropertyBinding)
        {
            // Required to initialize variables
            InitializeComponent();
            in_oLabelBinding.Mode = BindingMode.OneWay;
            lblEditLabel.SetBinding(TextBlock.TextProperty, in_oLabelBinding);

            // set our datacontext,
            DataContext = in_oVM;
            in_oPropertyBinding.Mode = BindingMode.TwoWay;
            tbQuickEditTextBox.SetBinding(TextBox.TextProperty, in_oPropertyBinding);
        }
	}
}