using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace BasicEditors
{
	public partial class QuickPrompt : UserControl
	{
        public string QueryText
        {
            get
            {
                return lblQuery.Text;
            }
            set
            {
                if (lblQuery.Text != value)
                    lblQuery.Text = value;
            }
        }

        private bool m_bResult = false;
        public bool Result
        {
            get
            {
                return m_bResult;
            }
            protected set
            {
                if (m_bResult != value)
                    m_bResult = value;
            }
        }

		public QuickPrompt()
		{
			// Required to initialize variables
			InitializeComponent();
            OK.Click += new RoutedEventHandler(OK_Click);
            Cancel.Click += new RoutedEventHandler(Cancel_Click);
            KeyDown += new KeyEventHandler(QuickPrompt_KeyDown);
            Loaded += new RoutedEventHandler(QuickPrompt_Loaded);
        }

        void QuickPrompt_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
        }

        void QuickPrompt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // we're outta here!
                Result = false;
                if (PromptClosed != null)
                    PromptClosed.Invoke(this, new EventArgs());

                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                // submit!
                Result = true;
                if (PromptClosed != null)
                    PromptClosed.Invoke(this, new EventArgs());

                e.Handled = true;
            }
        }

        void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            if (PromptClosed != null)
                PromptClosed.Invoke(this, new EventArgs());
        }

        void OK_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            if (PromptClosed != null)
                PromptClosed.Invoke(this, new EventArgs());
        }

        public event EventHandler PromptClosed;

    }
}