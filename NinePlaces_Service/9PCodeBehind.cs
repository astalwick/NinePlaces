using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.ServiceModel.Web;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using Amazon.S3.Model;
using Amazon;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Text;

namespace NinePlaces_Service
{
    public class NinePlaces_CodeBehind
    {
        NameValueCollection appConfig = null;
        public NinePlaces_CodeBehind()
        {
            appConfig = ConfigurationManager.AppSettings;
            System.Net.ServicePointManager.CertificatePolicy = new TrustAllCertificatePolicy();
        }

        public string Ping(string in_strText, XmlElement in_xRequest)
        {
            return "pong";
        }

        private Stream Response(string in_strResponseText, string in_strContentType)
        {
            try
            {
                HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                HttpContext.Current.Response.ContentType = in_strContentType;
                WebOperationContext.Current.OutgoingResponse.ContentType = in_strContentType;
                MemoryStream ms = new MemoryStream();

                UTF8Encoding u = new UTF8Encoding();
                byte[] resp = u.GetBytes(in_strResponseText);
                ms.Write(resp, 0, resp.Length);
                ms.Seek(0, SeekOrigin.Begin);

                return ms;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
                return null;
            }
        }

        public Stream RenderDebugItinerary(string VacationID)
        {
            try
            {
                
                Authentication a = new Authentication();

                TimelineObject t = new TimelineObject(a, a.Auth.UserID, VacationID);
                t.QueryYourself();

                foreach (TimelineObject tChild in t.Children)
                {
                    tChild.QueryYourself();
                }

                //
                // unless i'm wrong, we should now have a full tree.
                //
                string strOutput = string.Empty;
                string strTimeZoneID = "America/Montreal";
                XElement xCurrentTimeZone = Global.GetTimeZoneXml( strTimeZoneID );
                Dictionary<int, DateTime> dictCutovers = CutoversFromXML( xCurrentTimeZone );
                double dOffset = OffsetFromXML(xCurrentTimeZone);
                double dDSTOffset = DSTOffsetFromXML(xCurrentTimeZone);
                IEnumerable <TimelineObject> arSortedObjects = from child in t.Children where child.Type == ObjectType.Icon || child.Type == ObjectType.Generic orderby child.Properties["DockTime"] select child ;
                foreach (TimelineObject tSortedChild in arSortedObjects)
                {
                    DateTime dt;
                    DateTime.TryParseExact(tSortedChild.Properties["DockTime"], "yyyy-MM-ddTHH:mm:ss.ff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt) ;

                    // get offset for this time:
                    dt = dt.AddHours(IsDST(dictCutovers, dOffset, dDSTOffset, dt) ? dDSTOffset : dOffset);

                    strOutput += dt.ToString("MMMM d yyyy, h:mm tt") + "-" + tSortedChild.Properties["IconType"] + "\r\n";

                    foreach (string strKey in tSortedChild.Properties.Keys)
                    {
                        strOutput += "\t" + strKey + " - " + tSortedChild.Properties[strKey] + "\r\n";
                    }

                    if( tSortedChild.Properties.ContainsKey("TimeZone") )
                    {
                        strTimeZoneID = tSortedChild.Properties["TimeZone"];
                        xCurrentTimeZone = Global.GetTimeZoneXml( strTimeZoneID );
                        dOffset = OffsetFromXML(xCurrentTimeZone);
                        dDSTOffset = DSTOffsetFromXML(xCurrentTimeZone);
                        dictCutovers = CutoversFromXML( xCurrentTimeZone );
                    }
                }


                return Response(strOutput, "text/plain");
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
                return null;
            }
        }

        public bool IsDST(Dictionary<int, DateTime> in_dictCutovers, double in_dOffset, double in_dDSTOffset, DateTime in_dtToCheck)
        {
            DateTime dtTest;
            if (!in_dictCutovers.TryGetValue(in_dtToCheck.Year, out dtTest) || in_dtToCheck < dtTest.AddHours(-in_dOffset) ||
                !in_dictCutovers.TryGetValue(-in_dtToCheck.Year, out dtTest) || in_dtToCheck > dtTest.AddHours(-in_dDSTOffset))
                return false;  // winter
            else
                return true;  // summer
        }

        public double OffsetFromXML(XElement xTimeZone)
        {
            return Convert.ToDouble(xTimeZone.Element("Offset").Value); 
        }
        public double DSTOffsetFromXML(XElement xTimeZone)
        {
            return Convert.ToDouble(xTimeZone.Element("DSTOffset").Value);
        }

        public Dictionary<int, DateTime> CutoversFromXML(XElement xTimeZone)
        {
            var cutovers = from x in xTimeZone.Element("Cutovers").Elements("Cutover") select x.Value;

            Dictionary<int, DateTime> dicttoRet = new Dictionary<int, DateTime>();
            foreach (var strValue in cutovers)
            {
                // now, can it be parsed into a date time?
                DateTime dtCutover = DateTime.Parse(strValue, null, System.Globalization.DateTimeStyles.AdjustToUniversal);

                int nYear = dtCutover.Year;
                if (dtCutover.Month >= 6)
                    nYear *= -1;

                // ok, it can.  if it's month > 6, then it must be an END dst cutover.
                // other wise, it marks the BEGINNING of DST
                if (!dicttoRet.ContainsKey(nYear))
                    dicttoRet.Add(nYear, dtCutover);
            }

            return dicttoRet;
        }

        public XElement GetObject(string UserID, string ObjectName, string ID)
        {
            try
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.CacheControl, "no-cache");
                Authentication a = new Authentication();

                TimelineObject t = new TimelineObject(a, UserID, ID);
                t.QueryYourself();
                return t.ToXml();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
                return null;     
            }
        }

        public void PutObject(string UserID, string ObjectName, string ID, XElement in_xToPut)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PutObject: " + ID);
                WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.CacheControl, "no-cache");
                Authentication a = new Authentication();
                if (!a.IsAuthenticated())
                {
                    Log.Warning("User is not authenticated for PutObject");
                    Helpers.SetResponseCode(HttpStatusCode.Forbidden); 
                    return;
                }

                Log.Verbose(in_xToPut.ToString());
                TimelineObject t = new TimelineObject(a, UserID);
                t.FromXml(in_xToPut);
                t.PutYourself();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
            }
        }

        public void DeleteObject(string UserID, string ObjectName, string ID)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("DeleteObject: " + ID);
                WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.CacheControl, "no-cache");
                Authentication a = new Authentication();
                if (!a.IsAuthenticated())
                {
                    Log.Warning("User is not authenticated for DeleteObject");
                    Helpers.SetResponseCode(HttpStatusCode.Forbidden);
                    return;
                }

                TimelineObject t = new TimelineObject(a, UserID, ID);
                t.DeleteYourself();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
            }
        }


        public XElement GetUserIDFromUserName(string UserName)
        {
            try
            {
                Log.Verbose("GetUserIDFromUserName = " + UserName);
                UserAccountMgr u = new UserAccountMgr();
                XElement xDoc = new XElement("UserID", u.LookupUserID(UserName));
                
                return xDoc;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
                return null;
            }

        }
        public XElement GetCities(string LocationSubstring)
        {
            try
            {
                Log.Verbose("GetCities = " + LocationSubstring);
                XElement xDoc = new XElement("Cities");
                var Results = (from city in Global.Cities where city.CityName.StartsWith(LocationSubstring, StringComparison.InvariantCultureIgnoreCase) orderby city.Population descending select city).Take(10);
                foreach (CityDetails c in Results)
                {
                    xDoc.Add(c.ToXml());
                }
                return xDoc;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
                return null;
            }
        }

        public XElement GetTimeZone(string TimeZone)
        {
            try
            {
                Log.Verbose("GetTimeZone = " + TimeZone);

                return Global.GetTimeZoneXml(TimeZone);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
                return null;
            }
        }
        
        public XElement VerifyAuthToken(string UserName, string AuthToken, string Password)
        {
            try
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.CacheControl, "no-cache");

                Authentication a;
                if (!string.IsNullOrEmpty(AuthToken))
                    a = new Authentication(AuthToken);
                else
                    a = new Authentication(UserName, Password);

                if (!a.IsAuthenticated())
                {
                    Log.Verbose("Auth token is NOT valid.");
                    Helpers.SetResponseCode(HttpStatusCode.Forbidden);
                    return null;
                }

                Log.Verbose("Auth token is valid - " + a.Auth.UserName + " logged in");

                return new XElement("Authentication",
                    new XElement("AuthToken", a.Auth.AuthToken),
                    new XElement("UserID", a.Auth.UserID),
                    new XElement("UserName", a.Auth.UserName),
                    new XElement("EMail", a.Auth.EMail));
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
                return null;
            }
        }

        public void EndSession(string UserID)
        {
            try
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.CacheControl, "no-cache");
                Authentication a = new Authentication();

                a.DestroyAuth();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
            }
        }

        public XElement GetAccountInfo(string UserID)
        {
            Helpers.SetResponseCode(HttpStatusCode.NotImplemented);
            return null;
        }

        public XElement CreateAccount(XmlElement in_xAccountInfo)
        {
            try
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.CacheControl, "no-cache");
                UserAccountMgr u = new UserAccountMgr();
                string strUserID = u.CreateUser(
                    in_xAccountInfo.SelectSingleNode("EMail").InnerText, 
                    in_xAccountInfo.SelectSingleNode("Name").InnerText, 
                    in_xAccountInfo.SelectSingleNode("Password").InnerText,
                    in_xAccountInfo.SelectSingleNode("HomeCity").InnerText,
                    in_xAccountInfo.SelectSingleNode("HomeProvince").InnerText,
                    in_xAccountInfo.SelectSingleNode("HomeCountry").InnerText,
                    in_xAccountInfo.SelectSingleNode("HomeTimeZone").InnerText
                    );
                if (string.IsNullOrEmpty(strUserID))
                {
                    Helpers.SetResponseCode(HttpStatusCode.BadRequest);
                    return new XElement("ERROR");
                }

                return new XElement("Account",
                    new XElement("UserID", strUserID));
            }
            catch (Exception ex)
            {
                Log.Exception(ex); ;
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
            }

            return new XElement("ERROR");
        }

        internal void SetImageData(string UserID, string ID, Stream in_sBlock)
        {
            try
            {
                Authentication a = new Authentication();
                if (!a.IsAuthenticated())
                {
                    Log.Warning("User " + UserID + " is not authenticated");
                    Helpers.SetResponseCode(HttpStatusCode.Forbidden);
                    return;
                }

                if (!Directory.Exists(appConfig["ProjectStorage"] + "/" + UserID + "/photo"))
                {
                    Log.Warning("Creating temporary folder " + appConfig["ProjectStorage"] + "/" + UserID + "/photo");
                    Directory.CreateDirectory(appConfig["ProjectStorage"] + "/" + UserID + "/photo");
                }

                FileStream fs = File.Open(appConfig["ProjectStorage"] + "/" + UserID + "/photo/" + ID, FileMode.Create, FileAccess.Write);
                if (fs == null)
                {
                    Log.Error("Could not open file for write: " + appConfig["ProjectStorage"] + "/" + UserID + "/photo/" + ID);
                    Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
                    return;
                }

                Log.Verbose("Persisting file to temp folder.");
                byte[] bBuffer = new byte[10000];
                int bytesRead, totalBytesRead = 0;
                do
                {
                    bytesRead = in_sBlock.Read(bBuffer, 0, bBuffer.Length);
                    if (bytesRead > 0)
                        fs.Write(bBuffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                } while (bytesRead > 0);
                fs.Flush();
                fs.Close();

                Log.Verbose("Enqueueing file for submission: " + appConfig["ProjectStorage"] + "/" + UserID + "/photo/" + ID);
                GenerateThumbAndSendToS3 gThumb = new GenerateThumbAndSendToS3(UserID, ID, appConfig["ProjectStorage"] + "/" + UserID + "/photo/" + ID, GenerateThumbAndSendToS3.Res.ThumbRes);
                GenerateThumbAndSendToS3 gMid = new GenerateThumbAndSendToS3(UserID, ID, appConfig["ProjectStorage"] + "/" + UserID + "/photo/" + ID, GenerateThumbAndSendToS3.Res.MidRes);
                GenerateThumbAndSendToS3 gFull = new GenerateThumbAndSendToS3(UserID, ID, appConfig["ProjectStorage"] + "/" + UserID + "/photo/" + ID, GenerateThumbAndSendToS3.Res.FullRes);
                QueueManager.Submit(gThumb as IBackgroundWork);
                QueueManager.Submit(gMid as IBackgroundWork);
                QueueManager.Submit(gFull as IBackgroundWork);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
            }
        }

        internal Stream GetImageDataRes(string UserID, string ID, string Res)
        {
            try
            {
                Authentication a = new Authentication();

                if (Res != "MidRes" && Res != "ThumbRes" && Res != "FullRes")
                {
                    Log.Verbose("Res not specified.  Returning MidRes.");
                    Res = "MidRes";
                }

                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Redirect;
                if( Res == "ThumbRes" )
                    WebOperationContext.Current.OutgoingResponse.Location = "http://usercontent.nineplaces.com.s3.amazonaws.com/" + UserID + "/thumbnail/" + ID;
                else if(Res == "FullRes")
                    WebOperationContext.Current.OutgoingResponse.Location = "http://usercontent.nineplaces.com.s3.amazonaws.com/" + UserID + "/photo/" + ID;
                else if (Res == "MidRes")
                    WebOperationContext.Current.OutgoingResponse.Location = "http://usercontent.nineplaces.com.s3.amazonaws.com/" + UserID + "/photo_midres/" + ID;

                return null;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(HttpStatusCode.InternalServerError);
            }
            return null;
        }
    }


}
