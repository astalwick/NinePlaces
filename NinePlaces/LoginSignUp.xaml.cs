using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common.Interfaces;
using Common;
using NinePlaces.ViewModels;
using System.Net;
using System.Collections.Generic;
using System.Xml;

namespace NinePlaces
{
    public partial class LoginSignUp : UserControl
	{
        bool m_bProcessingAuthChange = false;
        ILoginViewModel m_oVM = null;
        public ILoginViewModel VM
        {
            get
            {
                return m_oVM;
            }
        }

        public event AuthenticationEventHandler AuthenticationStatusChanged;

		public LoginSignUp()
		{
			// Required to initialize variables
			InitializeComponent();

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                m_oVM = new LoginSignupViewModel();
                m_oVM.AuthenticationStatusChanged += new AuthenticationEventHandler(m_oVM_AuthenticationStatusChanged);
                m_oVM.AccountCreationStatusChanged += new AccountCreationEventHandler(m_oVM_AccountCreationStatusChanged);
                m_oVM.RequestStatusChanged += new EventHandler(m_oVM_RequestStatusChanged);
            }

            SignupHomeCity.Populating += new PopulatingEventHandler(SignupHomeCity_Populating);
            SignupHomeCity.LostFocus += new RoutedEventHandler(SignupHomeCity_LostFocus);

            SignupName.KeyDown += new KeyEventHandler(SignupName_KeyDown);
            SignupEmail.KeyDown += new KeyEventHandler(Signup_KeyDown);
            SignupPassword.KeyDown += new KeyEventHandler(Signup_KeyDown);
            SignupValidatePassword.KeyDown += new KeyEventHandler(Signup_KeyDown);

            Password.KeyDown += new KeyEventHandler(Login_KeyDown);
            LoginName.KeyDown += new KeyEventHandler(Login_KeyDown);
            LoginButton.Click += new RoutedEventHandler(LoginButton_Click);
            SignupButton.Click += new RoutedEventHandler(SignupButton_Click);
            LogoutButton.Click += new RoutedEventHandler(LogoutButton_Click);
            StayLoggedInCheckBox.Checked += new RoutedEventHandler(StayLoggedInCheckBox_Checked);
            MouseLeftButtonDown += new MouseButtonEventHandler(LoginSignUp_MouseLeftButtonDown);

            App.SelectionMgr.SelectionCleared += new EventHandler(SelectionMgr_SelectionCleared);
            App.LoginControl = this;
		}



        void SignupHomeCity_LostFocus(object sender, RoutedEventArgs e)
        {
            List<LocationObject> aPossibleItems = (List<LocationObject>)SignupHomeCity.ItemsSource;
            if (SignupHomeCity.SelectedItem == null && aPossibleItems != null && aPossibleItems.Count > 0)
            {
                SignupHomeCity.SelectedItem = aPossibleItems[0];
            }
        }

        public virtual void SignupHomeCity_Populating(object sender, PopulatingEventArgs e)
        {
            e.Cancel = true;
            CallToWebService(sender as AutoCompleteBox);
        }

        public virtual void CallToWebService(AutoCompleteBox in_acb)
        {
            WebClient wc = new WebClient();

            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(LocationQueryComplete);
            wc.DownloadStringAsync(new Uri(Common.Helpers.GetAPIURL() + "Cities?q=" + in_acb.SearchText), in_acb);
        }

        public virtual void LocationQueryComplete(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                return;
            }

            AutoCompleteBox acb = e.UserState as AutoCompleteBox;

            List<LocationObject> aLocations = new List<LocationObject>();
            LocationObject l = new LocationObject();
            try
            {
                bool bCity, bCountry, bProvince, bTimezone;
                bCity = bCountry = bProvince = bTimezone = false;

                using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(e.Result)))
                {
                    // iterate through the elements...
                    while (reader.IsStartElement() || reader.Read())
                    {
                        // we only care about start elements (open tags).
                        if (reader.IsStartElement())
                        {
                            if (reader.LocalName == "UnicodeName")
                            {
                                // aha, found the DST offset.
                                bCity = true;
                                l.CityName = reader.ReadElementContentAsString();
                            }
                            else if (reader.LocalName == "CountryCode")
                            {
                                // aha, found the standard tiem offset.
                                bCountry = true;
                                l.Country= reader.ReadElementContentAsString();
                            }
                            else if (reader.LocalName == "AdminRegion")
                            {
                                // aha, found timezone identifier.  
                                bProvince = true;
                                l.Province = reader.ReadElementContentAsString();
                            }
                            else if (reader.LocalName == "Identifier")
                            {
                                // aha, found timezone identifier.  
                                bTimezone = true;
                                l.TimeZoneID = reader.ReadElementContentAsString();
                            }
                            else
                            {
                                if (!reader.Read())
                                    break;
                            }
                        }

                        if (bCity && bCountry && bProvince && bTimezone)
                        {
                            bCity = bCountry = bProvince = bTimezone = false;
                            aLocations.Add(l);
                            l = new LocationObject();
                        }
                    }
                }

                if (aLocations.Count == 0)
                {
                    aLocations.Add(new LocationObject() { CityName = acb.SearchText });
                }

                acb.ItemsSource = aLocations;
                acb.PopulateComplete();
            }
            catch { }
        }


        ILocationObject m_oLocationObject = new LocationObject();
        public ILocationObject Location
        {
            get
            {
                return m_oLocationObject;
            }
            protected set
            {
                m_oLocationObject = value;
            }
        }

        void m_oVM_RequestStatusChanged(object sender, EventArgs e)
        {
            switch( m_oVM.RequestState )
            {
                case RequestStates.Idle:
                    break;
                case RequestStates.Requesting:
                    LoginFailedText.Visibility = Visibility.Collapsed;
                    break;
                case RequestStates.Requesting_Slow:
                    Log.WriteLine("Model returned 'retrying' notification", LogEventSeverity.Warning);
                    LoginFailedText.Text = "Please be patient, your request is taking longer than normal...";
                    LoginFailedText.Visibility = Visibility.Visible;
                    break;
                case RequestStates.Retrying:
                    Log.WriteLine("Model returned 'retrying' notification", LogEventSeverity.Warning);
                    LoginFailedText.Text = "NinePlaces is having difficulty contacting the server.  Retrying...";
                    LoginFailedText.Visibility = Visibility.Visible;
                    break;
                case RequestStates.Failed :
                    Log.WriteLine("Model returned 'requestfailed' notification", LogEventSeverity.Warning);
                    LoginFailedText.Text = "NinePlaces was unable to process the request.  Please try again.";
                    LoginFailedText.Visibility = Visibility.Visible;
                    break;
            }
        }

        void StayLoggedInCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (m_bProcessingAuthChange)
                return;

            m_oVM.StayLoggedIn = StayLoggedInCheckBox.IsChecked == true;
        }

        void Login_KeyDown(object sender, KeyEventArgs e)
        {
            if (m_bProcessingAuthChange)
                return;

            if (e.Key == Key.Escape)
            {
                LoginFailedText.Visibility = Visibility.Collapsed;
                // we're outta here!
                CancelLogin();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && sender == Password)
            {
                LoginFailedText.Visibility = Visibility.Collapsed;
                DoLogin();
                e.Handled = true;
            }

        }

        void SignupName_KeyDown(object sender, KeyEventArgs e)
        {
            //Letters (a-z and A-Z), numbers (0-9) only.
            if ((e.Key < Key.A || e.Key > Key.Z) &&
                (e.Key < Key.D0 || e.Key > Key.D9 || Keyboard.Modifiers == ModifierKeys.Shift) &&
                 e.Key != Key.Tab && e.Key != Key.Back && e.Key != Key.Delete && e.Key != Key.Left && e.Key != Key.Right)
            {
                e.Handled = true;
                return;
            }

            Signup_KeyDown(sender, e);
        }

        void Signup_KeyDown(object sender, KeyEventArgs e)
        {
            if (m_bProcessingAuthChange)
                return;

            if (e.Key == Key.Escape)
            {
                LoginFailedText.Visibility = Visibility.Collapsed;
                // we're outta here!
                CancelSignup();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && sender == SignupPassword)
            {
                LoginFailedText.Visibility = Visibility.Collapsed;
                // submit!
                DoSignup();
                e.Handled = true;
            }
        }

        void LoginSignUp_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        void SelectionMgr_SelectionCleared(object sender, EventArgs e)
        {
            if (m_bProcessingAuthChange)
                return;

            LoginFailedText.Visibility = Visibility.Collapsed;

            FocusContainer.Focus();
            if (!m_oVM.Authenticated)
            {
                VisualStateManager.GoToState(this, "NotLoggedIn", false);
                VisualStateManager.GoToState(this, "NotLoggedInAnim", true);
            }
        }

        void CancelLogin()
        {
            if (m_bProcessingAuthChange)
                return;

            Log.TrackPageView("/NinePlaces/App"); // back to the app.

            LoginFailedText.Visibility = Visibility.Collapsed;
            LoginName.Text = "";
            Password.Text = "";
            VisualStateManager.GoToState(this, "NotLoggedIn", false);
            VisualStateManager.GoToState(this, "NotLoggedInAnim", true);
        }

        void CancelSignup()
        {
            if (m_bProcessingAuthChange)
                return;

            Log.TrackPageView("/NinePlaces/App"); // back to the app.

            LoginFailedText.Visibility = Visibility.Collapsed;
            SignupPassword.Text = "";
            SignupValidatePassword.Text = "";
            SignupEmail.Text = "";
            SignupName.Text = "";
            VisualStateManager.GoToState(this, "NotLoggedIn", false);
            VisualStateManager.GoToState(this, "NotLoggedInAnim", true);
        }

        void DoSignup()
        {
            try
            {
                App.AssemblyLoadManager.Requires("REST_XML_Model.xap");

                if (m_bProcessingAuthChange)
                    return;

                if (!ValidateUserName(SignupName.Text))
                {
                    LoginFailedText.Text = App.Resource.GetString("InvalidUserName");// "Invalid Email Address...";
                    LoginFailedText.Visibility = Visibility.Visible;
                    Log.WriteLine("Invalid UserName: " + SignupName.Text, LogEventSeverity.Warning);
                    return;
                }

                if (!ValidateHomeCity(SignupHomeCity.Text))
                {
                    LoginFailedText.Text = App.Resource.GetString("InvalidHomeCity");// "Invalid Email Address...";
                    LoginFailedText.Visibility = Visibility.Visible;
                    Log.WriteLine("Invalid Home City: " + SignupHomeCity.Text, LogEventSeverity.Warning);
                    return;
                }

                if (!ValidateEmail(SignupEmail.Text))
                {
                    LoginFailedText.Text = App.Resource.GetString("InvalidEmailAddress");// "Invalid Email Address...";
                    LoginFailedText.Visibility = Visibility.Visible;
                    Log.WriteLine("Invalid Email Address: " + SignupEmail.Text, LogEventSeverity.Warning);
                    return;
                }
                if (!ValidatePassword(SignupPassword.PasswordText))
                {
                    LoginFailedText.Text = App.Resource.GetString("PasswordTooShort");// "Password too short...";
                    LoginFailedText.Visibility = Visibility.Visible;
                    Log.WriteLine("Password too short: " + SignupPassword.PasswordText.Length, LogEventSeverity.Warning);
                    return;
                }
                if (SignupPassword.PasswordText != SignupValidatePassword.PasswordText)
                {
                    LoginFailedText.Text = App.Resource.GetString("PasswordReentryMismatch");// "Passwords did not match...";
                    LoginFailedText.Visibility = Visibility.Visible;
                    Log.WriteLine("Passwords did not match. (" + SignupPassword.PasswordText.Length + ", " + SignupValidatePassword.Text.Length + ")", LogEventSeverity.Warning);
                    return;
                }

                Log.TrackPageView("/NinePlaces/LoginSignup/DoSignup"); // back to the app.

                Log.WriteLine("DoSignup called - new user to be created: " + m_oVM.UserName, LogEventSeverity.Informational);

                EnableControls(false);
                m_bProcessingAuthChange = true;
                m_oVM.UserName = SignupName.Text;
                m_oVM.EMail = SignupEmail.Text;
                m_oVM.Password = SignupPassword.PasswordText;
                m_oVM.Location = SignupHomeCity.SelectedItem as LocationObject;

                m_oVM.CreateAccount();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Log.WriteLine("Error in Disignup");

                VisualStateManager.GoToState(this, "NotLoggedIn", false);
                VisualStateManager.GoToState(this, "NotLoggedInAnim", true);

                LoginFailedText.Text = "Error occurred on signup"; //App.Resource.GetString("PasswordReentryMismatch");// "Passwords did not match...";
                LoginFailedText.Visibility = Visibility.Visible;
            }
        }

        private void EnableControls(bool in_bEnabled)
        {
            LoginName.IsEnabled = in_bEnabled;
            SignupEmail.IsEnabled = in_bEnabled;
            SignupName.IsEnabled = in_bEnabled;
            SignupButton.IsEnabled = in_bEnabled;
            SignupPassword.IsEnabled = in_bEnabled;
            SignupValidatePassword.IsEnabled = in_bEnabled;
            Password.IsEnabled = in_bEnabled;
            LoginButton.IsEnabled = in_bEnabled;
            LogoutButton.IsEnabled = in_bEnabled;
        }

        void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_bProcessingAuthChange)
                return;

            LoginFailedText.Visibility = Visibility.Collapsed;
            if (SignupEmail.Visibility == Visibility.Visible)
            {
                DoSignup();
            }
            else
            {
                Log.TrackPageView("/NinePlaces/LoginSignup/ShowSignupForm"); // back to the app.
                VisualStateManager.GoToState(this, "Signup", false);
                VisualStateManager.GoToState(this, "SignupAnim", true);

                if (!string.IsNullOrEmpty(LoginName.Text))
                {
                    SignupName.Text = SignupName.Text;
                    Password.Focus();
                }
                else
                {
                    SignupName.Focus();
                }
            }
        }

        void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_bProcessingAuthChange)
                return;

            Log.TrackPageView("/NinePlaces/LoginSignup/DoLogout"); // back to the app.

            Log.WriteLine("Logout - " + m_oVM.UserName, LogEventSeverity.Informational);
            EnableControls(false);
            m_bProcessingAuthChange = true;
            LoginFailedText.Visibility = Visibility.Collapsed;
            if (m_oVM.Authenticated)
                m_oVM.ClearAuthentication();
        }

        void m_oVM_AuthenticationStatusChanged(object sender, AuthenticationEventArgs e)
        {           
            
            EnableControls(true);
            m_bProcessingAuthChange = false;
            SignupPassword.Text = "";
            SignupValidatePassword.Text = "";
            SignupEmail.Text = "";
            SignupName.Text = "";

            if (e.Authenticated)
            {
                Log.TrackPageView("/NinePlaces/LoginSignup/LoginSuccess"); // back to the app.
                Log.WriteLine("Server authenticated " + m_oVM.UserName, LogEventSeverity.Informational);
                VisualStateManager.GoToState(this, "LoggedIn", false);
                VisualStateManager.GoToState(this, "LoggedInAnim", true);
                LoggedInEmail.Text = m_oVM.UserName;
            }
            else
            {

                LoginName.Text = string.Empty;
                Password.Text = string.Empty;
                LoginName.ChangeVisualState();
                Password.ChangeVisualState();

                VisualStateManager.GoToState(this, "NotLoggedIn", false);
                VisualStateManager.GoToState(this, "NotLoggedInAnim", true);
                LoggedInEmail.Text = "";

                if (e.Failed && m_oVM.RequestState == RequestStates.Failed)
                {
                    Log.TrackPageView("/NinePlaces/LoginSignup/LoginFailed"); // back to the app.
                    Log.WriteLine("Server returned an error while authenticating " + m_oVM.UserName, LogEventSeverity.Warning);
                    LoginFailedText.Text = App.Resource.GetString("IncorrectEmailPass");
                    LoginFailedText.Visibility = Visibility.Visible;
                }

                Log.TrackPageView("/NinePlaces/App"); // back to the app.
            }

            if (!e.Failed)
                LoginFailedText.Visibility = Visibility.Collapsed;

            if(AuthenticationStatusChanged != null )
                AuthenticationStatusChanged.Invoke(this, e);
        }

        void DoLogin()
        {
            try
            {
                App.AssemblyLoadManager.Requires("REST_XML_Model.xap");

                if (m_bProcessingAuthChange || string.IsNullOrEmpty(LoginName.Text) || string.IsNullOrEmpty(Password.PasswordText))
                    return;

                Log.TrackPageView("/NinePlaces/LoginSignup/DoLogin"); // back to the app.

                EnableControls(false);
                m_bProcessingAuthChange = true;
                LoginFailedText.Visibility = Visibility.Collapsed;
                m_oVM.UserName = LoginName.Text;
                m_oVM.Password = Password.PasswordText;

                Log.WriteLine("Authenticating " + m_oVM.UserName, LogEventSeverity.Informational);
                m_oVM.Authenticate();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Log.WriteLine("Error in DoLogin");

                VisualStateManager.GoToState(this, "NotLoggedIn", false);
                VisualStateManager.GoToState(this, "NotLoggedInAnim", true);

                LoginFailedText.Text = "Error occurred on signup"; //App.Resource.GetString("PasswordReentryMismatch");// "Passwords did not match...";
                LoginFailedText.Visibility = Visibility.Visible;
            }
        }

        void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoginName.Visibility == Visibility.Collapsed)
            {
                VisualStateManager.GoToState(this, "Signin", false);
                VisualStateManager.GoToState(this, "SigninAnim", true);
            }
            else
            {
                if (m_bProcessingAuthChange)
                    return;

                LoginFailedText.Visibility = Visibility.Collapsed;
                DoLogin();
            }
        }

        void m_oVM_AccountCreationStatusChanged(object sender, AccountCreationEventArgs e)
        {

            if (e.Failed)
            {
                Log.TrackPageView("/NinePlaces/LoginSignup/SignupFailed");
            }
            else
            {
                Log.TrackPageView("/NinePlaces/LoginSignup/SignupSuccess");
            }

            // we'll authenticate whether we succeed or fail.  it gets us to a clean state easily.
            m_oVM.Authenticate();
        }

        private bool ValidateUserName(string in_strUserName)
        {
            return in_strUserName.Length > 2 && Common.Helpers.IsURLFriendly(in_strUserName) && in_strUserName.Length < 128;
        }

        private bool ValidateEmail(string in_strEmail)
        {
            return in_strEmail.Length > 3 && (in_strEmail.IndexOf(".") > 2) && (in_strEmail.IndexOf("@") > 0) && Common.Helpers.IsURLFriendly(in_strEmail) && in_strEmail.Length < 256;
        }

        private bool ValidatePassword(string in_strPassword)
        {
            return in_strPassword.Length > 5;
        }

        private bool ValidateHomeCity(string in_strHomeCity)
        {
            return in_strHomeCity.Length> 0 && in_strHomeCity.Length < 128;
        }
     }
}