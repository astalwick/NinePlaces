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
using System.Xml.Linq;
using Common;
using System.Text;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Threading;

namespace NinePlaces.Models.REST
{
    public class RESTAuthentication : ILoginSignupModel
    {

        #region ILoginSignupModel Members

        // event that notifies the outside world of the state of our request
        public event EventHandler RequestStatusChanged;

        // event that notifies the outside world that we've loggedin or logged out.
        public event AuthenticationEventHandler AuthenticationStatusChanged;

        // this static auth accessor allows anyone else in the REST_XML_MODEL
        // assembly to refer to us easily.  (they'll need to do stuff like
        // RESTAuthentication.Auth.AuthToken to get the token to send to the server).
        internal static RESTAuthentication Auth = null;

        // if authentication is successful, the server will give us an authtoken for
        // future requests.  we put that auth token here.
        public string AuthToken { get; internal set; }
        public string UserID { get; internal set; }

        private bool m_bAuthenticated = false;
        public bool Authenticated
        {
            get
            {
                return m_bAuthenticated;
            }
            set
            {
                try
                {
                    if (value == false)     // the outside world can only set auth to false.  it'll get set to true internally.
                    {
                        // we're logging out.  forget who we were.
                        m_bAuthenticated = value;
                        UserName = "";
                        Password = "";
                        EMail = "";
                        AuthToken = "";
                        UserID = "";

                        ForgetAuth();

                        // we've changed.
                        if (AuthenticationStatusChanged != null)
                            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => AuthenticationStatusChanged.Invoke(this, new AuthenticationEventArgs(false, false)));
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
        }

        public string UserName { get; set; }
        public string Password { get; set; }
        public string EMail { get; set; }
        public bool StayLoggedIn { get; set; }
        public string HomeCity { get; set; }
        public string HomeCountry { get; set; }
        public string HomeProvince { get; set; }
        public string HomeTimeZone { get; set; }

        public event AccountCreationEventHandler AccountCreationStatusChanged;
        private DispatcherTimer m_tAuthTimer = new DispatcherTimer();
        private RequestStates m_eRequestState = RequestStates.Idle;
        public RequestStates RequestState 
        { 
            get
            {
                return m_eRequestState;
            }
            set
            {
                if (m_eRequestState != value)
                {
                    m_eRequestState = value;

                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            switch (value)
                            {
                                case RequestStates.Idle:
                                    m_tAuthTimer.Stop();
                                    break;
                                case RequestStates.Requesting:
                                    // we'll give it 15 seconds.
                                    m_tAuthTimer.Stop();
                                    m_tAuthTimer.Interval = new TimeSpan(0, 0, 15);
                                    m_tAuthTimer.Start();
                                    break;
                                case RequestStates.Requesting_Slow:
                                    // we've marked it 'slow', now give it 30 more seconds.
                                    m_tAuthTimer.Stop();
                                    m_tAuthTimer.Interval = new TimeSpan(0, 0, 30);
                                    m_tAuthTimer.Start();
                                    break;
                                case RequestStates.Retrying:
                                    // the retry gets 30 seconds
                                    m_tAuthTimer.Stop();
                                    m_tAuthTimer.Interval = new TimeSpan(0, 0, 30);
                                    m_tAuthTimer.Start();
                                    break;
                                case RequestStates.Failed:
                                    // just stop the timer
                                    m_tAuthTimer.Stop();
                                    break;
                            }

                            if (RequestStatusChanged != null)
                                RequestStatusChanged.Invoke(this, new EventArgs());
                        });
                }
            }

        }
        private HttpWebRequest m_owrCurrentRequest = null;
        
        public RESTAuthentication()
        {
            Auth = this;
            m_tAuthTimer.Tick += new EventHandler(m_tAuthTimer_Tick);
        }

        /// <summary>
        /// Given the previously set credentials, begin the authentication at the server.
        /// </summary>
        /// <returns></returns>
        public bool Authenticate()
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
            {
                // can we authenticate from isostorage?
                string strAuthToTest, strUserName;
                if (RestoreAuth(out strUserName, out strAuthToTest))
                {
                    // whoa, cool, we were able to restore auth from isostorage.
                    // lets double-check that it's ok at the server.
                    m_bAuthenticated = false;
                    Log.WriteLine("Authenticating " + strUserName, LogEventSeverity.Informational);
                    Log.Indent();
                    Log.WriteLine("With AuthToken: " + strAuthToTest, LogEventSeverity.Verbose);

                    UserName = strUserName;

                    //Get the list of available timelines
                    Uri uriAutoAuth = new Uri(Common.Helpers.GetAPIURL() + "Authentication?AuthToken=" + strAuthToTest + "&UserName=" + UserName);
                    m_owrCurrentRequest = (HttpWebRequest)WebRequest.Create(uriAutoAuth);
                    m_owrCurrentRequest.Method = "GET";

                    RequestState = RequestStates.Requesting;
                    IAsyncResult resAutoAuth = m_owrCurrentRequest.BeginGetResponse(new AsyncCallback(AuthCompleted), m_owrCurrentRequest);
                }
                return true;
            }
            Log.WriteLine("Authenticating " + UserName, LogEventSeverity.Informational);
            Log.Indent();
            Log.WriteLine("With Password " + Password, LogEventSeverity.Verbose);

            m_bAuthenticated = false;

            // Execute the auth request.
            Uri uri = new Uri(Common.Helpers.GetAPIURL() + "Authentication?Password=" + Password + "&UserName=" + UserName);
            m_owrCurrentRequest = (HttpWebRequest)WebRequest.Create(uri);
            m_owrCurrentRequest.Method = "GET";

            RequestState = RequestStates.Requesting;
            IAsyncResult result = m_owrCurrentRequest.BeginGetResponse(new AsyncCallback(AuthCompleted), m_owrCurrentRequest);

            return true;
        }

        void m_tAuthTimer_Tick(object sender, EventArgs e)
        {
            switch (RequestState)
            {
                case RequestStates.Idle:
                    break;
                case RequestStates.Requesting:
                    //
                    // notify that our request is taking longer than normal. 
                    //
                    RequestState = RequestStates.Requesting_Slow;
                    break;
                case RequestStates.Requesting_Slow:
                    // abort
                    RequestState = RequestStates.Failed;
                    m_owrCurrentRequest.Abort();
                    break;
                case RequestStates.Retrying:
                    // retry took too long.  give up.
                    RequestState = RequestStates.Failed;
                    m_owrCurrentRequest.Abort();
                    break;
            }
        }

        /// <summary>
        /// Called when an auth request returns from the server
        /// </summary>
        /// <param name="result"></param>
        void AuthCompleted(IAsyncResult result)
        {
            try
            {
                WebRequest r = (WebRequest)result.AsyncState;
                using (HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(result))
                {
                    RequestState = RequestStates.Idle;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {

                        Stream strResp = response.GetResponseStream();
                        XElement xResp = XElement.Load(strResp);

                        Log.WriteLine("Authentication succeeded", LogEventSeverity.Informational);

                        if (xResp.Name == "Authentication")
                        {
                            
                            m_bAuthenticated = true;
                            AuthToken = xResp.Element("AuthToken").Value;
                            UserID = xResp.Element("UserID").Value;
                            UserName = xResp.Element("UserName").Value;
                            EMail = xResp.Element("EMail").Value;

                            if (StayLoggedIn)
                            {
                                RememberAuth();
                            }
                        }
                    }
                #warning What if status code is NOT httpstatuscode.ok
                }
            }
            catch (Exception ex)
            {
                RequestState = RequestStates.Failed;
                Log.Exception(ex);
            }

            Log.Unindent();

            // we went through the authenticaation process and - we're not authenticated.
            // we should clear out the ISO store auth, if it exists.
            if (!m_bAuthenticated)
                ForgetAuth();

            // We're on a thread.  We need to notify out that authentication status has changed,
            // so we invoke.
            if( AuthenticationStatusChanged != null )
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>  AuthenticationStatusChanged.Invoke(this, new AuthenticationEventArgs(m_bAuthenticated, !m_bAuthenticated)));
        }

        /// <summary>
        /// Called to persist an authtoken to file.
        /// </summary>
        private void RememberAuth()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("auth", FileMode.Create, isf))
                {
                    using (StreamWriter sw = new StreamWriter(isfs))
                    {
                        sw.Write(UserName + "," + AuthToken);
                        sw.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Called to restore and authtoken from IsoStorage
        /// </summary>
        /// <param name="out_strUserName"></param>
        /// <param name="out_strAuthToken"></param>
        /// <returns></returns>
        private bool RestoreAuth(out string out_strUserName, out string out_strAuthToken)
        {
            out_strUserName = string.Empty;
            out_strAuthToken = string.Empty;
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!isf.FileExists("auth"))
                    return false ;

                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("auth", FileMode.Open, isf))
                {
                    using (StreamReader sr = new StreamReader(isfs))
                    {
                        string strAuth = sr.ReadLine();
                        string[] arElems = strAuth.Split(',');
                        out_strUserName = arElems[0];
                        out_strAuthToken = arElems[1];
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Called to delete auth from IsoStorage.
        /// </summary>
        private void ForgetAuth()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!isf.FileExists("auth"))
                    return;

                isf.DeleteFile("auth");
            }
        }

        /// <summary>
        /// Called to create a new account using the (previously set) 
        /// account information.
        /// </summary>
        public void CreateAccount()
        {
            Log.WriteLine("Creating user " + UserName, LogEventSeverity.Informational);
            Log.Indent();
            Log.WriteLine("EMail: " + EMail, LogEventSeverity.Verbose);
            Log.WriteLine("Name: " + UserName, LogEventSeverity.Verbose);
            Log.WriteLine("Password: " + Password, LogEventSeverity.Verbose);
            // lets construct up the xml createaccount request.
            XElement xAuth = new XElement( "CreateAccount",
                            new XElement("EMail", EMail),
                            new XElement("Name", UserName), 
                            new XElement( "Password", Password ),
                            new XElement( "HomeCity", HomeCity ),
                            new XElement( "HomeCountry", HomeCountry ),
                            new XElement( "HomeProvince", HomeProvince ),
                            new XElement( "HomeTimeZone", HomeTimeZone )
                            );


            //Get the list of available timelines
            Uri uri = new Uri(Common.Helpers.GetAPIURL() + "CreateAccount");
            m_owrCurrentRequest = (HttpWebRequest)WebRequest.Create(uri);
            //wr.Method = "PUT";

            // this is only necessary if PUT is not allowed at the server!
            m_owrCurrentRequest.Method = "POST";
            m_owrCurrentRequest.ContentType = "application/xml";

            byte[] byteData = UTF8Encoding.UTF8.GetBytes(xAuth.ToString());
            m_owrCurrentRequest.AllowReadStreamBuffering = true;

            RequestState = RequestStates.Requesting;
            IAsyncResult aGetRequestResult = m_owrCurrentRequest.BeginGetRequestStream(x =>
            {
                Stream rs = m_owrCurrentRequest.EndGetRequestStream(x);
                rs.Write(byteData, 0, byteData.Length);
                rs.Close();

                IAsyncResult result = m_owrCurrentRequest.BeginGetResponse(new AsyncCallback(CreateUserCompleted), m_owrCurrentRequest);
            }, null);
        }

        /// <summary>
        /// Callback when the create user request returns.
        /// </summary>
        /// <param name="result"></param>
        void CreateUserCompleted(IAsyncResult result)
        {
            try
            {
                WebRequest r = (WebRequest)result.AsyncState;
                using (HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(result))
                {
                    RequestState = RequestStates.Idle;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Log.WriteLine("Creation Succeeded: " + UserName, LogEventSeverity.Informational);
                        if (AccountCreationStatusChanged != null)
                            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                                {
                                    m_tAuthTimer.Stop(); ;
                                    AccountCreationStatusChanged.Invoke(this, new AccountCreationEventArgs( false ));
                                });

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                RequestState = RequestStates.Failed;
                Log.Exception(ex);
            }

            Log.WriteLine("Creation Failed: " + UserName, LogEventSeverity.Error);
            if (AccountCreationStatusChanged != null)
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => AccountCreationStatusChanged.Invoke(this, new AccountCreationEventArgs(true)));

            Log.Unindent();
        }

        #endregion

        #region IModel Members

        // there is no uniqueid for the auth model.
        // arguably, uniqueid shouldn't be part of imodel, it should be part of some
        // dervied interface (actually, that's not even arguable - it's kinda just fact)
        public int UniqueID
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}
