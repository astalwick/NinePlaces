using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Activation;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Configuration;
using System.ServiceModel.Channels;
using System.Web;
using System.ServiceModel.Web;
using System.Collections.Specialized;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Amazon;
using System.Net;

namespace NinePlaces_Service
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class NinePlaces : I9P
    {
        public NinePlaces()
        {
            HttpContext.Current.Items["Log"] = new Log();
        }

        #region TypeMapper
        public class MyMapper : WebContentTypeMapper
        {
            public override WebContentFormat GetMessageFormatForContentType(string contentType)
            {
                return WebContentFormat.Raw; // always
            }
        }
        static Binding GetBinding()
        {
            CustomBinding result = new CustomBinding(new WebHttpBinding());
            WebMessageEncodingBindingElement webMEBE = result.Elements.Find<WebMessageEncodingBindingElement>();
            webMEBE.ContentTypeMapper = new MyMapper();
            return result;
        }
        #endregion

        NinePlaces_CodeBehind m_oImpl = new NinePlaces_CodeBehind();
        public string Ping(string in_strText, XmlElement in_xRequest)
        {
            return m_oImpl.Ping(in_strText, in_xRequest);
        }

        public string GetPing()
        {
            return "pong";
        }

        public Stream RenderList(string ListID, string AuthToken)
        {
            try
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.CacheControl, "no-cache");
                Message request = OperationContext.Current.RequestContext.RequestMessage;
                HttpRequestMessageProperty prop = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];
                prop.Headers.Add("X-NinePlaces-Auth", AuthToken);

                Authentication a = new Authentication();

                // we're authenticated.  nice.
                ListObj l = new ListObj(a, ListID);
                l.Load();
                return Response(l.RenderToHTML(), "text/html");
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
                return null;
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public Stream RenderItinerary(string VacationID, string AuthToken)
        {
            try
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.CacheControl, "no-cache");
                Message request = OperationContext.Current.RequestContext.RequestMessage;
                HttpRequestMessageProperty prop = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];
                prop.Headers.Add("X-NinePlaces-Auth", AuthToken);

                Authentication a = new Authentication();

                // we're authenticated.  nice.
                ItineraryObj i = new ItineraryObj(a, VacationID);
                i.Load();
                return Response( i.RenderToHTML(), "text/html");
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
                return null;
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public Stream RenderDebugItinerary(string VacationID, string AuthToken)
        {
            try
            {
                Message request = OperationContext.Current.RequestContext.RequestMessage;
                HttpRequestMessageProperty prop = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];
                prop.Headers.Add("X-NinePlaces-Auth", AuthToken);

                // we're authenticated.  nice.
                return m_oImpl.RenderDebugItinerary(VacationID) ;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
                return null;
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public XElement GetObject(string UserID, string ObjectName, string ID)
        {
            try
            {
                Log.Verbose("GetObject - [UserID: " + UserID + "] [ObjectName: " + ObjectName + "] [ID: " + ID + "]");
                return m_oImpl.GetObject(UserID, ObjectName, ID);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
                return null;
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public void PutObject(string UserID, string ObjectName, string ID, Stream in_xToPut)
        {
            try
            {

                Log.Verbose("PutObject - [UserID: " + UserID + "] [ObjectName: " + ObjectName + "] [ID: " + ID + "]");
                XElement x = null;
                TextReader s = new StreamReader(in_xToPut);
                x = XElement.Parse(s.ReadToEnd());

                m_oImpl.PutObject(UserID, ObjectName, ID, x);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public void DeleteObject(string UserID, string ObjectName, string ID)
        {
            try
            {
                Log.Verbose("DeleteObject - [UserName: " + UserID + "] [ObjectName: " + ObjectName + "] [ID: " + ID + "]");
                m_oImpl.DeleteObject(UserID, ObjectName, ID);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public XElement VerifyAuthToken(string UserName, string AuthToken, string Password)
        {
            try
            {
                Log.Verbose("VerifyAuthToken");
                return m_oImpl.VerifyAuthToken(UserName, AuthToken, Password);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
                return null;
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public void EndSession(string UserName)
        {
            try
            {
                Log.Verbose("EndSession");
                m_oImpl.EndSession(UserName);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
            }
            finally
            {
                Log.SubmitLog();
            }
        }
        
        public XElement GetAccountInfo(string UserName)
        {
            try
            {
                Log.Verbose("GetObject - [UserName: " + UserName + "]");
                return m_oImpl.GetAccountInfo(UserName);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
                return null;
            }
            finally
            {
                Log.SubmitLog();
            }
        }


        public Stream ReportException(Stream in_strLogEntry)
        {
            try
            {
                Log.Verbose("Client reported exception: ");
                Response("ok cowboy, thanks", "text/plain");
                string strLog = string.Empty;
                using (TextReader tr = new StreamReader(in_strLogEntry))
                {

                    strLog = tr.ReadToEnd();
                }
                Log.CriticalError(strLog);
            }
            catch (Exception)
            {
            }
            finally
            {
                Log.SubmitLog();
            }

            return Response("no deal, sorry", "text/plain");
        }

        public XElement CreateAccount(XmlElement in_xAccountInfo)
        {
            try
            {
                Log.Verbose("CreateAccount");
                return m_oImpl.CreateAccount(in_xAccountInfo);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
            }
            finally
            {
                Log.SubmitLog();
            }

            return new XElement("ERROR");
        }

        public void SetImageData(string UserName, string ID,  Stream in_sBlock)
        {
            try
            {
                Log.Verbose("GetObject - [UserName: " + UserName + "] [ID: " + ID + "] ");
                m_oImpl.SetImageData(UserName, ID, in_sBlock);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public Stream GetImageData(string UserName, string ID)
        {
            return GetImageDataRes(UserName, ID, null);
        }

        public Stream GetImageDataRes(string UserName, string ID, string Res)
        {
            try
            {
                Log.Verbose("GetImageDataRes - [UserName: " + UserName + "] [ID: " + ID + "] [Res: " + Res + "]");
                return m_oImpl.GetImageDataRes(UserName, ID, Res);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
                return null;
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public XElement GetUserIDFromUserName(string UserName)
        {
            try
            {
                Log.Verbose("GetUserIDFromUserName - [UserName: " + UserName + "]");
                return m_oImpl.GetUserIDFromUserName(UserName);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
                return null;
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public XElement GetTimeZone(string TimeZone)
        {
            try
            {
                Log.Verbose("GetTimeZone - [TimeZone: " + TimeZone + "]");
                return m_oImpl.GetTimeZone(TimeZone);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
                return null;
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public XElement GetCities(string LocationSubstring)
        {
            try
            {
                Log.Verbose("GetCities - [LocationSubstring: " + LocationSubstring +"]");
                return m_oImpl.GetCities(LocationSubstring);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Helpers.SetResponseCode(System.Net.HttpStatusCode.InternalServerError);
                return null;
            }
            finally
            {
                Log.SubmitLog();
            }
        }

        public Stream GetLogs()
        {
            return GetLogs("Verbose", 500);
        }


        public Stream GetLogs(string in_strSeverity, int in_nLimit)
        {
            StringBuilder sbToRet = new StringBuilder();
            sbToRet.Append("<html><head></head><body><table>");

            NameValueCollection appConfig = ConfigurationManager.AppSettings;
            AmazonSimpleDBConfig s3Config = new AmazonSimpleDBConfig();
            s3Config.UseSecureStringForAwsSecretKey = false;
            AmazonSimpleDB sdb = AWSClientFactory.CreateAmazonSimpleDBClient(
                appConfig["AWSAccessKey"],
                appConfig["AWSSecretKey"]
                , s3Config 
                );


            LogSeverity lsSelect;
            Enum.TryParse(in_strSeverity, out lsSelect);

            string selectExpression = "Select * From NPLogs Where Severity>='" + SimpleDBHelpers.ValueToSimpleDB(((int)lsSelect).ToString()) + "' and  DateTime > '" + DateTime.UtcNow.AddDays(-70).ToString("yyyy-MM-ddTHH:mm:ssZ") + "' order by DateTime desc limit " + in_nLimit;
            SelectRequest selectRequestAction = new SelectRequest().WithSelectExpression(selectExpression);
            SelectResponse selectResponse = sdb.Select(selectRequestAction);

            
            foreach (Item i in selectResponse.SelectResult.Item)
            {
                string strLogDateTime = string.Empty;
                string strSeverity = "Undefined";
                string strID = string.Empty;
                DateTime dt = DateTime.UtcNow;;
                foreach (Amazon.SimpleDB.Model.Attribute a in i.Attribute)
                {
                    if (a.Name == "ID")
                        strID = a.Value;
                    else if (a.Name == "Severity")
                    {
                        strSeverity = SimpleDBHelpers.ValueFromSimpleDB(a.Value);
                    }
                    else if (a.Name == "DateTime")
                    {
                        
                        DateTime.TryParse(a.Value, out dt);
                        dt = dt.ToUniversalTime();
                    }
                }

                LogSeverity ls;
                Enum.TryParse(strSeverity, out ls);

                sbToRet.Append("<tr><td>" + dt.ToShortDateString() + " - " + dt.ToShortTimeString() + "</td><td>" + ls.ToString() + "</td><td><a href=\"http://logs.nineplaces.com/" + dt.ToString("yyyy-MM-dd") + "/" + strID + "\">" + strID + "</a></td></tr>");
            }
            sbToRet.Append("</table></html>");

            return Response(sbToRet.ToString(), "text/html");
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
            }
            return Response("failed", "text/plain");
        }
    }
}