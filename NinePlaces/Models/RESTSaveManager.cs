using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace NinePlaces.Models.REST
{
    public class RESTSaveManager
    {
        //public static string CloudURL = "http://nineplaces.cloudapp.net";
        public static string CloudURL = "http://nineplaces.cloudapp.net";
        private object m_oPersistLock = new object();

        private bool SavingOrLoading
        {
            get
            {
                return m_oSaving.Count > 0 || m_oLoading.Count > 0;
                
            }
        }
        private static DispatcherTimer SaveTimer = null;
        private static DispatcherTimer LoadTimer = null;

        private List<IPersistableModel> m_oToSave = new List<IPersistableModel>();
        private List<IPersistableModel> m_oSaving = new List<IPersistableModel>();
        public event EventHandler SaveComplete;
        public event EventHandler SaveError;

        private List<IPersistableModel> m_oToLoad = new List<IPersistableModel>();
        private List<IPersistableModel> m_oLoading = new List<IPersistableModel>();
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
                else
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
                            System.Diagnostics.Debug.WriteLine("ALLOWPERSIST set to FALSE with savedata waiting!");
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
            if (SaveTimer.IsEnabled)
                SaveTimer.Stop();
            UIThreadSave();
        }

        void LoadTimer_Tick(object sender, EventArgs e)
        {
            if (SavingOrLoading || m_oToLoad.Count == 0 || DelayPersist || !AllowPersist || MustSave) 
                return;     // no, sorry, we're really not allowed to load!

            if (LoadTimer.IsEnabled)
                LoadTimer.Stop();

            DelayPersist = true;

            System.Diagnostics.Debug.WriteLine("LoadTick - " + m_oToLoad.Count.ToString() + " items");
            lock (m_oPersistLock)
            {
                // we mark it as being saved (toss it into the saving list)
                m_oLoading.AddRange(m_oToLoad);
                // we *iterate* through a local list, though, to protect against
                // item removal while we're still in the loop below.  (SaveComplete
                // gets invoked on an item before we're done iterating).
                List<IPersistableModel> oToLoad = new List<IPersistableModel>();
                oToLoad.AddRange(m_oToLoad);

                m_oToLoad.Clear();

                for (int i = 0; i < oToLoad.Count; i++)
                {
                    oToLoad[i].LoadComplete += new EventHandler(im_LoadComplete);
                    oToLoad[i].ISOLoad();
                }
            }
            MustSave = false;
        }

        void im_LoadComplete(object sender, EventArgs e)
        {
            lock (m_oPersistLock)
            {
                IPersistableModel oPersistableModel = sender as IPersistableModel;
                m_oLoading.Remove(sender as IPersistableModel);
                oPersistableModel.LoadComplete -= new EventHandler(im_LoadComplete);
                if (m_oLoading.Count == 0)
                {
                    // ok, we're ready to start accepting persistence requests again.
                    DelayPersist = false;

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

            if (LoadComplete != null)
                LoadComplete.Invoke(this, new EventArgs());
        }

        void SaveTimer_Tick(object sender, EventArgs e)
        {
            if (SavingOrLoading || DelayPersist || !AllowPersist || m_oToSave.Count == 0)      /// no deal
                return;

            // ok, while we're saving
            DelayPersist = true;
            System.Diagnostics.Debug.WriteLine("SaveTick - " + m_oToSave.Count.ToString() + " items");
            lock (m_oPersistLock)
            {
                // we mark it as being saved (toss it into the saving list)
                m_oSaving.AddRange(m_oToSave);
                // we *iterate* through a local list, though, to protect against
                // item removal while we're still in the loop below.  (SaveComplete
                // gets invoked on an item before we're done iterating).
                List<IPersistableModel> oToSave = new List<IPersistableModel>();
                oToSave.AddRange(m_oToSave);
                
                m_oToSave.Clear();

                for (int i = 0; i < oToSave.Count; i++)
                {
                    oToSave[i].SaveComplete += new EventHandler(im_SaveComplete);
                    oToSave[i].ISOSave();
                }
            }
        }

        void im_SaveComplete(object sender, EventArgs e)
        {
            lock (m_oPersistLock)
            {
                IPersistableModel oPersistableModel = sender as IPersistableModel;
                m_oSaving.Remove(sender as IPersistableModel);
                oPersistableModel.SaveComplete -= new EventHandler(im_SaveComplete);
                if (m_oSaving.Count == 0)
                {
                    if( m_oToSave.Count == 0 )
                        MustSave = false;
                    
                    App.UndoMgr.CommitStep();

                    // ok, we're ready to start accepting persistence requests again.
                    DelayPersist = false;

                    if (SaveComplete != null)
                        SaveComplete.Invoke(this, new EventArgs());

                    if (m_oToSave.Count > 0)
                    {
                        // we have new stuff to persist already.  start the timer, and do it right away.
                        SaveTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
                        SaveTimer.Start();
                    }
                    else
                    {
                        SaveTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
                    }
                }
            }
        }
        
        public void EnqueueToSave(IPersistableModel in_oPersistable)
        {
            EnqueueToSave(in_oPersistable, true);
        }

        public void EnqueueToSave(IPersistableModel in_oPersistable, bool in_bTimerStart)
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

        public void RemoveSave(IPersistableModel in_oPersistable)
        {
            lock (m_oPersistLock)
            {
                if (m_oToSave.Contains(in_oPersistable))
                {
                    m_oToSave.Remove(in_oPersistable);
                }
            }
        }

        public void EnqueueToLoad(IPersistableModel in_oPersistable)
        {
            EnqueueToLoad(in_oPersistable, true);
        }

        public void EnqueueToLoad(IPersistableModel in_oPersistable, bool in_bTimerStart)
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

        public void RemoveLoad(IPersistableModel in_oPersistable)
        {
            lock (m_oPersistLock)
            { 
                if (m_oToLoad.Contains(in_oPersistable))
                    m_oToLoad.Remove(in_oPersistable);
            }
        }

        void UIThreadSave()
        {
            System.Diagnostics.Debug.WriteLine("SYCHRONOUS EMERGENCY SAVE BEGINNING");

            foreach (IPersistableModel im in m_oToSave)
            {
                im.EmergencySave();
            }

            foreach (IPersistableModel im in m_oSaving)
            {
                im.EmergencySave();
            }

            System.Diagnostics.Debug.WriteLine("SYCHRONOUS EMERGENCY SAVE COMPLETED");
        }
    }
}
