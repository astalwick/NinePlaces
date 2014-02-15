using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows.Browser;
using System.Xml.Linq;
using NinePlaces.Models.REST;
using Common.Interfaces;
using Common;
using System.Threading;
using System.IO;
using System.Text;

namespace NinePlaces.Models
{
    public interface IPrivateTimelineElementModel : ITimelineElementModel
    {
        new int UniqueID { get; set; }
    }

    public interface IPrivateRESTTimelineElementModel : IPrivateTimelineElementModel
    {
        string ModelURL { get; }
    }

    /// <summary>
    /// Base class for all timeline-based hierarchical persistence.
    /// </summary>
    public class RESTBaseModel : TimelineElementBaseModel, IPrivateRESTTimelineElementModel
    {
        
        // link back to our Save manager. 
        protected RESTSaveManager m_oSaveMgr = null;
       
        /// <summary>
        /// Marks the model to be saved, and enqueues it in the save mgr.
        /// </summary>
        protected bool MustSave
        {
            get
            {
                return m_bMustSave;
            }
            set
            {
                if (value != true)
                    throw new Exception("you can only flag a model to save.  mustsave can only be set to true");
                m_bMustSave = true;
                m_oSaveMgr.EnqueueToSave(this as IPrivatePersistableModel, true);
            }
        }
        private bool m_bMustSave = false;

        public RESTBaseModel(RESTSaveManager in_oSaveMgr)
        {
            m_oSaveMgr = in_oSaveMgr;
        }

        public RESTBaseModel(IHierarchicalPropertyModel in_oParentModel, RESTSaveManager in_oSaveMgr)
        {
            m_oSaveMgr = in_oSaveMgr;
            Parent = in_oParentModel as IPrivateTimelineElementModel;
        }

        public override bool IsFullyPersisted
        {
            get
            {
                return m_oSaveMgr.ToSaveCount == 0;
            }
        }

        /// <summary>
        /// Removes aall properties and childrenn from the model
        /// </summary>
        /// <param name="in_nUniqueID"></param>
        /// <returns></returns>
        protected virtual bool Empty()
        {
            try
            {
                Log.WriteLine("Empty called on model - " + UniqueID, LogEventSeverity.Debug);
                
                m_oSaveMgr.RemoveSave(this);
                m_oSaveMgr.RemoveLoad(this);

                foreach (ITimelineElementModel im in m_oChildModelsByUniqueID.Values)
                {
                    RemoveChild(im);// RemoveYourself();
                }

                m_oChildModelsByUniqueID.Clear();

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            return false;
        }

        public override void SetProperty(string in_strName, string in_strValue)
        {
            SetProperty(in_strName, in_strValue, false);
        }

        public override void SetProperty(string in_strName, string in_strValue, bool in_bNoSave)
        {
            base.SetProperty(in_strName, in_strValue);
            if (!in_bNoSave)
                MustSave = true;
        }

        #region IPersistableModel Members

        /// <summary>
        /// Persists the model.
        /// </summary>
        public override void DoSave()
        {
            try
            {
                if (string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken) || !WritePermitted)
                {
                    Log.WriteLine("Not authenticated for Save to server", LogEventSeverity.Debug);
                    DoneSave();
                    return;
                }

                //Get the list of available timelines
                Uri uri = new Uri(ModelURL);
                Log.WriteLine("Saving to " + ModelURL, LogEventSeverity.Debug);
                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(uri);

                //wr.Method = "PUT";

                // this is only necessary if PUT is not allowed at the server!
                wr.Method = "POST";
                wr.Headers["X-HTTP-Method-Override"] = "PUT";
                wr.Headers["X-HTTP-Method"] = "PUT";
                wr.ContentType = "text/plain";
                wr.Headers["X-NinePlaces-Auth"] = RESTAuthentication.Auth.AuthToken;

                byte[] byteData = UTF8Encoding.UTF8.GetBytes(ToXml().ToString());
                wr.AllowReadStreamBuffering = true;

                IAsyncResult aGetRequestResult = wr.BeginGetRequestStream(x =>
                {
                    Stream rs = wr.EndGetRequestStream(x);
                    rs.Write( byteData, 0, byteData.Length );
                    rs.Close();

                    IAsyncResult result = wr.BeginGetResponse(new AsyncCallback(RemoteSaveCompleted), wr);
                }, null);

            }
            catch (Exception ex)
            {
                Log.WriteLine("An error occurred while saving (will be retried if possible): " + NodeType.ToString() + " " + UniqueID);
                Log.Exception(ex);

                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => InvokeSaveError(this, new EventArgs()));
            }
        }

        private XElement ToXml()
        {
            try
            {
                XElement xProps = new XElement("Properties");
                foreach( string strKey in Properties.Keys )
                {
                    xProps.Add(new XElement(strKey, Properties[strKey]));
                }

                XElement xChildren = new XElement("Children");
                foreach (ITimelineElementModel ielem in m_oChildModelsByUniqueID.Values)
                {
                    XElement xChild = new XElement(ielem.NodeType.ToString(),
                                        new XAttribute("UniqueID", ielem.UniqueID));
                    if (ielem is IIconModel)
                    {
                        IIconModel icon = ielem as IIconModel;
                        xChild.Add(new XElement("IconType", icon.IconType.ToString()));

                        xChild.Add(new XElement("DockTime", icon.Properties["DockTime"])); 
                        if( icon.Properties.ContainsKey("Duration") )
                            xChild.Add(new XElement("Duration", icon.Properties["Duration"]));

                        if (icon.Properties.ContainsKey("CurrentClass"))  
                            xChild.Add(new XElement("CurrentClass", icon.Properties["CurrentClass"]));

                        if (icon.Properties.ContainsKey("TimeZone"))
                            xChild.Add(new XElement("TimeZone", icon.Properties["TimeZone"]));
                    }

                    xChildren.Add(xChild);
                }

                XElement x = new XElement(Name, new XAttribute("UniqueID", UniqueID),
                                xProps,
                                xChildren);

                if (Parent != null && Parent is ITimelineElementModel)
                {
                    x.Add(new XAttribute("ParentID", (Parent as ITimelineElementModel).UniqueID));
                }

                return x;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            return null;
        }

        /// <summary>
        /// Depersists the model.
        /// This assumes the model already knows its uniqueid.
        /// </summary>
        public override void DoLoad()
        {
            try
            {
#warning remove this when AUTH is not required to load projects!
                if (string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken) && string.IsNullOrEmpty(OverrideUserID))
                {
                    Log.WriteLine("Not authenticated for Load from server", LogEventSeverity.Debug);
                    Empty();
                    DoneLoad();
                    return;
                }

                Log.WriteLine("Loading from " + ModelURL, LogEventSeverity.Debug);


                //Get the list of available timelines
                Uri uri = new Uri(ModelURL);
                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(uri);
                wr.Method = "GET";
                if( !string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken) )
                    wr.Headers["X-NinePlaces-Auth"] = RESTAuthentication.Auth.AuthToken;

                IAsyncResult result = wr.BeginGetResponse(new AsyncCallback(RemoteLoadCompleted), wr);
            }
            catch (Exception ex)
            {
                Log.WriteLine("An error occurred while loading " + NodeType.ToString() + " " + UniqueID);
                Log.Exception(ex);

                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => InvokeLoadError(this, new EventArgs()));
            }
        }

        /// <summary>
        /// Callback - called when the webrequest has completed and the response
        /// is available.
        /// 
        /// Handles parsing the response and notifying out that the model has been loaded.
        /// </summary>
        /// <param name="result"></param>
        protected virtual void RemoteLoadCompleted(IAsyncResult result)
        {
#warning - better way to do this?
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
                () =>
                {
                    try
                    {
                        WebRequest r = (WebRequest)result.AsyncState;
                        HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(result);

                        if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
                        {
                            Log.WriteLine("Load failed: " + ModelURL, LogEventSeverity.CriticalError);
                            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => InvokeLoadError(this, new EventArgs()));

                            Empty();
                        }
                        else
                        {
                            if (response.StatusCode == HttpStatusCode.NoContent)
                            {
                                Log.WriteLine("Load failed - no such item: " + ModelURL, LogEventSeverity.Error);
                                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => InvokeLoadError(this, new EventArgs()));
                                //Empty();
                                return;
                            }

                            Stream strResp = response.GetResponseStream();
                            XElement xRoot = XElement.Load(strResp);

                            if (string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken) && xRoot.Attribute("Access") == null)
                            {
                                // we're not authenticated to load.  we'll just consider this return out.
                                // (this can happen if the user hits logout while a load is happening).
                                Log.WriteLine("No longer authenticated to load " + ModelURL, LogEventSeverity.Warning);
                                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => InvokeLoadError(this, new EventArgs()));

                                Empty();
                                return;
                            }

                            try
                            {
                                if (xRoot.Attribute("UniqueID") != null)
                                    m_nUniqueID = Convert.ToInt32(xRoot.Attribute("UniqueID").Value);

                                if (string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken) || (xRoot.Attribute("Access") != null && xRoot.Attribute("Access").Value == "ReadOnly" ))
                                    WritePermitted = false;
                                else
                                    WritePermitted = true;

                                XElement xChildrenRoot = xRoot.Element("Children");
                                XElement xZones = xRoot.Element("TimeZones");
                                XElement xPropertiesRoot = xRoot.Element("Properties");

                                List<ITimelineElementModel> arModels = new List<ITimelineElementModel>();
                                foreach (ITimelineElementModel oExistingModel in m_oChildModelsByUniqueID.Values)
                                {
                                    arModels.Add(oExistingModel);
                                }

                                // set the props.
                                var props = from prop in xPropertiesRoot.Elements()
                                            where prop.Value != null
                                            select new
                                            {
                                                key = prop.Name.LocalName,
                                                value = prop.Value
                                            };

                                foreach (var prop in props)
                                {
                                    SetProperty(prop.key, prop.value, true);
                                }

                                //
                                // initialize up the timezones.
                                //
                                if (xZones != null)
                                {
                                    foreach (XElement xZone in xZones.Elements())
                                    {
                                        TimeZone.GetTimeZoneFromXML(xZone);
                                    } 
                                }

                                //
                                // create all the IconXMLModels
                                //

                                if (xChildrenRoot != null)
                                {
                                    var vacations = from vacation in xChildrenRoot.Elements()
                                                    where vacation.Attributes("UniqueID") != null
                                                    select new
                                                    {
                                                        uniqueid = vacation.Attribute("UniqueID").Value,
                                                        item = vacation
                                                    };

                                    List<int> lToRet = new List<int>();
                                    int n = 0;
                                    foreach (var w in vacations)
                                    {
                                        n++;
                                        PersistenceElements e;
                                        Enum.TryParse(w.item.Name.LocalName, true, out e);

                                        IPrivateTimelineElementModel icm = ConstructChildModel(e, false);
                                        icm.UniqueID = Convert.ToInt32(w.uniqueid);

                                        foreach (XElement xPropToSet in w.item.Elements())
                                        {
                                            icm.SetProperty(xPropToSet.Name.LocalName, xPropToSet.Value, true);
                                        }

                                        m_oChildModelsByUniqueID.Add(Convert.ToInt32(w.uniqueid), icm);
                                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                                            {
                                                AddChild(icm);
                                            });
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("RemoteLoad inner exception");
                                Log.Exception(ex);
                                System.Diagnostics.Debug.Assert(false);
                                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => InvokeLoadError(this, new EventArgs()));
                                return;
                            }

                            Log.WriteLine("Load completed: " + ModelURL, LogEventSeverity.Debug);
                            DoneLoad();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("RemoteLoad outer exception");
                        Log.Exception(ex);
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => InvokeLoadError(this, new EventArgs()));
                    }
                });
        }

        /// <summary>
        /// Callback - called when the persist action has been completed.
        /// </summary>
        /// <param name="result"></param>
        protected virtual void RemoteSaveCompleted(IAsyncResult result)
        {
            try
            {
                WebRequest r = (WebRequest)result.AsyncState;
                HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(result);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Log.WriteLine("Save completed: " + ModelURL, LogEventSeverity.Debug);
                    DoneSave();
                }
                else
                {
                    Log.WriteLine("Save failed (will be retried if possible): " + ModelURL, LogEventSeverity.Warning);
                    Log.WriteLine("\tStatus: " + response.StatusCode.ToString());

                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => InvokeSaveError(this, new EventArgs()));

                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);

                Log.WriteLine("Save failed (will be retried if possible): " + ModelURL, LogEventSeverity.Warning);

                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => InvokeSaveError(this, new EventArgs()));
            }
        }

        /// <summary>
        /// Called when load has completed.  Fires notificatinos of property changes out to interested listeners
        /// </summary>
        /// <returns></returns>
        public virtual bool DoneLoad()
        {
            //
            // ok, for every PROPERTY in this model, we need to fire a notification.
            //
            Loaded = true;

            List<string> arNames = new List<string>();
            foreach (string strKey in Properties.Keys)
            {
                arNames.Add(strKey);
            }

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => 
            {
                foreach( string strName in arNames )
                {
                    InvokePropertyChanged(this, new PropertyChangedEventArgs(strName));
                }

                InvokeLoadComplete(this, new EventArgs());
            });

            return true;
        }

        public virtual bool DoneSave()
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => InvokeSaveComplete(this, new EventArgs()));

            return true;
        }

        /// <summary>
        /// Commits the deletion of this model from persistence.
        /// </summary>
        /// <returns></returns>
        public bool RemoveChildFromServer(IPrivateRESTTimelineElementModel in_pToRemove)
        {
            try
            {
                if (string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken))
                {
                    Log.WriteLine("Not authenticated for Remove from server", LogEventSeverity.Debug);
                    return true;
                }

                Uri uri = new Uri(in_pToRemove.ModelURL);
                Log.WriteLine("Removing " + in_pToRemove.ModelURL + " from server!", LogEventSeverity.Informational);
                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(uri);

                //wr.Method = "DELETE";

                // this is only necessary if PUT is not allowed at the server!
                wr.Method = "POST";
                wr.Headers["X-HTTP-Method-Override"] = "DELETE";
                wr.Headers["X-HTTP-Method"] = "DELETE";
                wr.ContentType = "text/plain";
                wr.Headers["X-NinePlaces-Auth"] = RESTAuthentication.Auth.AuthToken;

                byte[] byteData = UTF8Encoding.UTF8.GetBytes("<Delete />");
                wr.AllowReadStreamBuffering = true;

                IAsyncResult aGetRequestResult = wr.BeginGetRequestStream(x =>
                {
                    Stream rs = wr.EndGetRequestStream(x);
                    rs.Write(byteData, 0, byteData.Length);
                    rs.Close();

                    IAsyncResult result = wr.BeginGetResponse(new AsyncCallback(RemoveCompleted), wr);
                }, null);

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                return false;
            }
        }

        /// <summary>
        /// Callback - called when removal of the model has been completed.
        /// </summary>
        /// <param name="result"></param>
        private void RemoveCompleted(IAsyncResult result)
        {
            try
            {
                WebRequest r = (WebRequest)result.AsyncState;
                HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(result);
                

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Log.WriteLine("Remove succeeded: " + response.ResponseUri, LogEventSeverity.Debug);
                }
                else
                {
                    Log.WriteLine("Remove failed: " + response.ResponseUri, LogEventSeverity.CriticalError);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }


        /// <summary>
        /// Enqueues this model to be loaded.
        /// </summary>
        /// <returns></returns>
        public override bool Load()
        {
            m_oSaveMgr.EnqueueToLoad(this as IPrivatePersistableModel, true);
            return true;
        }

        /// <summary>
        /// Enqueues this model to be saved.
        /// </summary>
        /// <returns></returns>
        public override bool Save()
        {
            MustSave = true;
            return true;
        }

        /// <summary>
        /// When this is false, persistence is disabled.  (Any persist requests are ignored)
        /// </summary>
        public override bool AllowPersist
        {
            get
            {
                return m_oSaveMgr.AllowPersist;
            }
            set
            {
                m_oSaveMgr.AllowPersist = value;
            }
        }

        public override string OverrideUserName
        {
            get
            {
                return base.OverrideUserName;
            }
            set
            {
                
                base.OverrideUserName = value;
                if (string.IsNullOrEmpty(OverrideUserID) && !string.IsNullOrEmpty(OverrideUserName))
                {
                    m_oSaveMgr.DelayPersist = true;

                    // lookup
                    WebClient wc = new WebClient();
                    wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);

                    Uri uri = new Uri(Helpers.GetAPIURL() + "UserNameToUserID?q=" + OverrideUserName);
                    wc.DownloadStringAsync(uri);

                }
            }
        }

        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error == null && e.Cancelled == false)
            {
                OverrideUserID = XElement.Parse(e.Result).Value;
            }
            m_oSaveMgr.DelayPersist = false;
        }

        /// <summary>
        /// returns the URL of this model's REST persist location.
        /// </summary>
        public virtual string ModelURL
        {
            get
            {
                if (string.IsNullOrEmpty(OverrideUserID))
                    return Common.Helpers.GetAPIURL()  + RESTAuthentication.Auth.UserID + "/" + NodeType.ToString().ToLower() + "/" + UniqueID;
                else
                    return Common.Helpers.GetAPIURL()  + OverrideUserID + "/" + NodeType.ToString().ToLower() + "/" + UniqueID;
            }
        }

        /// <summary>
        /// This is called if the app is in the process of shutting down, and must do a *synchronous*
        /// save.  This will block the app from closing until javascript (yes, it calls out and has
        /// javascript do the save) has finished saving.
        /// </summary>
        public override void EmergencySave()
        {
            try
            {
                if (string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken))
                {
                    return;
                }

                Log.WriteLine("EmergencySave of " + ModelURL );
                ScriptObject xhr = HtmlPage.Window.CreateInstance("XMLHttpRequest");
                xhr.Invoke("open", "POST", ModelURL, false);
                xhr.Invoke("setRequestHeader", "X-HTTP-Method-Override", "PUT");
                xhr.Invoke("setRequestHeader", "X-HTTP-Method", "PUT");
                
                xhr.Invoke("setRequestHeader", "Content-Type", "text/plain");
                xhr.Invoke("setRequestHeader", "X-NinePlaces-Auth", RESTAuthentication.Auth.AuthToken);
                xhr.Invoke("send", ToXml());
                string strResponse = (string)xhr.GetProperty("responseText");
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        #endregion

        protected virtual IPrivateTimelineElementModel ConstructChildModel(PersistenceElements in_eNodeType, bool in_bAlreadyLoaded)
        {
            throw new NotImplementedException();
        }

        #region ITimelineElementModel Members

        public override bool RemoveChild(int in_nUniqueID)
        {
            ITimelineElementModel p = m_oChildModelsByUniqueID[in_nUniqueID];
            return RemoveChild(p);
        }

        public override bool RemoveChild(IHierarchicalPropertyModel in_p)
        {
            ITimelineElementModel oT = in_p as ITimelineElementModel;

            m_oSaveMgr.RemoveSave(in_p as IPrivatePersistableModel);
            m_oSaveMgr.RemoveLoad(in_p as IPrivatePersistableModel);

            m_oChildModelsByUniqueID.Remove(oT.UniqueID);
            base.RemoveChild(in_p);
            RemoveChildFromServer(in_p as IPrivateRESTTimelineElementModel);
            MustSave = true;

            return true;
        }


        /// <summary>
        /// Creates a new child model, and sets default properties on the newly created child.
        /// </summary>
        /// <param name="in_eNodeType"></param>
        /// <param name="in_dictPropertiesToSet"></param>
        /// <returns></returns>
        public override ITimelineElementModel NewChild(PersistenceElements in_eNodeType, Dictionary<string, string> in_dictPropertiesToSet)
        {
            return NewChild(in_eNodeType, in_dictPropertiesToSet, 0);
        }

        /// <summary>
        /// Creates a new child model of a given type.
        /// </summary>
        /// <param name="in_eNodeType"></param>
        /// <returns></returns>
        public override ITimelineElementModel NewChild(PersistenceElements in_eNodeType)
        {
            return NewChild(in_eNodeType, null, 0);
        }

        /// <summary>
        /// Creates a new child of a given type, with a specified UniqueID.
        /// This method should be used with care - UniqueID collisions are dangerous and must be avoided.
        /// </summary>
        /// <param name="in_eNodeType"></param>
        /// <param name="in_dictPropertiesToSet"></param>
        /// <param name="in_nUniqueID"></param>
        /// <returns></returns>
        public override ITimelineElementModel NewChild(PersistenceElements in_eNodeType, Dictionary<string, string> in_dictPropertiesToSet, int in_nUniqueID)
        {
            try
            {
                int nID = in_nUniqueID;
                if (nID == 0)
                {
                    // generate an id.
                    // loop until it's unique.
                    do
                    {
                        nID = r.Next();
                    }
                    while (m_oChildModelsByUniqueID.ContainsKey(nID));
                }


                // create a new model for this child
                IPrivateTimelineElementModel xChildModel = ConstructChildModel(in_eNodeType, true);
                xChildModel.UniqueID = nID;

                // add it to our ID <-> IconModel dictionary.
                m_oChildModelsByUniqueID.Add(nID, xChildModel);
                

                if (in_dictPropertiesToSet != null)
                {
                    foreach (string strProperty in in_dictPropertiesToSet.Keys)
                    {
                        xChildModel.SetProperty(strProperty, in_dictPropertiesToSet[strProperty]);

                    }
                }

                Log.WriteLine("New child model created - " + in_eNodeType.ToString() + " - " + nID, LogEventSeverity.Debug);
                AddChild(xChildModel);

                // make sure we persist this change.
                MustSave = true;

                return xChildModel as ITimelineElementModel;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            return null;
        }


        public override string Name
        {
            get
            {
                return NodeType.ToString();
            }
            protected set
            {
            }
        }
        #endregion
    }

}
