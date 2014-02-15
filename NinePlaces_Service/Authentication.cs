using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Amazon.SimpleDB.Model;
using Amazon.SimpleDB;
using System.Collections.Specialized;
using System.Configuration;
using Amazon;
using System.IO;
using Amazon.S3;
using System.Runtime.Serialization;

namespace NinePlaces_Service
{

    public class Authentication
    {
        private DBManager DB = new DBManager();
        private AmazonSimpleDB sdb = null;
        public AuthenticatedSession Auth { get; private set; }
        public Authentication() 
        {
            Message request = OperationContext.Current.RequestContext.RequestMessage;
            HttpRequestMessageProperty prop = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];
            if (!string.IsNullOrEmpty(prop.Headers["X-NinePlaces-Auth"]))
            {
                Auth = DB.LookupAuthSession(prop.Headers["X-NinePlaces-Auth"]);
            }

        }

        public Authentication(string in_strAuthToken)
        {
            Auth = DB.LookupAuthSession(in_strAuthToken);
        }

        public Authentication(string in_strUserName, string in_strPassword)
        {
            Auth = DB.CreateAuthSession(in_strUserName, in_strPassword);
        }

        public bool IsAuthenticated()
        {
            return (Auth != null && !string.IsNullOrEmpty(Auth.AuthToken) && DateTime.UtcNow < Auth.Expiry);
        }

        public void DestroyAuth()
        {
            if (!IsAuthenticated())
                return;

            DB.DestroyAuth(Auth.AuthToken);
        }
    }
}
