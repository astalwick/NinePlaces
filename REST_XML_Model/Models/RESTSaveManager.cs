using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Common.Interfaces;
using Common;
using System.ComponentModel;

namespace NinePlaces.Models.REST
{
    public class RESTSaveManager
    {



        private object m_oPersistLock = new object();

        private bool SavingOrLoading
        {
            get
            {
                return m_oSaving.Count > 0 || m_oLoading.Count > 0;
            }
        }

        public int ToSaveCount
        {
            get
            {
                return m_oSaving.Count + m_oToSave.Count;
            }
        }

        private static DispatcherTimer SaveTimer = null;
        private static DispatcherTimer LoadTimer = null;

        private List<IPrivatePersistableModel> m_oToSave = new List<IPrivatePersistableModel>();
        private Queue<IPrivateUploadableModel> m_qToUpload = new Queue<IPrivateUploadableModel>();
        private List<IPrivatePersistableModel> m_oSaving = new List<IPrivatePersistableModel>();
        public event EventHandler SaveComplete;
        public event EventHandler SaveError;

        private List<IPrivatePersistableModel> m_oToLoad = new List<IPrivatePersistableModel>();
        private List<IPrivatePersistableModel> m_oLoading = new List<IPrivatePersistableModel>();
        public event EventHandler LoadComplete;
        public event EventHandler LoadError;

        //
        // delay persist means that a save/load tick should not
        // happen, but that we should continue accumulating
        // property changes so that when we're no longer delaying,
        // we can persist out.
        //
        private int m_nDelayPersist = 0;
        public bool DelayPersist
        {
            get
            {
                return m_nDelayPersist > 0;
            }
            set
            {
                if (!value && m_nDelayPersist > 0)
                    m_nDelayPersist--;
                else if (value) 
                    m_nDelayPersist++;

                if (DelayPersist)
                {
                    // stop everything!
                    if (LoadTimer.IsEnabled)
                        LoadTimer.Stop();
                    if (SaveTimer.IsEnabled)
                        SaveTimer.Stop();
                }
                else if (!SavingOrLoading)
                {
                    // restart our timers, if necessary.
                    if (m_oToSave.Count > 0)
                        SaveTimer.Start();
                    else if (m_oToLoad.Count > 0)
                        LoadTimer.Start();
                }
            }
        }

        //
        // allow persist means that we NO LONGER TRACK property change
        // notifications.  they are effectively lost to persistence.
        //
        private int m_nDisallowPersist = 0;
        public bool AllowPersist
        {
            get
            {
                return m_nDisallowPersist == 0;
            }
            set
            {
                if (value && m_nDisallowPersist > 0)
                    m_nDisallowPersist--;
                else if (!value)
                    m_nDisallowPersist++;
                
                if (!AllowPersist)
                {
                    //
                    // first, shutdown the timers.
                    //
                    if (LoadTimer.IsEnabled)
                        LoadTimer.Stop();
                    if (SaveTimer.IsEnabled)
                        SaveTimer.Stop();

                    lock (m_oPersistLock)
                    {
                        if (m_oToSave.Count > 0)
                        {
                            Log.WriteLine("ALLOWPERSIST set to FALSE with savedata waiting!", LogEventSeverity.Warning);
                            //
                            // ok, we're heading into dangerous territory.  we're being told NOT to allow
                            // persistence anymore, but we've got stuff in our save list.
                            // we HAVE TO SAVE it now - we have to flush the queue so that we 
                            // don't lose the information we were supposed to save..
                            //
                            while (SavingOrLoading)
                            {
                                // this sucks, we're locking up the ui thread, but that's life.
                                // we have to wait for the backgroundworker to do its thing.
                                System.Threading.Thread.Sleep(10);
                            }

                            UIThreadSave();
                        }

                        m_oToSave.Clear();
                        m_oToLoad.Clear();
                    }
                }
            }
        }

        // some changed state info.
        private bool m_bMustSave = false;
        private bool MustSave
        {
            get
            {
                return m_bMustSave;
            }
            set
            {
                if (AllowPersist)
                {
                    m_bMustSave = value;
                    if (!DelayPersist && m_bMustSave)
                    {
                        // we're allowed to persist!
                        SaveTimer.Start();
                    }
                    else
                    {
                        if( SaveTimer.IsEnabled )
                            SaveTimer.Stop();
                    }
                }
                else if (!AllowPersist )
                {
                    // we're not allowed to persist,
                    // so that's ok - just stop the timers
                    // and set the value.
                    m_bMustSave = false;
                    if (SaveTimer.IsEnabled)
                        SaveTimer.Stop();
                }
            }
        }

        public RESTSaveManager()
        {
            SaveTimer = new DispatcherTimer();
            LoadTimer = new DispatcherTimer();
            SaveTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            LoadTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);


            SaveTimer.Tick += new EventHandler(SaveTimer_Tick);
            LoadTimer.Tick += new EventHandler(LoadTimer_Tick);
            Application.Current.Exit += new EventHandler(AppClosing);
        }

        void AppClosing(object sender, EventArgs e)
        {
            Log.WriteLine("App is closing!", LogEventSeverity.Debug);
            if (SaveTimer.IsEnabled)
                SaveTimer.Stop();
            UIThreadSave();
        }

        void LoadTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (SavingOrLoading || m_oToLoad.Count == 0 || DelayPersist || !AllowPersist || MustSave)
                    return;     // no, sorry, we're really not allowed to load!

                if (LoadTimer.IsEnabled)
                    LoadTimer.Stop();

                DelayPersist = true;

                Log.WriteLine("Loading " + m_oToLoad.Count.ToString() + " items", LogEventSeverity.Informational);
                Log.Indent();
                lock (m_oPersistLock)
                {
                    // we mark it as being saved (toss it into the saving list)
                    m_oLoading.AddRange(m_oToLoad);
                    // we *iterate* through a local list, though, to protect against
                    // item removal while we're still in the loop below.  (SaveComplete
                    // gets invoked on an item before we're done iterating).
                    List<IPrivatePersistableModel> oToLoad = new List<IPrivatePersistableModel>();
                    oToLoad.AddRange(m_oToLoad);

                    m_oToLoad.Clear();

                    for (int i = 0; i < oToLoad.Count; i++)
                    {
                        oToLoad[i].LoadComplete += new EventHandler(Model_LoadComplete);
                        oToLoad[i].LoadError += new EventHandler(Model_LoadError);
                        oToLoad[i].DoLoad();
                    }
                }
                MustSave = false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("LoadTimer_Tick exception - we're now in a dangerous state!", LogEventSeverity.CriticalError);
                Log.Exception(ex);
            }
        }

        void Model_LoadError(object sender, EventArgs e)
        {
            try
            {
                CleanupLoad(sender as IPrivatePersistableModel, true);
                Log.WriteLine("Error occurred on load.", LogEventSeverity.Error);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Model_LoadError exception - we're now in a dangerous state!", LogEventSeverity.CriticalError);
                Log.Exception(ex);
            }
        }

        void Model_LoadComplete(object sender, EventArgs e)
        {
            try
            {
                CleanupLoad(sender as IPrivatePersistableModel, false);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Model_LoadComplete exception - we're now in a dangerous state!", LogEventSeverity.CriticalError);
                Log.Exception(ex);
            }
        }

        private void CleanupLoad(IPrivatePersistableModel in_oModel, bool in_bError)
        {
            lock (m_oPersistLock)
            {
                // always remove from the loading list. 
                // if we encountered an error, we'll re-enqueue to retry.
                m_oLoading.Remove(in_oModel);
                in_oModel.LoadComplete -= new EventHandler(Model_LoadComplete);
                in_oModel.LoadError -= new EventHandler(Model_LoadError);

                // ok, we're ready to start accepting persistence requests again.
                if( m_oLoading.Count == 0 )
                    DelayPersist = false;

                if (in_bError && in_oModel.RetryCount < 3)
                {
                    // we've failed our save, but we have not yet hit our retry limit.
                    // lets retry.
                    LoadTimer.Stop();
                    LoadTimer.Interval = new TimeSpan(0, 0, 0, 0, 300);
                    in_oModel.RetryCount++;
                    EnqueueToLoad(in_oModel, true);
                    Log.Unindent();
                    return;
                }

                in_oModel.RetryCount = 0;

                if (m_oLoading.Count == 0)
                {
                    Log.Unindent();



                    if (m_oToLoad.Count > 0)
                    {
                        // we have new stuff to persist already.  start the timer, and do it right away.
                        LoadTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
                        LoadTimer.Start();
                    }
                    else
                    {
                        LoadTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
                    }
                }
            }

            if (!in_bError && LoadComplete != null)
                LoadComplete.Invoke(this, new EventArgs());
            else if (in_bError && LoadError != null)
                LoadError.Invoke(this, new EventArgs());
        }

        void SaveTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (SavingOrLoading || DelayPersist || !AllowPersist || m_oToSave.Count == 0)      /// no deal
                    return;

                // ok, while we're saving
                DelayPersist = true;
                Log.WriteLine("Saving " + m_oToSave.Count.ToString() + " items", LogEventSeverity.Informational);
                Log.Indent();
                lock (m_oPersistLock)
                {
                    // we mark it as being saved (toss it into the saving list)
                    m_oSaving.AddRange(m_oToSave);
                    // we *iterate* through a local list, though, to protect against
                    // item removal while we're still in the loop below.  (SaveComplete
                    // gets invoked on an item before we're done iterating).
                    List<IPrivatePersistableModel> oToSave = new List<IPrivatePersistableModel>();
                    oToSave.AddRange(m_oToSave);

                    m_oToSave.Clear();

                    for (int i = 0; i < oToSave.Count; i++)
                    {
                        oToSave[i].SaveError += new EventHandler(Model_SaveError);
                        oToSave[i].SaveComplete += new EventHandler(Model_SaveComplete);
                        oToSave[i].DoSave();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("SaveTimer_Tick exception - we're now in a dangerous state!", LogEventSeverity.CriticalError);
                Log.Exception(ex);
            }
        }

        void Model_SaveError(object sender, EventArgs e)
        {
            try
            {
                CleanupSave(sender as IPrivatePersistableModel, true);
            }
            catch (Exception ex)
            {
                Log.WriteLine("EXCEPTION IN Model_SaveError!  The system is now in a dangerous state!", LogEventSeverity.CriticalError);
                Log.Exception(ex);
            }
        }

        void Model_SaveComplete(object sender, EventArgs e)
        {
            try
            {
                CleanupSave(sender as IPrivatePersistableModel, false);
            }
            catch (Exception ex)
            {
                Log.WriteLine("EXCEPTION IN Model_SaveComplete!  The system is now in a dangerous state!", LogEventSeverity.CriticalError);
                Log.Exception(ex);
            }
        }

        private void CleanupSave(IPrivatePersistableModel in_oSavedObject, bool in_bError)
        {
            lock (m_oPersistLock)
            {
                //
                // we ALWAYS remove the object from the saving queue, and unsubscribe from its events
                // whether the save was successful or not.  if it failed, we'll re-enqueue it and deal
                // with it down the line, as a new save event.
                //
                m_oSaving.Remove(in_oSavedObject);
                in_oSavedObject.SaveComplete -= new EventHandler(Model_SaveComplete);
                in_oSavedObject.SaveError -= new EventHandler(Model_SaveError);

                if( m_oSaving.Count == 0 )
                    DelayPersist = false;

                if (string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken))
                {
                    //
                    // we haven't actually saved anything. we're not logged in.
                    //
                    SaveTimer.Stop();
                    EnqueueToSave(in_oSavedObject, false);
                    Log.Unindent();
                    return;
                }
                else if (in_bError && in_oSavedObject.RetryCount < 5)
                {
                    // we've failed our save, but we have not yet hit our retry limit.
                    // lets retry.
                    SaveTimer.Stop();
                    SaveTimer.Interval = new TimeSpan(0, 0, 0, 0, 300);
                    in_oSavedObject.RetryCount++;
                    Log.WriteLine("Save failed, retrying (" + in_oSavedObject.RetryCount + "): " + (in_oSavedObject as IPrivateRESTTimelineElementModel).ModelURL, LogEventSeverity.Warning);
                    EnqueueToSave(in_oSavedObject, true);
                    Log.Unindent();
                    return;
                }
                else
                {
                    in_oSavedObject.RetryCount = 0;

                    // we've either succeeded or else failed past our retries.
                    // notify out.  
                    // don't retry anymore.
                    if (!in_bError && SaveComplete != null)
                    {
                        // we've saved!
                        SaveComplete.Invoke(this, new EventArgs());
                    }
                    else if (in_bError && SaveError != null)
                    {
                        Log.WriteLine("Save failed, NO MORE RETRIES!!!: " + (in_oSavedObject as IPrivateRESTTimelineElementModel).ModelURL, LogEventSeverity.CriticalError);
                        SaveError.Invoke(this, new EventArgs());
                    }
                }

                if (m_oSaving.Count == 0)
                {
                    //
                    // our save queue is empty.
                    //
                    if (m_oToSave.Count == 0)
                    {
                        SaveTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
                        MustSave = false;
                    }
                    else if (m_oToSave.Count > 0)
                    {
                        // we have new stuff to persist already.  start the timer, and do it right away.
                        SaveTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
                        SaveTimer.Start();
                    }

                    Log.Unindent();
                }
            }
        }

        public void EnqueueToSave(IPrivatePersistableModel in_oPersistable)
        {
            EnqueueToSave(in_oPersistable, true);
        }

        public void EnqueueToSave(IPrivatePersistableModel in_oPersistable, bool in_bTimerStart)
        {
            if (!AllowPersist)
                return;

            lock (m_oPersistLock)
            {
                if (!m_oToSave.Contains(in_oPersistable))
                    m_oToSave.Add(in_oPersistable);
            }

            if (m_oToSave.Count > 0 && in_bTimerStart)
                SaveTimer.Start();
        }

        int m_nMaxUploads = 2;
        List<IPrivateUploadableModel> m_arCurrentlyUploading = new List<IPrivateUploadableModel>();
        public void EnqueueToUpload(IPrivateUploadableModel in_oToUpload)
        {
            if (!AllowPersist)
                return;

            lock (m_oPersistLock)
            {
                if (m_arCurrentlyUploading.Count < m_nMaxUploads)
                {
                    // just start the upload.
                    m_arCurrentlyUploading.Add(in_oToUpload);
                    in_oToUpload.UploadComplete += new EventHandler(m_oCurrentUpload_UploadComplete);
                    in_oToUpload.DoUpload();
                }
                else
                {
                    if (!m_qToUpload.Contains(in_oToUpload))
                        m_qToUpload.Enqueue(in_oToUpload);
                }
            }

        }

        void m_oCurrentUpload_UploadComplete(object sender, EventArgs e)
        {
            lock (m_oPersistLock)
            {
                IPrivateUploadableModel oFinished = sender as IPrivateUploadableModel;
                oFinished.UploadComplete -= new EventHandler(m_oCurrentUpload_UploadComplete);
                m_arCurrentlyUploading.Remove(oFinished);
                if (m_qToUpload.Count >= 1 && m_arCurrentlyUploading.Count < m_nMaxUploads)
                {
                    IPrivateUploadableModel oToUpload = m_qToUpload.Dequeue();
                    m_arCurrentlyUploading.Add(oToUpload);
                    oToUpload.UploadComplete += new EventHandler(m_oCurrentUpload_UploadComplete);
                    oToUpload.DoUpload();
                }
            }
        }

        public void RemoveSave(IPrivatePersistableModel in_oPersistable)
        {
            lock (m_oPersistLock)
            {
                if (m_oToSave.Contains(in_oPersistable))
                {
                    m_oToSave.Remove(in_oPersistable);
                }
            }
        }

        public void EnqueueToLoad(IPrivatePersistableModel in_oPersistable)
        {
            EnqueueToLoad(in_oPersistable, true);
        }

        public void EnqueueToLoad(IPrivatePersistableModel in_oPersistable, bool in_bTimerStart)
        {
            if (!AllowPersist)
                return;

            lock (m_oPersistLock)
            {
                if (!m_oToLoad.Contains(in_oPersistable) && !m_oLoading.Contains(in_oPersistable))
                    m_oToLoad.Add(in_oPersistable);
            }
            LoadTimer.Start();
        }

        public void RemoveLoad(IPrivatePersistableModel in_oPersistable)
        {
            lock (m_oPersistLock)
            { 
                if (m_oToLoad.Contains(in_oPersistable))
                    m_oToLoad.Remove(in_oPersistable);
            }
        }

        void UIThreadSave()
        {
            try
            {
                if (m_oToSave.Count == 0 && m_oSaving.Count == 0)
                    return;

                #if( DEBUG )
                    Log.WriteLine("SYCHRONOUS EMERGENCY SAVE SKIPPED (debug)", LogEventSeverity.Warning);
                    return;
                #else
                    Log.WriteLine("SYCHRONOUS EMERGENCY SAVE BEGINNING", LogEventSeverity.Warning);
                    Log.Indent();
                    foreach (IPrivatePersistableModel im in m_oToSave)
                    {
                        im.EmergencySave();
                    }

                    foreach (IPrivatePersistableModel im in m_oSaving)
                    {
                        im.EmergencySave();
                    }
                    Log.Unindent();
                    Log.WriteLine("SYCHRONOUS EMERGENCY SAVE COMPLETED", LogEventSeverity.Warning);
                #endif
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }
}
