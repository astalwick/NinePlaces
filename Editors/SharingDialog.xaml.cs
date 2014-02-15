using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common;
using Common.Interfaces;

namespace Editors
{
	public partial class SharingDialog : UserControl
	{
        private IVacationViewModel m_oVacation = null;
        public IVacationViewModel ActiveVacation
        {
            get
            {
                return m_oVacation;
            }

            private set
            {
                m_oVacation = value;
                DataContext = m_oVacation;
            }
        }

        public string URLPrefix
        {
            get
            {
                return "http://www.nineplaces.com/" + UserName + "/";
            }

        }
        public string UserName { get; private set; }
        public string URL
        {
            get
            {
                return URLPrefix + ActiveVacation.VacationShortName;
            }
        }


        protected void VacationShortName_KeyDown(object sender, KeyEventArgs e)
        {
            //Letters (a-z and A-Z), numbers (0-9) only.
            if ((e.Key < Key.A || e.Key > Key.Z) &&
                (e.Key < Key.D0 || e.Key > Key.D9 || Keyboard.Modifiers == ModifierKeys.Shift) &&
                 e.Key != Key.Tab && e.Key != Key.Back && e.Key != Key.Delete && e.Key != Key.Left && e.Key != Key.Right &&
                 e.Key != Key.Subtract)
            {
                e.Handled = true;
                return;
            }
        }


        public event EventHandler Close;
		public SharingDialog(string in_strUserName, IVacationViewModel in_oActiveVacation)
		{
            ActiveVacation = in_oActiveVacation;
            UserName = in_strUserName;
			// Required to initialize variables
			InitializeComponent();
            VacationShortName.KeyDown+=new KeyEventHandler(VacationShortName_KeyDown);
            KeyDown += new KeyEventHandler(SharingDialog_KeyDown);
            Loaded += new RoutedEventHandler(SharingDialog_Loaded);
            LostFocus += new RoutedEventHandler(SharingDialog_LostFocus);
            CopyToClipboardButton.Click += new RoutedEventHandler(CopyToClipboardButton_Click);
            CloseButton.Click += new RoutedEventHandler(CloseButton_Click);
            DataContext = this;
		}

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Close != null)
            {
                Log.TrackPageView("/NinePlaces/App"); 
                Close.Invoke(this, new EventArgs());
            }
        }

        void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            Log.TrackPageView("/NinePlaces/Sharing/CopyToClipboard"); 
            Clipboard.SetText(URL);
        }

        void SharingDialog_LostFocus(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = (FrameworkElement) FocusManager.GetFocusedElement();
            if (Helpers.IsAncestorControl(this, fe))
                return; // this is part of our hierarchy, nothing to worry about.  no need to close yet.

            if (Close != null)
            {
                Log.TrackPageView("/NinePlaces/App"); 
                Close.Invoke(this, new EventArgs());
            }
        }

        void SharingDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Log.TrackPageView("/NinePlaces/Sharing"); 
            this.Focus();
        }

        void SharingDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && Close != null)
            {
                Log.TrackPageView("/NinePlaces/App"); 
                Close.Invoke(this, new EventArgs());
            }
        }
	}
}