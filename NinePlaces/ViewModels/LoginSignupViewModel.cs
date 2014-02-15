using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common.Interfaces;
using NinePlaces.Models.REST;
using Common;

namespace NinePlaces.ViewModels
{
    public class LoginSignupViewModel : ILoginViewModel
    {
        private ILoginSignupModel Model = null; 

        public LoginSignupViewModel()
        {
            App.AssemblyLoadManager.LoadXap("REST_XML_Model.xap", new EventHandler(wcDownloadXap_OpenReadCompleted));
        }

        public void InitializeModel()
        {
            if (Model == null)
            {
                Model = new RESTAuthentication();
                Model.AccountCreationStatusChanged += new AccountCreationEventHandler(Model_AccountCreationStatusChanged);
                Model.AuthenticationStatusChanged += new AuthenticationEventHandler(Model_AuthenticationStatusChanged);
                Model.RequestStatusChanged += new EventHandler(Model_RequestStatusChanged);
                Model.Authenticate();
            }
        }

        void Model_RequestStatusChanged(object sender, EventArgs e)
        {
            if (RequestStatusChanged != null)
                RequestStatusChanged.Invoke(this, new EventArgs());
        }

        void wcDownloadXap_OpenReadCompleted(object sender, EventArgs e)
        {
            InitializeModel();
        }

        void Model_AuthenticationStatusChanged(object sender, AuthenticationEventArgs e)
        {
            if (AuthenticationStatusChanged != null)
                AuthenticationStatusChanged.Invoke(this, e);
        }

        void Model_AccountCreationStatusChanged(object sender, AccountCreationEventArgs e)
        {
            if (AccountCreationStatusChanged != null)
                AccountCreationStatusChanged.Invoke(this, e);
        }

        #region ILoginViewModel Members

        public event EventHandler RequestStatusChanged;
        public RequestStates RequestState
        {
            get
            {
                return Model.RequestState;
            }
        }

        public bool StayLoggedIn
        {
            get
            {
                return Model.StayLoggedIn;
            }
            set
            {
                Model.StayLoggedIn = value;
            }
        }

        public bool Authenticated
        {
            get { return Model != null ? Model.Authenticated : false; }
        }

         public string UserName
        {
            get
            {
                return Model.UserName;
            }
            set
            {
                Model.UserName = value;
            }
        }

         ILocationObject m_oLocationObject = new LocationObject();
         public ILocationObject Location
         {
             get
             {
                 return m_oLocationObject;
             }
             set
             {
                 m_oLocationObject = value;
                 Model.HomeCity = m_oLocationObject.CityName;
                 Model.HomeCountry = m_oLocationObject.Country;
                 Model.HomeProvince = m_oLocationObject.Province;
                 Model.HomeTimeZone = m_oLocationObject.TimeZoneID;

             }
         }

        public string Password
        {
            set { Model.Password = value; }
        }

        public void Authenticate()
        {
            Model.Authenticate();
        }

        public void ClearAuthentication()
        {
            Model.Authenticated = false;
        }

        public event AuthenticationEventHandler AuthenticationStatusChanged;
        
        public void CreateAccount()
        {
            Model.CreateAccount();
        }

        public string EMail
        {
            get
            {
                return Model.EMail;
            }
            set
            {
                Model.EMail = value;
            }
        }

        public event AccountCreationEventHandler AccountCreationStatusChanged;

        #endregion

        #region ILoginViewModel Members

        public string AuthToken
        {
            get { return Model.AuthToken; }
        }

        #endregion
    }
}
