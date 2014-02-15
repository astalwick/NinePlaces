using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Web.Hosting;
using System.IO;

namespace NinePlaces_Service
{
    public class ListObj : IRenderableEvent
    {
        TimelineObject m_oList = null;
        protected Dictionary<string, string> Properties { get { return m_oList.Properties; } }

        public ListObj(Authentication in_aAuth, string in_strListID)
        {
            m_oList = new TimelineObject(in_aAuth, in_aAuth.Auth.UserID, in_strListID);
        }

        public string Property(string in_strName)
        {
            if (Properties.ContainsKey(in_strName))
                return Properties[in_strName];
            return string.Empty;
        }

        public string ListName
        {
            get
            {
                return Property("ListName");
            }
        }

        List<IRenderableEvent> arEvents = new List<IRenderableEvent>();
        public void Load()
        {

            m_oList.QueryYourself();
            IEnumerable<TimelineObject> arSortedObjects = from child in m_oList.Children where child.Type == ObjectType.ListItem select child;
            foreach (TimelineObject tChild in arSortedObjects)
            {
                tChild.QueryYourself();
                arEvents.Add(new ListItemObject(tChild));
            }
        }

        public string RenderToHTML()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IRenderableEvent r in arEvents)
            {
                sb.Append(r.RenderToHTML());
            }

            string strToRender = File.ReadAllText(HostingEnvironment.ApplicationPhysicalPath + "App_Data\\HTML\\listbody.thtml");

            return strToRender.Replace("%%LISTNAME%%", ListName).Replace("%%ITEMS%%", sb.ToString());
        }

        public class ListItemObject : IRenderableEvent
        {
            protected TimelineObject m_tObject = null;
            protected Dictionary<string, string> Properties { get { return m_tObject.Properties; } }
            public ListItemObject(TimelineObject in_tObject)
            {
                m_tObject = in_tObject;
            }

            public string Property(string in_strName)
            {
                if (Properties.ContainsKey(in_strName))
                    return Properties[in_strName];
                return string.Empty;
            }

            public string ListEntry
            {
                get
                {
                    return Property("ListEntry");
                }
            }

            public bool Checked
            {
                get
                {
                    return string.IsNullOrEmpty(Property("Checked")) ? false : Convert.ToBoolean(Property("Checked"));
                }
            }
            #region IRenderableEvent Members

            public string RenderToHTML()
            {
                string strToRender = File.ReadAllText(HostingEnvironment.ApplicationPhysicalPath + "App_Data\\HTML\\listitem.thtml");
                return strToRender.Replace("%%LISTENTRY%%", ListEntry).Replace("%%CHECKED%%", Checked ? "checked" : "");
            }

            #endregion
        }
        
    }
}