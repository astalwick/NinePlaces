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
using Common.Interfaces;
using System.Windows.Media.Imaging;
using Common;
using NinePlaces.Helpers;
using System.Globalization;

namespace NinePlaces.ViewModels
{
    public class PhotoViewModel : BaseViewModel, IPhotoViewModel
    {
        public PhotoViewModel(ITimelineElementModel in_oXMLModel)
        {
            IsThumbnailAvailable = false;
            Model = in_oXMLModel;
            // the model will notify us when it has a thhumbnail image to hand over.
            (Model as IPhotoModel).ThumbnailReady += new PhotoDownloadEventHandler(PhotoViewModel_ThumbnailReady);
            (Model as IPhotoModel).PhotoReady += new PhotoDownloadEventHandler(PhotoViewModel_PhotoReady);
            (Model as IPhotoModel).ProgressChanged += new EventHandler(PhotoViewModel_ProgressChanged);
            Model.LoadComplete+=new EventHandler(Model_LoadComplete);
        }

        void PhotoViewModel_ProgressChanged(object sender, EventArgs e)
        {
            UploadStatus = (Model as IPhotoModel).UploadStatus;
            DownloadStatus = (Model as IPhotoModel).DownloadStatus;
        }

        void PhotoViewModel_PhotoReady(object sender, PhotoDownloadEventArgs args)
        {
            IsPhotoRes = true;
            Photo = args.DownloadedImage;
        }

        // docktime property is important for docking!
        private DateTime m_dtDockTime;
        public virtual DateTime DockTime
        {
            get
            {
                return m_dtDockTime;
            }
            set
            {
                m_dtDockTime = value;
                if (m_dtDockTime.Ticks == 0)
                    System.Diagnostics.Debug.Assert(false);
                NotifyPropertyChanged("DockTime", m_dtDockTime.ToPersistableString());
                NotifyPropertyChanged("LocalDockTime");
            }
        }

        public virtual DateTime LocalDockTime
        {
            get
            {
                return DockTime.AddHours(App.TimeZones.OffsetAtTime(DockTime));
            }
            set
            {
                DockTime = value.AddHours(-App.TimeZones.OffsetAtTime(DockTime, true));
            }
        }

        public virtual bool LocalDockTimeUnreachable { get { return false; } }


        public bool IsThumbnailAvailable { get; set; }
        private bool m_bIsPhotoRes = false;
        public bool IsPhotoRes
        {
            get
            {
                return m_bIsPhotoRes;
            }
            protected set
            {
                if (m_bIsPhotoRes != value)
                    m_bIsPhotoRes = value;
            }
        }

        public bool IsUploaded
        {
            get
            {
                return (Model as IPhotoModel).IsAtServer;
            }
        }


        private BitmapImage m_iOrigPhoto = null;
        public BitmapImage Photo
        {
            get
            {
                return m_iOrigPhoto;
            }
            protected set
            {
                m_iOrigPhoto = value;
                NotifyPropertyChanged("Photo");
            }
        }

        public string IconDescription
        {
            get
            {
                return FileName;
            }
        }

        private string m_strFileName = string.Empty;
        public string FileName
        {
            get
            {
                return m_strFileName;
            }
            protected set
            {
                if (m_strFileName != value)
                {
                    m_strFileName = value;
                    NotifyPropertyChanged("IconDescription");
                }
            }
        }
        private int m_nUploadStatus = -1;
        public int UploadStatus
        {
            get
            {
                return m_nUploadStatus;
            }
            protected set
            {
                if (m_nUploadStatus != value)
                {
                    m_nUploadStatus = value;
                    NotifyPropertyChanged("UploadStatus");
                }
            }
        }

        private int m_nDownloadStatus = -1;
        public int DownloadStatus
        {
            get
            {
                return m_nDownloadStatus;
            }
            protected set
            {
                if (m_nDownloadStatus != value)
                {
                    m_nDownloadStatus = value;
                    NotifyPropertyChanged("DownloadStatus");
                }
            }
        }

        public void DownloadPhoto()
        {
            (Model as IPhotoModel).BeginGetPhoto();
        }
    
        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;
            if( in_strProperty == "FileName" && Model.HasProperty("FileName" ))
            {
                FileName = (Model as IPhotoModel).GetProperty("FileName") ;
            }
            else if (in_strProperty == "DockTime" && Model.HasProperty("DockTime"))
            {
                DateTime dtTime;
                DateTime.TryParseExact(Model.GetProperty("DockTime"), "yyyy-MM-ddTHH:mm:ss.ff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtTime);

                if (dtTime != m_dtDockTime)
                {
                    m_dtDockTime = dtTime;
                    System.Diagnostics.Debug.Assert(m_dtDockTime.Ticks > 0);
                    return true;
                }
            }

            return base.UpdatePropertyFromModel(in_strProperty);
        }

#warning - this code shows up in IconViewModel.cs as well.  should be put in a common base class.
        protected override void UpdateBaseProperties()
        {
            if (Model == null)
                return;

            try
            {
                if (Model.HasProperty("DockTime"))
                {
                    DateTime.TryParseExact(Model.GetProperty("DockTime"), "yyyy-MM-ddTHH:mm:ss.ff", CultureInfo.InvariantCulture, DateTimeStyles.None, out m_dtDockTime);
                    System.Diagnostics.Debug.Assert(m_dtDockTime.Ticks > 0);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                System.Diagnostics.Debug.Assert(false);
            }

            base.UpdateBaseProperties();
        }


        protected override void Model_LoadComplete(object sender, EventArgs e)
        {
            base.Model_LoadComplete(sender, e);

            if (Model == null)
                return;

            InvokeLoadComplete(this, new EventArgs());
            InvokeChildrenLoaded(this, new EventArgs());

            if (!(Model as IPhotoModel).IsAtServer)
            {
                Log.WriteLine("Incomplete photo upload detected: " + FileName + " - removing");
                Model.Parent.RemoveChild(Model);
                return;
            }

            if( Photo == null )
                (Model as IPhotoModel).BeginGetThumb();
        }

        void PhotoViewModel_ThumbnailReady(object sender, PhotoDownloadEventArgs args)
        {
            // no need
            if (IsPhotoRes)
                return;

            IsThumbnailAvailable = true;

            if( Photo == null )
                Photo = args.DownloadedImage;
        }

        private IInterfaceElementWithVM m_oInterfaceElem = null;

        public new IInterfaceElementWithVM InterfaceElement
        {
            get
            {
                return m_oInterfaceElem as IInterfaceElementWithVM;
            }
            set
            {
                if (value == null)
                {
                    m_oInterfaceElem = null;
                }
                else
                {
                    m_oInterfaceElem = value as IInterfaceElementWithVM;
                }
            }
        }
    }
}
