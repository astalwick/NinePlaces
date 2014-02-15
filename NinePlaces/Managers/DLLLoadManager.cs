using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Resources;
using System.Reflection;
using System.Collections.Generic;
using System.Xml;
using Common;

namespace NinePlaces.Managers
{
    
    public class DLLLoadManager
    {
        public DLLLoadManager()
        {
        }

        // xap file completion dict.
        private Dictionary<string, bool> m_dictDone = new Dictionary<string, bool>();
        // events to fire upon completion of a xap file download
        private Dictionary<string, List<EventHandler>> m_dictEvents = new Dictionary<string, List<EventHandler>>();
        // timings to load given xap file.
        private Dictionary<string, DateTime> m_dictTimings = new Dictionary<string, DateTime>();

        // dependencies for a given xap file.
        private Dictionary<string, List<string>> m_dictXapAssemblyDependencies = new Dictionary<string, List<string>>();

        /// <summary>
        /// given a xap file name and an event handler, will queue up the xap file for downloading.
        /// if the xap file is already queued up, your event will be added to the list of invoked
        /// events when xap file is complete.
        /// </summary>
        /// <param name="strXaptoLoad"></param>
        /// <param name="eventHandler"></param>
        public void LoadXap(string strXaptoLoad, EventHandler eventHandler)
        {
            if( Done( strXaptoLoad ))
            {
                // whoa, we're already done.  fine, we'll invoke the given event.
                if( eventHandler != null )
                    eventHandler.Invoke(this, new EventArgs());
            }
            else if (InProgress(strXaptoLoad))
            {
                // ah, it's in progress.  ok, lets wire up the event.
                if (eventHandler != null)
                {
                    if (!m_dictEvents.ContainsKey(strXaptoLoad))
                        m_dictEvents.Add(strXaptoLoad, new List<EventHandler>());
                    // remember the event to invoke when we're complete.
                    m_dictEvents[strXaptoLoad].Add(eventHandler);
                }
            }
            else
            {
                // ok, we need to start loading this xap file.
                if (eventHandler != null)
                {
                    if (!m_dictEvents.ContainsKey(strXaptoLoad))
                        m_dictEvents.Add(strXaptoLoad, new List<EventHandler>());
                    // remember the event to invoke when we're complete.
                    m_dictEvents[strXaptoLoad].Add(eventHandler);
                }

                // remember when we started the download
                m_dictTimings.Add(strXaptoLoad, DateTime.Now);
                // mark that this guy is in progress and not yet complete.
                m_dictDone.Add(strXaptoLoad, false);

                // go fetch the xap file.
#if DEBUG
                string strXap = "/ClientBin/" + strXaptoLoad;
                WebClient wcDownloadXap = new WebClient();
                wcDownloadXap.OpenReadCompleted += new OpenReadCompletedEventHandler(wcDownloadXap_OpenReadCompleted);
                wcDownloadXap.OpenReadAsync(new Uri(strXap, UriKind.Relative), strXaptoLoad);
#elif RELEASE
                string strXap = "http://www.nineplaces.com/9P/ClientBin/" + strXaptoLoad;
                WebClient wcDownloadXap = new WebClient();
                wcDownloadXap.OpenReadCompleted += new OpenReadCompletedEventHandler(wcDownloadXap_OpenReadCompleted);
                wcDownloadXap.OpenReadAsync(new Uri(strXap, UriKind.Absolute), strXaptoLoad);
#elif RELEASETESTING
                string strXap = "http://testing.nineplaces.com/9P/ClientBin/" + strXaptoLoad;
                WebClient wcDownloadXap = new WebClient();
                wcDownloadXap.OpenReadCompleted += new OpenReadCompletedEventHandler(wcDownloadXap_OpenReadCompleted);
                wcDownloadXap.OpenReadAsync(new Uri(strXap, UriKind.Absolute), strXaptoLoad);

#endif


            }
        }

        /// <summary>
        /// Requires BLOCKS waiting for a given xap file to be donwloaded.
        /// </summary>
        /// <param name="in_strXapName"></param>
        /// <returns></returns>
        public bool Requires(string in_strXapName)
        {
            if (Done(in_strXapName))
                return true;

            if (!Done(in_strXapName) && !InProgress(in_strXapName))
                throw new Exception("You need to actually start the download!");

            // we give the xap file 30 seconds to download.
            for (int i = 0; i < 120 && InProgress(in_strXapName); i++)
                System.Threading.Thread.CurrentThread.Join(250);

            if (Done(in_strXapName))
                return true;

            throw new Exception("Requires for: " + in_strXapName + " failed.");
        }

        public bool InProgress(string in_strXapName)
        {
            if (!m_dictDone.ContainsKey(in_strXapName))
                return false;

            return m_dictDone[in_strXapName] == false;
        }

        public bool Done(string in_strXapName)
        {
            if (!m_dictDone.ContainsKey(in_strXapName))
                return false;

            return m_dictDone[in_strXapName];
        }

        protected void wcDownloadXap_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            // ok, our xap file is (hopefully) downloaded.

            if (e.Error != null)
            {
                Log.WriteLine("ERROR downloading XAP file: " + e.Error.Message, LogEventSeverity.Error);
                throw e.Error;
            }

            string strXapToLoad = e.UserState as string;
            //
            // ok, this is a bit of a hack, but this is the way it's done, i guess.
            // load up the xaml file.
            //
            // note that we can't use linq at this point, because we *don't have* linq.
            // it's coming down in one of these xap f iles!  xmlreader is all we've got,
            // unless we want to package linq along with nineplaces.xap (we don't, it's  huge).
            //

            StreamResourceInfo oXapFileResourceStream = new StreamResourceInfo(e.Result, null);
            StreamResourceInfo oAppManifestResourceStream = Application.GetResourceStream(oXapFileResourceStream, new Uri("AppManifest.xaml", UriKind.Relative));

            // good, we've got our app manifest reader.
            // now spin through it, pulling out assemblies.
            List<string> arAssemblies = new List<string>();
            List<string> arExternalAssemblies = new List<string>();


            using (XmlReader reader = XmlReader.Create(oAppManifestResourceStream.Stream))
            {
                while (reader.Read())
                {
                    bool bIsExternal = false;
                    if (reader.IsStartElement())
                    {
                        if (reader.LocalName == "ExtensionPart")
                            bIsExternal = true;

                        if (reader.HasAttributes)
                        {
                            while (reader.MoveToNextAttribute())
                            {
                                if (reader.Name == "Source")
                                {
                                    if (bIsExternal && !m_dictDone.ContainsKey(reader.Value))
                                    {
                                        lock (m_dictXapAssemblyDependencies)
                                        {
                                            if (!m_dictXapAssemblyDependencies.ContainsKey(strXapToLoad))
                                                m_dictXapAssemblyDependencies.Add(strXapToLoad, new List<string>());

                                            m_dictXapAssemblyDependencies[strXapToLoad].Add(reader.Value);
                                        }
                                    }
                                    else
                                    {
                                        arAssemblies.Add(reader.Value);
                                    }
                                }
                            }
                            // Move the reader back to the element node.
                            reader.MoveToElement();
                        }
                    }
                }
            }
            BeginLoadDependencies(strXapToLoad);

            // ok, cool, arAssemblies should list all of the assemblies contained within the xap file.
            foreach (string strAssembly in arAssemblies)
            {
                // initialize the xapfile resources stream.
                oXapFileResourceStream = new StreamResourceInfo(e.Result, "application/binary");
                StreamResourceInfo oAssemblyResourceStream = Application.GetResourceStream(oXapFileResourceStream, new Uri(strAssembly, UriKind.Relative));
                if (oAssemblyResourceStream != null)
                {
                    // sweet, we're here - we've got a valid assembly.
                    // lets load it up!
                    AssemblyPart assemblyPart = new AssemblyPart();
                    assemblyPart.Load(oAssemblyResourceStream.Stream);
                }
            }

            // ok, last trick:
            // notify out that we've done the xap file loading.

            NotifyLoadComplete(strXapToLoad);
            
        }

        private void BeginLoadDependencies(string in_strXap)
        {
            lock (m_dictXapAssemblyDependencies)
            {
                if (!m_dictXapAssemblyDependencies.ContainsKey(in_strXap) || m_dictXapAssemblyDependencies[in_strXap].Count == 0)
                    return;

                foreach (string strZip in m_dictXapAssemblyDependencies[in_strXap])
                {
                    if (m_dictDone.ContainsKey(strZip) && m_dictDone[strZip] == true)
                        // already done.
                        continue;

                    if (!m_dictDone.ContainsKey(strZip))
                        m_dictDone.Add(strZip, false);
#if DEBUG
                    string strZiptoLoad = "/ClientBin/" + strZip;
                    WebClient wcDownloadXap = new WebClient();
                    wcDownloadXap.OpenReadCompleted += new OpenReadCompletedEventHandler(wcDownloadZip_Completed);
                    wcDownloadXap.OpenReadAsync(new Uri(strZiptoLoad, UriKind.Relative), new ZipLoadNotifyObject() { Zip = strZip, Xap = in_strXap });
#elif RELEASE
                    string strZiptoLoad = "http://www.nineplaces.com/9P/ClientBin/" + strZip;
                    WebClient wcDownloadXap = new WebClient();
                    wcDownloadXap.OpenReadCompleted += new OpenReadCompletedEventHandler(wcDownloadZip_Completed);
                    wcDownloadXap.OpenReadAsync(new Uri(strZiptoLoad, UriKind.Absolute), new ZipLoadNotifyObject() { Zip = strZip, Xap = in_strXap });
#elif RELEASETESTING
                    string strZiptoLoad = "http://testing.nineplaces.com/9P/ClientBin/" + strZip;
                    WebClient wcDownloadXap = new WebClient();
                    wcDownloadXap.OpenReadCompleted += new OpenReadCompletedEventHandler(wcDownloadZip_Completed);
                    wcDownloadXap.OpenReadAsync(new Uri(strZiptoLoad, UriKind.Absolute), new ZipLoadNotifyObject() { Zip = strZip, Xap = in_strXap });
#endif
                }
            }

            NotifyLoadComplete(in_strXap);
        }

        private class ZipLoadNotifyObject
        {
            public string Zip { get; set; }
            public string Xap { get; set; }
        }

        void wcDownloadZip_Completed(object sender, OpenReadCompletedEventArgs e)
        {
            //
            // sweet, we just got an assembly that we depend on.
            //

            if (e.Error != null)
            {
                Log.WriteLine("ERROR downloading ZIP (external assembly) file: " + e.Error.Message, LogEventSeverity.Error);
                throw e.Error;
            }

            ZipLoadNotifyObject oNotify = e.UserState as ZipLoadNotifyObject;

            //
            // ok, this is a bit of a hack, but this is the way it's done, i guess.
            // load up the xaml file.
            //
            // note that we can't use linq at this point, because we *don't have* linq.
            // it's coming down in one of these xap f iles!  xmlreader is all we've got,
            // unless we want to package linq along with nineplaces.xap (we don't, it's  huge).
            //

            StreamResourceInfo oXapFileResourceStream = new StreamResourceInfo(e.Result, "application/binary");
            StreamResourceInfo oAssemblyResourceStream = Application.GetResourceStream(oXapFileResourceStream, new Uri(oNotify.Zip.Replace(".zip", ".dll"), UriKind.Relative));
            if (oAssemblyResourceStream != null)
            {
                // sweet, we're here - we've got a valid assembly.
                // lets load it up!
                AssemblyPart assemblyPart = new AssemblyPart();
                assemblyPart.Load(oAssemblyResourceStream.Stream);
            }

            m_dictDone[oNotify.Zip] = true;

            NotifyLoadComplete(oNotify.Xap);
        }

        private void NotifyLoadComplete(string in_strXap)
        {
            lock( m_dictXapAssemblyDependencies )
            {
                if (m_dictXapAssemblyDependencies.ContainsKey(in_strXap) && m_dictXapAssemblyDependencies[in_strXap].Count > 0)
                {
                    foreach (string strZip in m_dictXapAssemblyDependencies[in_strXap])
                        if (!m_dictDone.ContainsKey(strZip) || !m_dictDone[strZip])
                            return;     // this zip is not complete.
                }
            }

            m_dictDone[in_strXap] = true;        // mark done!
            if (m_dictEvents.ContainsKey(in_strXap) && m_dictEvents[in_strXap] != null)
            {
                // ok, we've got caller events to invoke.  do it now.
                TimeSpan tsTimeToLoad = DateTime.Now - m_dictTimings[in_strXap];
                Log.WriteLine("Dynamically loaded " + in_strXap + " in " + tsTimeToLoad.Milliseconds + " ms", LogEventSeverity.Verbose);

                // let the caller know!

                //
                //
                // NOTE: IF YOU ARE CRASHING HERE, 
                // IT IS MOST LIKELY BECAUSE YOU ARE TRYING TO DEBUG
                // THE RELEASE VERSION.  SWITCH BACK TO DEBUG!
                //
                //
                foreach (EventHandler ev in m_dictEvents[in_strXap])
                    ev.Invoke(this, new EventArgs());
            }
        }
    }
}
