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
using NinePlaces.Models;
using NinePlaces.Models.REST;
using Common.Interfaces;
using System.Xml.Linq;
using Common;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using ExifLib;

namespace NinePlaces.Models.REST
{
    public class RESTPhotoModel : RESTBaseModel, IPhotoModel, IPrivateUploadableModel
    {
        internal static Queue<RESTPhotoModel> g_qToUpload = new Queue<RESTPhotoModel>();
        internal static object g_qLock = new object();
        public RESTPhotoModel(RESTVacationModel in_oParentModel, RESTSaveManager in_oSaveMgr)
            : base(in_oParentModel, in_oSaveMgr)
        {
            NodeType = PersistenceElements.Photo;
        }

        public event EventHandler UploadComplete;
        public event EventHandler ProgressChanged;

        /// <summary>
        /// Returns true if the photo has been uploaded to the server.
        /// Also returns true if photo already existed at server.
        /// </summary>
        /// 
        private bool m_bUploadCompleted = false;
        public bool IsAtServer
        {
            get
            {
                return m_bUploadCompleted;
            }
        }

        private int m_nUploadStatus = 0;
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
                    if (ProgressChanged != null)
                        ProgressChanged.Invoke(this, new EventArgs());
                }
            }
        }

        private int m_nDownloadStatus = 0;
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
                    if (ProgressChanged != null)
                        ProgressChanged.Invoke(this, new EventArgs());
                }
            }
        }

        #region IModel Members

        public override bool DoneLoad()
        {
            if (GetProperty("UploadComplete") == "true")
                m_bUploadCompleted = true;

            return base.DoneLoad();
        }

        #endregion

        #region IPhotoModel Members

        FileInfo m_oFI = null;
        /// <summary>
        /// Set when a new photo is about to be uploaded.
        /// This is the FileInfo that is returned from the OpenFileDialog, way out
        /// at the interface.
        /// </summary>
        public FileInfo PhotoInfo
        {
            set 
            {
                m_oFI = value;
                UploadPhoto();
            }
        }

        private string m_strFileBeingUploaded = string.Empty;
        /// <summary>
        /// Begins uploading the photo to the server!
        /// </summary>
        private void UploadPhoto()
        {
            if (string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken))
            {
                Log.WriteLine("Not authenticated for Save to server", LogEventSeverity.Debug);
                return;
            }

            //
            // we'll use the full res image as thumb until
            // we're done the upload.
            //
            FileStream filestream = m_oFI.OpenRead();

            try
            {
                ExifReader reader = new ExifReader(filestream);
                DateTime dtResult;
                reader.GetTagValue<DateTime>(ExifTags.DateTimeOriginal, out dtResult);

                SetProperty("DockTime", dtResult.ToString("yyyy-MM-ddTHH:mm:ss.ff", System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                // file creation time is dangerous.  photoshopped image?

                /*try
                {
                    SetProperty("DockTime", m_oFI.CreationTime.ToString("yyyy-MM-ddTHH:mm:ss.ff", System.Globalization.CultureInfo.InvariantCulture));
                }
                catch
                {
                    Log.WriteLine("Error getting docktime from image!");
                }*/
                Log.Exception(ex);
            }

            filestream.Seek(0, SeekOrigin.Begin);
            BitmapImage bi = new BitmapImage();
            bi.SetSource(filestream);
            if (PhotoReady != null)
                PhotoReady.Invoke(this, new PhotoDownloadEventArgs(bi));

            m_strFileBeingUploaded = m_oFI.Name;

            SetProperty("FileName", m_strFileBeingUploaded);
            UploadStatus = 0;

            m_oSaveMgr.EnqueueToUpload(this);
        }

        public void DoUpload()
        {
            
            Stream filestream = m_oFI.OpenRead();

            int nProgress = 0;
            UploadStatus = nProgress;

            filestream.Seek(0, SeekOrigin.Begin);
            //Get the list of available timelines

            Uri uri = new Uri(DataURL);
            Log.WriteLine("Saving to " + DataURL, LogEventSeverity.Debug);
            
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(uri);

            // this is only necessary if PUT is not allowed at the server!
            wr.Method = "POST";
            wr.Headers["X-HTTP-Method-Override"] = "PUT";
            wr.Headers["X-HTTP-Method"] = "PUT";
            wr.ContentType = "image/jpeg";

            wr.Headers["X-NinePlaces-Auth"] = RESTAuthentication.Auth.AuthToken;

            wr.AllowReadStreamBuffering = true;
            wr.AllowWriteStreamBuffering = false;   // this enabled upload progress.
            wr.ContentLength = filestream.Length;

   
            IAsyncResult aGetRequestResult = wr.BeginGetRequestStream(x =>
            {
                Stream rs = wr.EndGetRequestStream(x);
                FileStream fs = x.AsyncState as FileStream;
                long lTotalBytes = fs.Length;
                long lUploadedBytes = 0;
                while (true)
                {
                    byte[] buffer = new byte[100000];
                    int count = fs.Read(buffer, 0, buffer.Length);
                    if (count > 0)
                    {
                        
                        rs.Write(buffer, 0, count);
                        rs.Flush();
                        lUploadedBytes += count;
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            UploadStatus = (int)((double)lUploadedBytes / (double)lTotalBytes * 100.0);
                        });
                    }
                    else
                    {
                        break;
                    }
                }

                rs.Close();
                fs.Close();
                fs.Dispose();

                IAsyncResult result = wr.BeginGetResponse(new AsyncCallback(RemoteUploadCompleted), wr);
            }, filestream);
        }

        protected virtual void RemoteUploadCompleted(IAsyncResult result)
        {
            WebRequest r = (WebRequest)result.AsyncState;
            HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(result);
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                // we've uploaded!  lets notify out that we're done.

                UploadStatus = 100;
                m_bUploadCompleted = true;
                SetProperty("FileName", m_strFileBeingUploaded);
                SetProperty("UploadComplete", "true");

                if (UploadComplete != null)
                    UploadComplete.Invoke(this, new EventArgs());
            });
        }

        #endregion

        #region IPhotoModel Members

        /// <summary>
        /// Initiate the download of the thumbnail for this photo
        /// </summary>
        public void BeginGetThumb()
        {
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(ThumbnailURL);
            wr.Method = "GET";
            if (!string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken))
                wr.Headers["X-NinePlaces-Auth"] = RESTAuthentication.Auth.AuthToken;
            wr.AllowReadStreamBuffering = true;
            Log.WriteLine("Getting thumbnail " + ThumbnailURL, LogEventSeverity.Debug);
            IAsyncResult result = wr.BeginGetResponse(new AsyncCallback(ThumbnailDownloadCompleted), wr);
        }

        protected virtual void ThumbnailDownloadCompleted(IAsyncResult result)
        {
            try
            {
                WebRequest r = (WebRequest)result.AsyncState;
                HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(result);


                if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
                {
                    Log.WriteLine("Download failed: " + ThumbnailURL, LogEventSeverity.CriticalError);
                }
                else
                {

                    //if (string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken))
                    //{
                    //    // we're not authenticated to load.  we'll just consider this return out.
                    //    // (this can happen if the user hits logout while a load is happening).
                    //    Log.WriteLine("No longer authenticated to load " + ThumbnailURL, LogEventSeverity.Warning);
                    //    return;
                    //}

                    if (response.StatusCode == HttpStatusCode.NoContent)
                    {
                        Log.WriteLine("Load failed - no such item: " + ThumbnailURL, LogEventSeverity.Error);
                        return;
                    }

                    Stream strResp = response.GetResponseStream();
    

                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => 
                        {
                            BitmapImage bi = new BitmapImage();
                            bi.SetSource(strResp);
                            if (ThumbnailReady != null)
                                ThumbnailReady.Invoke(this, new PhotoDownloadEventArgs(bi));
                        });
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>
        /// Initiate the download of the high-res version of this photo
        /// </summary>
        public void BeginGetPhoto()
        {
            DownloadStatus = 0;
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(MidResURL);
            wr.Method = "GET";
            if( !string.IsNullOrEmpty( RESTAuthentication.Auth.AuthToken ) )
                wr.Headers["X-NinePlaces-Auth"] = RESTAuthentication.Auth.AuthToken;
            wr.AllowReadStreamBuffering = true;
            Log.WriteLine("Getting (midres)1 photo " + MidResURL, LogEventSeverity.Debug);
            IAsyncResult result = wr.BeginGetResponse(new AsyncCallback(PhotoDownloadCompleted), wr);
        }

        protected virtual void PhotoDownloadCompleted(IAsyncResult result)
        {
            try
            {
                WebRequest r = (WebRequest)result.AsyncState;
                HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(result);

                if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
                {
                    Log.WriteLine("Download failed: " + MidResURL, LogEventSeverity.CriticalError);
                }
                else
                {

                    //if (string.IsNullOrEmpty(RESTAuthentication.Auth.AuthToken))
                    //{
                    //    // we're not authenticated to load.  we'll just consider this return out.
                    //    // (this can happen if the user hits logout while a load is happening).
                    //    Log.WriteLine("No longer authenticated to load " + ThumbnailURL, LogEventSeverity.Warning);
                    //    return;
                    //}

                    if (response.StatusCode == HttpStatusCode.NoContent)
                    {
                        Log.WriteLine("Load failed - no such item: " + ThumbnailURL, LogEventSeverity.Error);
                        return;
                    }

                    Stream strResp = response.GetResponseStream();


                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        BitmapImage bi = new BitmapImage();
                        bi.SetSource(strResp);
                        if (PhotoReady != null)
                            PhotoReady.Invoke(this, new PhotoDownloadEventArgs(bi));

                        DownloadStatus = 100;
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        public event PhotoDownloadEventHandler ThumbnailReady;
        public event PhotoDownloadEventHandler PhotoReady;

        #endregion

        #region IPhotoModel Members
        public string DataURL
        {
            get
            {
                return ModelURL + "/Data";
            }
        }

        public string PhotoURL
        {
            get
            {
                return DataURL + "/FullRes";
            }
        }

        public string MidResURL
        {
            get
            {
                return DataURL + "/MidRes";
            }
        }

        public string ThumbnailURL
        {
            get
            {
                return DataURL + "/ThumbRes";
            }
        }

        #endregion
    }
}
