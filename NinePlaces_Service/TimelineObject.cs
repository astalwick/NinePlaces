using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.SimpleDB.Model;
using Amazon.SimpleDB;
using System.Xml.Linq;
using Amazon;
using System.Collections.Specialized;
using System.Configuration;
using System.Text;
using System.Globalization;
using System.Net;

namespace NinePlaces_Service
{
    [Serializable]
    public class TimelineObject : ISendToSimpleDB, ICacheable
    {
        public bool QuietLogs { get { return false; } }
        public string Domain { get { return "NPElements"; } }
        public bool SentToSimpleDB { get; set; }
        public string ItemName
        {
            get
            {
                return ID + m_strOwnerID;
            }
        }
        [NonSerialized]
        private string m_strOwnerID = string.Empty;
        [NonSerialized]
        private Authentication Auth = null;


        [NonSerialized]
        private List<TimelineObject> m_arChildren = new List<TimelineObject>();
        public List<TimelineObject> Children
        {
            get
            {
                return m_arChildren;

            }
        }

        static TimelineObject()
        {
        }

        public TimelineObject(Authentication in_oAuth, string in_strOwnerID)
        {
            Auth = in_oAuth;        // this tells me who is logged in!
            m_strOwnerID = in_strOwnerID;       // this is the owner of the timeline object.
        }

        public TimelineObject(Authentication in_oAuth, string in_strOwnerID, string in_strID)
        {
            Auth = in_oAuth;
            m_strID = in_strID;
            m_strOwnerID = in_strOwnerID;
        }

        private Dictionary<string, string> m_dictProperties = new Dictionary<string, string>();
        public Dictionary<string, string> Properties
        {
            get
            {
                return m_dictProperties;
            }
        }

        [NonSerialized]
        private List<string> m_arChildrenIDs = null;

        private string m_strID = string.Empty;
        public string ID
        {
            get
            {
                return m_strID;
            }
            protected set
            {
                if (m_strID != value)
                    m_strID = value;
            }
        }

        private string m_strParentID = string.Empty;
        public string ParentID
        {
            get
            {
                return m_strParentID;
            }
            protected set
            {
                if (m_strParentID != value)
                    m_strParentID = value;
            }
        }

        private ObjectType m_eObjectType = ObjectType.Unknown;
        public ObjectType Type
        {
            get
            {
                return m_eObjectType;
            }
            protected set
            {
                if (m_eObjectType != value)
                    m_eObjectType = value;

            }
        }

        public bool FromItem(Item in_oItem)
        {
            m_dictProperties.Clear();
            WebClient wc = new WebClient();
            foreach (Amazon.SimpleDB.Model.Attribute a in in_oItem.Attribute)
            {
                switch (a.Name)
                {
                    //special properties set on every object.
                    case "Type":
                        {
                            Enum.TryParse<ObjectType>(a.Value, out m_eObjectType);
                            break;
                        }
                    case "ID":
                        {
                            ID = a.Value;
                            break;
                        }
                    case "ParentID":
                        {
                            ParentID = a.Value;
                            break;
                        }
                    // default simple property=value
                    default:
                        {
                            if (a.Value.StartsWith("[[@BIGVALUE@]]:"))
                            {
                                string strS3Key = a.Value.Substring(15, a.Value.Length - 15);
                                string strBigValue = wc.DownloadString(new Uri("http://usercontent.nineplaces.com.s3.amazonaws.com/" + strS3Key));

                                m_dictProperties.Add(a.Name, strBigValue);
                            }
                            else
                            {
                                m_dictProperties.Add(a.Name, SimpleDBHelpers.ValueFromSimpleDB(a.Value));
                            }
                            break;
                        }
                }
            }
            return true;
        }

        public bool FromXml( XElement in_xToParse )
        {
            m_dictProperties.Clear();
            m_arChildrenIDs = new List<string>();

            Enum.TryParse<ObjectType>(in_xToParse.Name.LocalName, out m_eObjectType);
            ID = in_xToParse.Attribute("UniqueID").Value;

            if (in_xToParse.Attribute("ParentID") != null)
                ParentID = in_xToParse.Attribute("ParentID").Value;


            XElement xChildrenRoot = in_xToParse.Element("Children");
            foreach( XElement xChild  in xChildrenRoot.Elements() )
            {
                // all we care about is the id.
                TimelineObject tChild = new TimelineObject(Auth, m_strOwnerID, xChild.Attribute("UniqueID").Value);
                ObjectType tType;
                tChild.ParentID = ID;
                
                Enum.TryParse<ObjectType>(xChild.Name.LocalName, out tType);
                tChild.Type = tType;
                Children.Add(tChild);
                m_arChildrenIDs.Add(tChild.ID);
            }

            XElement xPropertiesRoot = in_xToParse.Element("Properties");
            var Properties = from Property in xPropertiesRoot.Elements()
                        where Property.Value != null
                        select new
                        {
                            Name = Property.Name.LocalName,
                            Value = Property.Value
                        };

            foreach (var Property in Properties)
            {
                if (Property.Name == "URL" && Property.Value.Contains("http://usercontent.nineplaces.com.s3.amazonaws.com/"))
                {
                    m_dictProperties.Add(Property.Name, Property.Value.Replace("http://usercontent.nineplaces.com.s3.amazonaws.com/", ""));
                }
                else
                {
                    m_dictProperties.Add(Property.Name, Property.Value);
                }
            }

            return true;
        }

        public XElement ToXml()
        {
            try
            {
                List<string> arChildZones = new List<string>();
                XElement xProps = new XElement("Properties");
                foreach (string strKey in Properties.Keys)
                {
                    if (strKey == "URL")
                    {
                        xProps.Add(new XElement(strKey, "http://usercontent.nineplaces.com.s3.amazonaws.com/" + Properties[strKey]));
                    }
                    else
                    {
                        xProps.Add(new XElement(strKey, Properties[strKey]));
                    }
                }

                XElement xChildren = new XElement("Children");
                foreach (TimelineObject t in Children)
                {
                    XElement xChild = new XElement(t.Type.ToString(),
                                        new XAttribute("UniqueID", t.ID));
                    if (t.Type == ObjectType.Icon )
                    {
                        xChild.Add(new XElement("IconType", t.Properties["IconType"]));
                    }
                    if( t.Type == ObjectType.Photo || t.Type==ObjectType.Icon )
                    {
                        xChild.Add(new XElement("DockTime", t.Properties["DockTime"]));
                        if( t.Properties.ContainsKey("Duration") )
                            xChild.Add(new XElement("Duration", t.Properties["Duration"]));
                        if (t.Properties.ContainsKey("CurrentClass"))
                            xChild.Add(new XElement("CurrentClass", t.Properties["CurrentClass"]));
                        if (t.Properties.ContainsKey("TimeZone") && !string.IsNullOrEmpty( t.Properties["TimeZone"]))
                        {
                            xChild.Add(new XElement("TimeZone", t.Properties["TimeZone"]));
                            if( !arChildZones.Contains( t.Properties["TimeZone"] ) )
                                arChildZones.Add(t.Properties["TimeZone"]);
                        }
                    }

                    if (t.Type == ObjectType.List)
                    {
                        xChild.Add(new XElement("ListName", t.Properties["ListName"]));
                    }
                    xChildren.Add(xChild);
                }

                XElement x = new XElement(Type.ToString(), new XAttribute("UniqueID", ID),
                                xProps,
                                xChildren);

                if (arChildZones.Count > 0)
                {
                    XElement xZones = new XElement("TimeZones");
                    foreach (string strZone in arChildZones)
                    {
                        try
                        {
                            xZones.Add(Global.GetTimeZoneXml(strZone));
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Could not get timezone: " + strZone);
                            Log.Exception(ex);
                        }
                    }
                    x.Add(xZones);
                }

                if (!string.IsNullOrEmpty(ParentID))
                {
                    x.Add(new XAttribute("ParentID", ParentID));
                }

                if (!Auth.IsAuthenticated() || m_strOwnerID != Auth.Auth.UserID)
                {
                    x.Add(new XAttribute("Access", "ReadOnly"));
                }

                return x;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                throw;
            }
        }

        public bool ChildrenFromCache(string in_strID, ObjectType in_tType)
        {
            if (in_tType == ObjectType.Icon || in_tType == ObjectType.ListItem || in_tType == ObjectType.Photo || in_tType == ObjectType.Generic)
            {
                m_arChildrenIDs = new List<string>();
                return true;
            }

            m_arChildrenIDs = DBManager.Cache.Get<List<string>>("obj_childids_" + m_strOwnerID + ID);
            if (m_arChildrenIDs == null)
            {
                return false;
            }

            return true;
        }

        public bool FromCache(string in_strID)
        {
            TimelineObject tThis = DBManager.Cache.Get<TimelineObject>("obj_" + m_strOwnerID + in_strID);
            if (tThis == null)
                return false;

            m_eObjectType = tThis.m_eObjectType;
            ID = tThis.ID;
            ParentID = tThis.ParentID;

            foreach (string strKey in tThis.m_dictProperties.Keys)
            {
                if (m_dictProperties.ContainsKey(strKey))
                {
                    Log.Warning("Object from cache contains one or more entry for : " + strKey);
                    Log.Warning("\tCache value: " + tThis.m_dictProperties[strKey]);
                    Log.Warning("\tThisObject value: " + m_dictProperties[strKey]);
                    m_dictProperties[strKey] = tThis.m_dictProperties[strKey];
                }
                else
                {
                    m_dictProperties.Add(strKey, tThis.m_dictProperties[strKey]);
                }
            }

            return true;
        }

        public void ToCache()
        {
            Log.Verbose("\tCached timeline object obj_" + m_strOwnerID + ID);
            DBManager.Cache.Store(Enyim.Caching.Memcached.StoreMode.Set, "obj_" + m_strOwnerID + ID, this);
            if (m_arChildrenIDs != null)
            {
                Log.Verbose("\t\tCached timeline object obj_childids_" + m_strOwnerID + ID);
                DBManager.Cache.Store(Enyim.Caching.Memcached.StoreMode.Set, "obj_childids_" + m_strOwnerID + ID, m_arChildrenIDs);
            }
        }

        public void RemoveFromCache()
        {
            DBManager.Cache.Remove("obj_" + m_strOwnerID + ID);
            DBManager.Cache.Remove("obj_childids_" + m_strOwnerID + ID);
        }

        public bool QueryYourself()
        {
            if (FromCache(ID) && ChildrenFromCache(ID, Type))
            {
                Log.Verbose("Found root object " + ID + " in cache");
                List<string> arNotFound = new List<string>();
                // ok, we found ourself in the cache.  sweet.
                // lets try to recreate all of our children.
                foreach (string strID in m_arChildrenIDs)
                {
                    TimelineObject t = new TimelineObject(Auth, m_strOwnerID, strID);
                    if (t.FromCache(strID))
                        Children.Add(t);
                    else
                        arNotFound.Add(strID);
                }

                Log.Verbose("Found " + Children.Count + " children in cache");

                if( arNotFound.Count == 0 )
                    return true;

                Log.Verbose("Could not find all children in cache.  Fallback to SimpleDB");
            }

            Log.Warning("Querying simpledb for object: " + ID); 
            
            Children.Clear();
            m_arChildrenIDs = new List<string>();
            m_dictProperties.Clear();

            //
            // ok, we missed the cache on the root.
            //
            SelectResponse selectResponse = null;
            RetryUtility.RetryAction(3, 300, () =>
            {
                SelectRequest selectRequestAction = new SelectRequest();
                selectRequestAction.SelectExpression = "select * from NPElements where UserID='" + m_strOwnerID + "' and (ID='" + ID + "' or ParentID='" + ID + "')"; ;
                selectResponse = DBManager.SimpleDBService.Select(selectRequestAction);
            });

            foreach (Item i in selectResponse.SelectResult.Item)
            {
                if (i.Name == ItemName)
                {
                    FromItem(i);
                }
                else
                {
                    TimelineObject t = new TimelineObject(Auth, m_strOwnerID);
                    t.FromItem(i);
                    Children.Add(t);
                    m_arChildrenIDs.Add(t.ID);

                    t.ToCache();
                }
            }

            ToCache();

            return true;
        }

        public ReplaceableItem ToSimpleDBRequests()
        {
            ReplaceableItem oToRet = new ReplaceableItem();
            oToRet.ItemName = ItemName;
            if (ParentID != string.Empty) 
                SimpleDBHelpers.AddAttributeValue(ref oToRet, "ParentID", ParentID, true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "Type", Type.ToString(), true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "ID", ID, true);
            SimpleDBHelpers.AddAttributeValue(ref oToRet, "UserID", m_strOwnerID, true);

            foreach (string strKey in m_dictProperties.Keys)
            {
                SimpleDBHelpers.AddAttributeValue(ref oToRet, strKey, SimpleDBHelpers.ValueToSimpleDB(m_dictProperties[strKey]), true);
            }
            return oToRet;
        }

        public bool PutYourself()
        {
            if (Auth.Auth.UserID != m_strOwnerID)
                return false;

            ToCache();

            List<BigValue> arBigValues = new List<BigValue>();
            foreach (KeyValuePair<string, string> key in Properties)
            {
                if (key.Value.Length > 512)
                {
                    // big element!
                    arBigValues.Add(new BigValue(m_strOwnerID, ID, key.Key, key.Value));
                }
            }

            foreach (BigValue bv in arBigValues)
            {
                Properties[bv.Key] = "[[@BIGVALUE@]]:" + m_strOwnerID+ "/BigValues/" + ID + "/" + bv.Key;

                QueueManager.Submit(bv);
            }

            QueueManager.Submit(this as ISendToSimpleDB);
            return true;
        }

        public bool DeleteYourself()
        {
            if (Auth.Auth.UserID != m_strOwnerID)
                return false;

            RemoveFromCache();

            QueueManager.Remove(this as ISendToSimpleDB);
            return true;
        }

    }

    public enum ObjectType
    {
        Unknown,
        Timeline,
        Vacation,
        Photo,
        Icon,
        List,
        ListItem,
        Generic
    }

    public class BigValue : ITextSendToS3
    {
        public string UserID { get; set; }
        public string ID { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        private bool m_bSent = false;
        public BigValue( string in_strUserID, string in_strObjectID, string in_strKey, string in_strValue )
        {
            UserID = in_strUserID;
            ID = in_strObjectID;
            Key = in_strKey;
            Value = in_strValue;
        }

        #region ISendToS3 Members

        public string ContentType
        {
            get { return "text/plain; charset=utf-8"; }
        }

        public string DestinationKey
        {
            get { return UserID + "/BigValues/" + ID + "/" + Key; }
        }

        public string DestinationBucket
        {
            get { return "usercontent.nineplaces.com"; }
        }

        public bool PublicRead
        {
            get { return true; }
        }

        public bool SentToS3
        {
            get;
            set;
        }

        #endregion

        #region IBaseQueueItem Members

        public bool QuietLogs
        {
            get { return true; }
        }

        #endregion

        #region ITextSendToS3 Members

        public string Text
        {
            get { return Value; }
        }

        #endregion
    }
}