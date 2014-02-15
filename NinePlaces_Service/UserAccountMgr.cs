using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.SimpleDB;
using Amazon;
using System.Configuration;
using System.Collections.Specialized;
using Amazon.SimpleDB.Model;
using System.IO;

namespace NinePlaces_Service
{
    public class UserAccountMgr
    {
        public UserAccountMgr()
        {
        }

        private DBManager DB = new DBManager();
        public string LookupUserID(string in_strName)
        {
            return DB.LookupUserID(in_strName);

        }
        public string CreateUser(string in_strEMailAddr, string in_strName, string in_strPassword, string in_strHomeCity, string in_strHomeProvince, string in_strHomeCountry, string in_strHomeTimeZone)
        {
            return DB.CreateUser(in_strEMailAddr, in_strName, in_strPassword,in_strHomeCity, in_strHomeProvince, in_strHomeCountry, in_strHomeTimeZone);
        }
    }
}
