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
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;

namespace NinePlaces.Models.ISO
{
    public class XMLSaveManager
    {
        
        private object m_oPersistLock = new object();

        private BackgroundWorker m_oLoadSaveWorker = new BackgroundWorker();

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
                else if( !m_oLoadSaveWorker.IsBusy )
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
                            while (m_oLoadSaveWorker.IsBusy)
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

        public XMLSaveManager()
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
            while (m_oLoadSaveWorker.IsBusy)
                System.Threading.Thread.Sleep(20);

            UIThreadSave();
        }

        void LoadTimer_Tick(object sender, EventArgs e)
        {
            if (m_oLoadSaveWorker.IsBusy || m_oToLoad.Count == 0 || DelayPersist || !AllowPersist || MustSave) 
                return;     // no, sorry, we're really not allowed to load!

            DelayPersist = true;

            System.Diagnostics.Debug.WriteLine("LoadTick - " + m_oToLoad.Count.ToString() + " items");
            lock (m_oPersistLock)
            {
                //
                // three items at once, max.
                //
                for (int i = 0; i < 3 && i < m_oToLoad.Count; i++)
                {
                    IPersistableModel im = m_oToLoad[i];
                    m_oLoading.Add(im);
                    m_oToLoad.Remove(im);
                }
                //m_oLoading.AddRange(m_oToLoad);
                //m_oToLoad.Clear();
            }
            MustSave = false;

            m_oLoadSaveWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_LoadCompleted);
            m_oLoadSaveWorker.DoWork += new DoWorkEventHandler(bw_DoLoad);
            m_oLoadSaveWorker.WorkerSupportsCancellation = false;
            m_oLoadSaveWorker.WorkerReportsProgress = false;
            m_oLoadSaveWorker.RunWorkerAsync(m_oLoading);
        }

        void bw_DoLoad(object sender, DoWorkEventArgs e)
        {
            bool bRes = true;
            e.Result = true;
            List<IPersistableModel> oLoadingCopy = (List<IPersistableModel>) e.Argument;

            // persist everything that was in the doload queue.
            foreach (IPersistableModel oPersistableItem in oLoadingCopy)
            {
                bRes = oPersistableItem.ISOLoad();      // do it
                System.Diagnostics.Debug.Assert(bRes);
                if (!bRes)
                    e.Result = false;
            }
        }

        void bw_LoadCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // first thing, stop any timers so that nothing crazy happens while
            // we're completing the load
            if( LoadTimer.IsEnabled )
                LoadTimer.Stop();

            List<IPersistableModel> oLoadingCopy = new List<IPersistableModel>();
            lock (m_oPersistLock)
            {
                // empty out m_oLoading.
                oLoadingCopy.AddRange(m_oLoading);
                m_oLoading.Clear();
            }

            foreach (IPersistableModel oPersistableItem in oLoadingCopy)
            {
                // mark everything as done and allow the models to notify their interested viewmodels.
                oPersistableItem.DoneLoad();
            }
            
            // clean up the event.
            m_oLoadSaveWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(bw_LoadCompleted);
            m_oLoadSaveWorker.DoWork -= new DoWorkEventHandler(bw_DoLoad);

            if (((bool)e.Result) == false && LoadError != null)
            {
                LoadError.Invoke(this, new EventArgs());
            }
            else if (LoadComplete != null)
            {
                LoadComplete.Invoke(this, new EventArgs());
            }

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

        void SaveTimer_Tick(object sender, EventArgs e)
        {
            if (DelayPersist || !AllowPersist || m_oLoadSaveWorker.IsBusy || m_oToSave.Count == 0)      /// no deal
                return;

            // ok, while we're saving
            DelayPersist = true;
            System.Diagnostics.Debug.WriteLine("SaveTick - " + m_oToSave.Count.ToString() + " items");
            lock (m_oPersistLock)
            {
                m_oSaving.AddRange(m_oToSave);
                m_oToSave.Clear();
            }

            m_oLoadSaveWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_SaveCompleted);
            m_oLoadSaveWorker.DoWork += new DoWorkEventHandler(bw_DoSave);
            m_oLoadSaveWorker.WorkerSupportsCancellation = false;
            m_oLoadSaveWorker.WorkerReportsProgress = false;
            m_oLoadSaveWorker.RunWorkerAsync(m_oSaving);
        }

        void bw_DoSave(object sender, DoWorkEventArgs e)
        {
            List<IPersistableModel> oSaving = (List< IPersistableModel >)  e.Argument;
            bool bRes = true;
            e.Result = true;
            foreach (IPersistableModel oPersistableItem in oSaving)
            {
                bRes = oPersistableItem.ISOSave();
                System.Diagnostics.Debug.Assert(bRes);
                if (!bRes)
                    e.Result = false;
            }
        }

        void bw_SaveCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (SaveTimer.IsEnabled)
                SaveTimer.Stop();
            App.UndoMgr.CommitStep();

            List<IPersistableModel> oSavingCopy = new List<IPersistableModel>();
            lock (m_oPersistLock)
            {
                oSavingCopy.AddRange(m_oSaving);
                m_oSaving.Clear();
            }

            foreach (IPersistableModel oPersistableItem in oSavingCopy)
            {
                oPersistableItem.DoneSave();
            }

            MustSave = false;
            m_oLoadSaveWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(bw_SaveCompleted);
            m_oLoadSaveWorker.DoWork -= new DoWorkEventHandler(bw_DoSave);

            if (((bool)e.Result) == false && SaveError != null)
            {
                SaveError.Invoke(this, new EventArgs());
            }
            else if (SaveComplete != null)
            {
                SaveComplete.Invoke(this, new EventArgs());
            }
            DelayPersist = false;

            if (m_oToSave.Count > 0)
                SaveTimer.Start();
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
                if (!m_oToSave.Contains(in_oPersistable) && !m_oSaving.Contains(in_oPersistable))
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
            System.Diagnostics.Debug.WriteLine("UI THREAD SAVE BEGINNING");
            lock (m_oPersistLock)
            {
                m_oSaving.AddRange(m_oToSave);
                m_oToSave.Clear();
                bw_DoSave(this, new DoWorkEventArgs(m_oSaving));
            }
            System.Diagnostics.Debug.WriteLine("UI THREAD SAVE FINISHED");
        }
    }
}
