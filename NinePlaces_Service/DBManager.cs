using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Enyim.Caching;
using System.Net;
using Enyim.Caching.Configuration;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Enyim.Caching.Memcached;

namespace NinePlaces_Service
{
    public interface ICacheable
    {
        void ToCache();
        bool FromCache(string in_strId);
    }


    [Serializable]
    public class AuthenticatedSession : ISendToSimpleDB, ICacheable
    {
        public string UserID { get; set; }
        public string AuthToken { get; set; }
        public DateTime Expiry { get; set; }
        public string UserName { get; set; }
        public string EMail { get; set; }

        #region ISendToSimpleDB Members

        public string ItemName
        {
            get { return AuthToken; }
        }

        public string Domain
        {
            get { return "NPSessions"; }
        }

        public ReplaceableItem ToSimpleDBRequests()
        {
            ReplaceableItem oToRet = new ReplaceableItem();
            oToRet.ItemName = ItemName;
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "AuthToken", AuthToken, true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "UserID", UserID, true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "UserName", UserName.ToLower(), true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "EMail", EMail.ToLower(), true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "Expiry", Expiry.ToString("yyyy-MM-ddTHH:mm:ssZ"), true);
            return oToRet;
        }

        public bool SentToSimpleDB
        {
            get;
            set;
        }
        public bool QuietLogs
        {
            get { return false; }
        }

        #endregion

        #region ICacheable Members

        public void ToCache()
        {
            DBManager.Cache.Store(StoreMode.Set, "auth_" + this.AuthToken, this, this.Expiry);
        }

        public bool FromCache(string in_strId)
        {
            AuthenticatedSession s = DBManager.Cache.Get<AuthenticatedSession>("auth_" + in_strId);
            if (s != null)
            {
                UserID = s.UserID;
                AuthToken = s.AuthToken;
                Expiry = s.Expiry;
                UserName = s.UserName;
                EMail = s.EMail;
                return true;
            }
            return false;
        }

        #endregion
    }

    [Serializable]
    public class DBUser : ISendToSimpleDB, ICacheable
    {
        public string EMail { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }
        public string UserID { get; set; }
        public string HomeCity { get; set; }
        public string HomeCountry { get; set; }
        public string HomeProvince { get; set; }
        public string HomeTimeZone { get; set; }

        public bool FromCache(string in_strID)
        {
            DBUser d = DBManager.Cache.Get<DBUser>("user_" + in_strID);

            if (d == null)
            {
                // maybe it's a username?
                string strUserID = DBManager.Cache.Get<string>("usernametoid_" + in_strID);
                if( string.IsNullOrEmpty( strUserID ) )
                    // maybe it's an email?
                    strUserID = DBManager.Cache.Get<string>("useremailtoid_" + in_strID);

                if( string.IsNullOrEmpty( strUserID ) )
                    return false;       // nope, couldn't get it.

                d = DBManager.Cache.Get<DBUser>("user_" + strUserID);
            }

            if (d == null)
                return false;   // no deal.

            EMail = d.EMail;
            UserName = d.UserName;
            Password = d.Password;
            UserID = d.UserID;
            HomeCountry = d.HomeCountry;
            HomeCity = d.HomeCity;
            HomeProvince = d.HomeProvince;
            HomeTimeZone = d.HomeTimeZone;

            return true;
        }

        public void ToCache()
        {
            DBManager.Cache.Store(StoreMode.Set, "user_" + UserID, this);
            DBManager.Cache.Store(StoreMode.Set, "usernametoid_" + UserName.ToLower(), UserID);
            DBManager.Cache.Store(StoreMode.Set, "useremailtoid_" + EMail.ToLower(), UserID);
        }

        public string ItemName
        {
            get { return UserID; }
        }

        public string Domain
        {
            get { return "NPUsers"; }
        }

        public ReplaceableItem ToSimpleDBRequests()
        {
            ReplaceableItem oToRet = new ReplaceableItem();
            oToRet.ItemName = ItemName;
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "UserName", UserName.ToLower(), true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "EMail", EMail.ToLower(), true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "Password", Password, true);

            SimpleDBHelpers.AddAttributeValue(ref oToRet, "HomeCity", HomeCity, true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "HomeCountry", HomeCountry, true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "HomeProvince", HomeProvince, true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "HomeTimeZone", HomeTimeZone, true);
            return oToRet;
        }

        public bool SentToSimpleDB
        {
            get;
            set;
        }

        public bool QuietLogs
        {
            get { return false; }
        }

    }

    public class DBManager
    {
        public static AmazonSimpleDBClient SimpleDBService = new AmazonSimpleDBClient("", "");
        private static List<IPEndPoint> g_arServers = new List<IPEndPoint>();
        public static void AddServer(IPEndPoint in_oServer)
        {
            if (!g_arServers.Contains(in_oServer))
            {
                lock (g_arServers)
                {
                    // double check.
                    if (!g_arServers.Contains(in_oServer))
                    {
                        // add the new server.
                        g_arServers.Add(in_oServer);
                        // we need to destroy g_oMemcachedObj, 
                        // so that it resets with the  new config.
                        if (g_oMemcachedObj != null)
                        {
                            MemcachedClient mc = g_oMemcachedObj;
                            g_oMemcachedObj = null;
                            mc.Dispose();
                        }
                    }
                }
            }
        }

        private static MemcachedClient g_oMemcachedObj = null;
        internal static MemcachedClient Cache
        {
            get
            {
                if (g_oMemcachedObj == null)
                {
                    try
                    {
                        lock (g_arServers)
                        {
                            Log.Debug("CREATED MEMCACHED OBJECT");
                            MemcachedClientConfiguration config = new MemcachedClientConfiguration();

                            config.Protocol = MemcachedProtocol.Text;
                            foreach (IPEndPoint o in g_arServers)
                            {
                                Log.Debug("\tIP: " + o.Address.ToString());
                                Log.Debug("\tPORT: " + o.Port);
                                config.Servers.Add(o);
                            }

                            g_oMemcachedObj = new MemcachedClient(config);

                            if (g_oMemcachedObj.Store(StoreMode.Set, "test", "test"))
                                Log.Debug("\tSUCCESSFUL TEST WRITE");

                        }
                    }
                    catch (Exception)
                    {
                        g_oMemcachedObj = null;
                    }
                }

                return g_oMemcachedObj;
            }
        }

        public void DestroyAuth(string in_strAuthToken)
        {
            Cache.Remove("auth_" + in_strAuthToken);
            RetryUtility.RetryAction(3, 1000, () =>
            {
                DeleteAttributesRequest dr = new DeleteAttributesRequest();
                dr.DomainName = "NPSessions";
                dr.ItemName = in_strAuthToken;

                DeleteAttributesResponse drResp = SimpleDBService.DeleteAttributes(dr);
                Log.Verbose("DB DELETEATTRIBUTES");
            });
        }

        public AuthenticatedSession LookupAuthSession(string in_strAuthToken)
        {

            //
            // check some kind of a cache here!
            //
            AuthenticatedSession aNewAuth = new AuthenticatedSession();
            try
            {
                if (!aNewAuth.FromCache(in_strAuthToken))
                {
                    RetryUtility.RetryAction(3, 1000, () =>
                    {
                        Log.Verbose("LookupAuthSession could not find cached object " + "auth_" + in_strAuthToken + " --- DB required");
                        string selectExpression = "Select * From NPSessions where AuthToken = '" + in_strAuthToken + "'";
                        SelectRequest selectRequestAction = new SelectRequest().WithSelectExpression(selectExpression);
                        SelectResponse selectResponse = SimpleDBService.Select(selectRequestAction);
                        Log.Warning("DB SELECT");

                        if (!selectResponse.IsSetSelectResult())
                            throw new Exception();

                        SelectResult selectResult = selectResponse.SelectResult;
                        if (selectResult.Item.Count != 1)
                            throw new Exception();

                        aNewAuth = new AuthenticatedSession();
                        foreach (Amazon.SimpleDB.Model.Attribute a in selectResult.Item[0].Attribute)
                        {
                            if (a.Name == "UserID")
                                aNewAuth.UserID = a.Value;
                            else if (a.Name == "Expiry")
                                aNewAuth.Expiry = DateTime.Parse(a.Value);
                            else if (a.Name == "UserName")
                                aNewAuth.UserName = a.Value;
                            else if (a.Name == "EMail")
                                aNewAuth.EMail = a.Value;
                        }
                        aNewAuth.AuthToken = in_strAuthToken;
                    });
                }
            }
            catch (Exception)
            {
                // do nothing.  we've burned our retries, so we return null;
                return null;
            }

            return aNewAuth;
        }
        
        public AuthenticatedSession CreateAuthSession(string in_strUserName, string in_strPassword)
        {
            if (string.IsNullOrEmpty(in_strPassword) || string.IsNullOrEmpty(in_strUserName))
                return null;

            string strItemName = string.Empty;

            // lets see if we can get the user from the cache.

            DBUser u = new DBUser();
            if (!u.FromCache(in_strUserName.ToLower()))
            {
                // nope, no cached object.
                Log.Verbose("CreateAuthSession could not find user object for " + in_strUserName + " --- DB required");
                
                SelectResult selectResult = null;
                RetryUtility.RetryAction(3, 300, () =>
                {
                    string selectExpression = "Select * From NPUsers Where UserName = '" + in_strUserName.ToLower() + "' or EMail='" + in_strUserName.ToLower() + "'";
                    SelectRequest selectRequestAction = new SelectRequest().WithSelectExpression(selectExpression);
                    SelectResponse selectResponse = SimpleDBService.Select(selectRequestAction);
                    Log.Warning("DB SELECT");

                    if (!selectResponse.IsSetSelectResult())
                        throw new Exception();
                    selectResult = selectResponse.SelectResult;
                    if (selectResult.Item.Count != 1)
                        throw new Exception();
                });

                foreach (Amazon.SimpleDB.Model.Attribute a in selectResult.Item[0].Attribute)
                {
                    if (a.Name == "Password")
                        u.Password = a.Value;
                    if (a.Name == "UserName")
                        u.UserName = a.Value;
                    if (a.Name == "EMail")
                        u.EMail = a.Value;

                    if (a.Name == "HomeCity")
                        u.HomeCity = a.Value;
                    if (a.Name == "HomeCountry")
                        u.HomeCountry = a.Value;
                    if (a.Name == "HomeProvince")
                        u.HomeProvince = a.Value;
                    if (a.Name == "HomeTimeZone")
                        u.HomeTimeZone = a.Value;
                }
                u.UserID = selectResult.Item[0].Name;

                u.ToCache();
            }

            if (u.Password != in_strPassword)
                return null;

            AuthenticatedSession aNewAuth = new AuthenticatedSession();
            aNewAuth.UserID = u.UserID;
            aNewAuth.Expiry = DateTime.UtcNow.AddDays(7);
            aNewAuth.AuthToken = Guid.NewGuid().ToTiny();
            aNewAuth.UserName = u.UserName;
            aNewAuth.EMail = u.EMail;

            QueueManager.Submit(aNewAuth as ISendToSimpleDB);

            Log.Debug("Submitted auth token " + aNewAuth.AuthToken + " to NPSessions");

            aNewAuth.ToCache();

            return aNewAuth;
        }

        
        public string LookupUserID(string in_strName)
        {
            DBUser d = new DBUser();
            if (d.FromCache(in_strName.ToLower()))
            {
                return d.UserID;
            }
            
            try
            {
                Log.Verbose("LookupUserID could not find cached user" + in_strName + " --- DB required");
                //
                // first, does the user exist?
                //
                SelectResponse selectResponse = null;
                RetryUtility.RetryAction(3, 300, () =>
                {
                    string selectExpression = "Select * From NPUsers Where UserName = '" + in_strName.ToLower() + "'";
                    SelectRequest selectRequestAction = new SelectRequest().WithSelectExpression(selectExpression);
                    selectResponse = SimpleDBService.Select(selectRequestAction);
                    Log.Warning("DB SELECT");
                });

                if (selectResponse.IsSetSelectResult())
                {
                    SelectResult selectResult = selectResponse.SelectResult;
                    if (selectResult.Item.Count == 0)
                    {
                        return string.Empty;
                    }


                    if( Cache.Store(StoreMode.Set, "usernametoid_" + in_strName.ToLower(), selectResult.Item[0].Name) )
                        Log.Verbose("LookupUserID stored usernametoid_" + in_strName.ToLower() +" = " + selectResult.Item[0].Name );
                    return selectResult.Item[0].Name;
                }

            }
            catch (AmazonSimpleDBException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
                Console.WriteLine("Response Status Code: " + ex.StatusCode);
                Console.WriteLine("Error Code: " + ex.ErrorCode);
                Console.WriteLine("Error Type: " + ex.ErrorType);
                Console.WriteLine("Request ID: " + ex.RequestId);
                Console.WriteLine("XML: " + ex.XML);

            }
            catch (Exception exx)
            {
            }

            return string.Empty;
        }


        public string CreateUser(string in_strEMailAddr, string in_strName, string in_strPassword, string in_strHomeCity, string in_strHomeProvince, string in_strHomeCountry, string in_strHomeTimeZone)
        {
            try
            {
                Log.Verbose("CreateUser checking for already existing users - " + in_strName.ToLower());
                //
                // first, does the user exist?
                //
                if( !string.IsNullOrEmpty( LookupUserID( in_strName.ToLower() ) ) )
                {
                    Log.Informational("CreateUser cannot create an already created user!");
                    return string.Empty ;
                }

                Log.Informational("Creating user: " + in_strEMailAddr.ToLower());
                //
                // seems we're here.  user does not exist!
                //
                DBUser u = new DBUser();

                u.UserID = Guid.NewGuid().ToTiny();
                u.UserName = in_strName.ToLower();
                u.EMail = in_strEMailAddr.ToLower();
                u.Password = in_strPassword;
                u.HomeCity = in_strHomeCity;
                u.HomeCountry = in_strHomeProvince;
                u.HomeProvince = in_strHomeCountry;
                u.HomeTimeZone = in_strHomeTimeZone;

                u.ToCache();

                QueueManager.Submit( u as ISendToSimpleDB );

                Log.Debug("Submitted user " + u.UserID + " to SimpleDB");
                return u.UserID;
            }
            catch (AmazonSimpleDBException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
                Console.WriteLine("Response Status Code: " + ex.StatusCode);
                Console.WriteLine("Error Code: " + ex.ErrorCode);
                Console.WriteLine("Error Type: " + ex.ErrorType);
                Console.WriteLine("Request ID: " + ex.RequestId);
                Console.WriteLine("XML: " + ex.XML);

            }
            catch (Exception exx)
            {
            }

            return string.Empty;
        }
    }
}