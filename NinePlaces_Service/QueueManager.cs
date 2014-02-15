using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.IO;
using Amazon;
using Amazon.S3.Model;
using System.Configuration;
using System.Collections.Specialized;
using Amazon.S3;
using System.Drawing;
using System.Drawing.Imaging;
using Amazon.SimpleDB.Model;
using Amazon.SimpleDB;

namespace NinePlaces_Service
{
    public interface IBaseQueueItem
    {
        bool QuietLogs { get; }
    }

    public interface IPathSendToS3 : ISendToS3
    {
        string Path { get; }
    }
    public interface IStreamSendToS3 : ISendToS3
    {
        Stream ObjectStream { get; }
    }
    public interface ITextSendToS3 : ISendToS3
    {
        string Text { get; }
    }

    public interface ISendToS3 : IBaseQueueItem
    {
        string ContentType { get; }
        string DestinationKey { get; }
        string DestinationBucket { get; }

        bool PublicRead { get; }
        
        bool SentToS3 { get; set; }
    }

    public interface ISendToSimpleDB : IBaseQueueItem
    {
        string ItemName { get; }
        string Domain {get;}
        ReplaceableItem ToSimpleDBRequests();
        bool SentToSimpleDB { get; set; }
    }

    public interface IBackgroundWork : IBaseQueueItem
    {
        bool DoWork();
        bool WorkComplete { get; set; }
    }

    public static class QueueManager
    {
        private static object lockobject = new object();
        private static Queue<ISendToS3> m_arS3Queue = new Queue<ISendToS3>();
        private static Queue<IBackgroundWork> m_arBWQueue = new Queue<IBackgroundWork>();
        private static Queue<ISendToSimpleDB> m_arSDBQueue = new Queue<ISendToSimpleDB>();
        private static Queue<ISendToSimpleDB> m_arSDBDeletes = new Queue<ISendToSimpleDB>();
        private static Dictionary<string, ISendToSimpleDB> m_dictSDBDeletes = new Dictionary<string, ISendToSimpleDB>();
        public static NameValueCollection appConfig;
        public static AmazonS3 S3Client = null;
        public static System.Threading.Timer m_tSubmitS3 = null; 
        public static System.Threading.Timer m_tSubmitSDB = null;
        public static System.Threading.Timer m_tDoWork = null;  

        static QueueManager()
        {
            appConfig = ConfigurationManager.AppSettings;

            Amazon.S3.AmazonS3Config S3Config = new Amazon.S3.AmazonS3Config();
            S3Client = AWSClientFactory.CreateAmazonS3Client(
                appConfig["AWSAccessKey"],
                appConfig["AWSSecretKey"],
                S3Config);

            m_tSubmitS3 = new System.Threading.Timer(S3TimerProc, null, 3000, 3000);
            m_tSubmitSDB = new System.Threading.Timer(SDBTimerProc, null, 3000, 3000);
            m_tDoWork = new System.Threading.Timer(WorkTimerProc, null, 3000, 3000);
        }

        public static void Submit(IBaseQueueItem in_oObject)
        {
            if (in_oObject is IBackgroundWork && !((in_oObject as IBackgroundWork).WorkComplete))
            {
                lock (m_arBWQueue)
                    m_arBWQueue.Enqueue(in_oObject as IBackgroundWork);
            }
            else if (in_oObject is ISendToS3 && !((in_oObject as ISendToS3).SentToS3))
            {
                lock (m_arS3Queue)
                    m_arS3Queue.Enqueue(in_oObject as ISendToS3);
            }
            else if (in_oObject is ISendToSimpleDB && !((in_oObject as ISendToSimpleDB).SentToSimpleDB))
            {
                lock (m_arSDBQueue)
                    m_arSDBQueue.Enqueue(in_oObject as ISendToSimpleDB);
            }

            // nothing to do!
        }

        
        public static void Remove(ISendToSimpleDB in_oObject)
        {
            lock (m_arSDBDeletes)
            {
                if (!m_dictSDBDeletes.ContainsKey(in_oObject.ItemName))
                {
                    m_dictSDBDeletes.Add(in_oObject.ItemName, in_oObject);
                    m_arSDBDeletes.Enqueue(in_oObject);
                }
            }
        }


        private static void S3TimerProc(object state)
        {

            m_tSubmitS3.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            try
            {
                ISendToS3 toWork = null;
                lock (m_arS3Queue)
                {
                    if (m_arS3Queue.Count > 0)
                        toWork = m_arS3Queue.Dequeue();
                }

                while (toWork != null)
                {
                    DoSendToS3(toWork);
                    toWork.SentToS3 = true;

                    // we RESUBMIT in case this object still has more work it needs done.
                    QueueManager.Submit(toWork);

                    lock (m_arS3Queue)
                    {
                        if (m_arS3Queue.Count > 0)
                            toWork = m_arS3Queue.Dequeue();
                        else
                            toWork = null;
                    }
                }
            }
            catch (Exception)
            {
            }
            m_tSubmitS3.Change(3000, 3000);
        }

        private static bool DoSendToS3(ISendToS3 oToSend)
        {
            try
            {
                RetryUtility.RetryAction(3, 300, () =>
                    {
                        PutObjectRequest pr = new PutObjectRequest();
                        if (oToSend is IStreamSendToS3)
                            pr.InputStream = (oToSend as IStreamSendToS3).ObjectStream;
                        else if (oToSend is IPathSendToS3)
                            pr.FilePath = (oToSend as IPathSendToS3).Path;
                        else if (oToSend is ITextSendToS3)
                            pr.ContentBody = (oToSend as ITextSendToS3).Text;

                        pr.GenerateMD5Digest = true;
                        pr.ContentType = oToSend.ContentType;
                        pr.BucketName = oToSend.DestinationBucket;
                        pr.CannedACL = oToSend.PublicRead ? S3CannedACL.PublicRead : S3CannedACL.Private;
                        pr.Key = oToSend.DestinationKey;

                        System.Diagnostics.Debug.WriteLine("SendToS3: " + pr.Key );

                        PutObjectResponse prResp = QueueManager.S3Client.PutObject(pr);
                    }
               );
            } 
            catch (Exception ex)
            {
                File.AppendAllText("D:/inetpub/nsono/nineplaces/SVC/App_Data/Logs/UPLOAD.txt", "SendToS3:" + ex.Message + "\r\n");
                File.AppendAllText("D:/inetpub/nsono/nineplaces/SVC/App_Data/Logs/UPLOAD.txt", "SendToS3:" + ex.StackTrace + "\r\n");
            }
            return false;
        }

        private static void SDBTimerProc(object state)
        {
            m_tSubmitSDB.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            try
            {
                Dictionary<string, Dictionary<string, ISendToSimpleDB>> dictDomainsToSubmissionList = new Dictionary<string, Dictionary<string, ISendToSimpleDB>>();
                do
                {
                    //
                    // ok, we'll just keep dequeueing until we've reached the bottom of the stack.
                    //
                    // if, at any point, one of our domains has hit 24 items (max for batchput),
                    // then we submit it!
                    //
                    ISendToSimpleDB t = null;
                    lock (m_arSDBQueue)
                    {
                        if (m_arSDBQueue.Count > 0)
                        {
                            t = m_arSDBQueue.Dequeue();
                            lock (m_arSDBDeletes)
                            {
                                if (m_dictSDBDeletes.ContainsKey(t.ItemName))
                                    t = null;
                            }
                        }
                        else t = null;
                    }


                    if (t != null)
                    {
                        if (!dictDomainsToSubmissionList.ContainsKey(t.Domain))
                            dictDomainsToSubmissionList.Add(t.Domain, new Dictionary<string, ISendToSimpleDB>());

                        if (dictDomainsToSubmissionList[t.Domain].ContainsKey(t.ItemName))
                            dictDomainsToSubmissionList[t.Domain][t.ItemName] = t;
                        else
                            dictDomainsToSubmissionList[t.Domain].Add(t.ItemName, t);


                        if (dictDomainsToSubmissionList[t.Domain].Count >= 24)
                        {
                            DoSubmitToSimpleDB(t.Domain, dictDomainsToSubmissionList[t.Domain].Values.ToList<ISendToSimpleDB>());
                            dictDomainsToSubmissionList[t.Domain].Clear();
                        }
                    }
                } while (m_arSDBQueue.Count > 0);

                foreach (string strKey in dictDomainsToSubmissionList.Keys)
                {
                    if (dictDomainsToSubmissionList[strKey].Count > 0)
                        DoSubmitToSimpleDB(strKey, dictDomainsToSubmissionList[strKey].Values.ToList<ISendToSimpleDB>());
                }

                // now, handle the deletes.
                do
                {
                    ISendToSimpleDB t = null;
                    lock (m_arSDBDeletes)
                    {
                        if (m_arSDBDeletes.Count > 0)
                        {
                            t = m_arSDBDeletes.Dequeue();
                            m_dictSDBDeletes.Remove(t.ItemName);
                        }
                        else t = null;
                    }

                    if (t != null)
                    {
                        DoDeleteFromSimpleDB(t.Domain, t.ItemName);
                    }
                }
                while (m_arSDBDeletes.Count > 0);
            }
            catch (Exception ex)
            {
            }

            m_tSubmitSDB.Change(3000,3000);
        }

        private static bool DoDeleteFromSimpleDB(string in_strDomain, string in_strItemName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("DeleteFromSimpleDB: " + in_strItemName);

                RetryUtility.RetryAction(15, 1000, () =>
                {
                    DeleteAttributesRequest dr = new DeleteAttributesRequest();
                    dr.DomainName = in_strDomain;
                    dr.ItemName = in_strItemName;
                    DeleteAttributesResponse drResp = DBManager.SimpleDBService.DeleteAttributes(dr);
                });
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                return false;
            }
            return true;
        }

        private static bool DoSubmitToSimpleDB(string in_strDomain, List<ISendToSimpleDB> arToSubmit)
        {
            try
            {
                RetryUtility.RetryAction(15, 1000, () =>
                    {
                        BatchPutAttributesRequest bpr = new BatchPutAttributesRequest();
                        bpr.DomainName = in_strDomain;
                        foreach (ISendToSimpleDB i in arToSubmit)
                        {
                            bpr.Item.Add(i.ToSimpleDBRequests());
                        }

                        System.Diagnostics.Debug.WriteLine("SubmitToSimpleDB: " + bpr.Item.Count);
                        BatchPutAttributesResponse bprResp = DBManager.SimpleDBService.BatchPutAttributes(bpr);
                    }
                );
                return true;
            }
            catch (Exception ex)
            {
                File.AppendAllText("D:/inetpub/nsono/nineplaces/SVC/App_Data/Logs/UPLOAD.txt", "SendToS3:" + ex.Message + "\r\n");
                File.AppendAllText("D:/inetpub/nsono/nineplaces/SVC/App_Data/Logs/UPLOAD.txt", "SendToS3:" + ex.StackTrace + "\r\n");
            }
            foreach (ISendToSimpleDB i in arToSubmit)
            {
                i.SentToSimpleDB = true;
                QueueManager.Submit(i);
            }
            return false;
        }
        
        private static void WorkTimerProc(object state)
        {
            m_tDoWork.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            try
            {
                IBackgroundWork toWork = null;
                lock (m_arBWQueue)
                {
                    if (m_arBWQueue.Count > 0)
                        toWork = m_arBWQueue.Dequeue();
                }

                while (toWork != null)
                {
                    // no retries.  it either fails or succeeds.
                    System.Diagnostics.Debug.WriteLine("DoWork");
                    toWork.DoWork();
                    toWork.WorkComplete = true;

                    // we RESUBMIT in case this object still has more work it needs done.
                    QueueManager.Submit(toWork);

                    lock (m_arBWQueue)
                    {
                        if (m_arBWQueue.Count > 0)
                            toWork = m_arBWQueue.Dequeue();
                        else
                            toWork = null;
                    }
                }
            }
            catch (Exception)
            { }


            m_tDoWork.Change(3000, 3000);
        }
    }

    public class GenerateThumbAndSendToS3 : IStreamSendToS3, IBackgroundWork
    {
        public bool QuietLogs { get { return false; } }
        private string UserName;
        private string ID;
        private string Path;
        private Res Resolution;
        private int MaxRes;

        private MemoryStream ResStream = new MemoryStream();
        public Stream ObjectStream 
        { 
            get
            {
                return ResStream;
            }
        }

        public enum Res
        {
            ThumbRes,
            MidRes, 
            FullRes
        }

        public GenerateThumbAndSendToS3(string in_strUserName, string in_strID, string in_strPath, Res in_eRes)
        {
            Path = in_strPath;
            ID = in_strID;
            UserName = in_strUserName;
            Resolution = in_eRes;

            string strFolder = "photo";
            MaxRes = 3000;
            if (Resolution == Res.ThumbRes)
            {
                strFolder = "thumbnail";
                MaxRes = 120;
            }
            else if (Resolution == Res.MidRes)
            {
                strFolder = "photo_midres";
                MaxRes = 1024;
            }

            DestinationKey = UserName + "/" + strFolder + "/" + ID ;
        }
        
        private MemoryStream GenerateRes(Stream in_sImageStream, double in_dMax)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                using (Image iOriginal = Image.FromStream( in_sImageStream ))
                {
                    if (iOriginal.Width <= in_dMax && iOriginal.Height <= in_dMax)
                    {
                        // copy the stream.
                        iOriginal.Save(ms, ImageFormat.Jpeg);
                    }
                    else
                    {
                        double dScaleWidth = iOriginal.Width > iOriginal.Height ? 1.0 : ((double)iOriginal.Width) / ((double)iOriginal.Height);
                        double dScaleHeight = iOriginal.Height > iOriginal.Width ? 1.0 : ((double)iOriginal.Height) / ((double)iOriginal.Width);

                        double dNewHeight = in_dMax * dScaleHeight;
                        double dNewWidth = in_dMax * dScaleWidth;

                        using (Bitmap bNew = new System.Drawing.Bitmap((int)dNewWidth + 1, (int)dNewHeight + 1))
                        {
                            using (Graphics grDraw = System.Drawing.Graphics.FromImage(bNew))
                            {
                                grDraw.DrawImage(iOriginal, new Rectangle(0, 0, (int)dNewWidth + 1, (int)dNewHeight + 1));
                                bNew.Save(ms, ImageFormat.Jpeg);
                            }
                        }
                    }
                }

                ms.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                File.AppendAllText("D:/inetpub/nsono/nineplaces/SVC/App_Data/Logs/UPLOAD.txt", "GenerateThumbnail:" + ex.Message + "\r\n");
                File.AppendAllText("D:/inetpub/nsono/nineplaces/SVC/App_Data/Logs/UPLOAD.txt", "GenerateThumbnail:" + ex.StackTrace + "\r\n");
            }
            return ms;
        }

        #region IBackgroundWork Members

        public bool DoWork()
        {
            using( MemoryStream ms = new MemoryStream() )
            {
                using( FileStream fs = new FileStream(Path, FileMode.Open) )
                {
                    byte[] bBuffer = new byte[10000];
                    int bytesRead, totalBytesRead = 0;
                    do
                    {
                        bytesRead = fs.Read(bBuffer, 0, bBuffer.Length);
                        if (bytesRead > 0)
                            ms.Write(bBuffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                    } while (bytesRead > 0);
                }


                ms.Seek(0, SeekOrigin.Begin);

                ResStream = GenerateRes(ms, MaxRes);
            }

            return true;
        }

        public bool WorkComplete { get; set; }

        #endregion

        #region ISendToS3 Members

        public string ContentType
        {
            get { return "image/jpeg"; }
        }

        private string Key;
        public string DestinationKey
        {
            get { return Key; }
            protected set
            { 
                Key = value;
            }
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
            get; set;
        }

        #endregion
    }

}