using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace NinePlaces_Service
{
    [ServiceContract]
    public interface I9P
    {

        /// <summary>
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "/1/List/{ListID}?AuthToken={AuthToken}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream RenderList(string ListID, string AuthToken);

        /// <summary>
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "/1/Itinerary/{VacationID}?AuthToken={AuthToken}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream RenderItinerary(string VacationID, string AuthToken);

        /// <summary>
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "/1/DEBUGItinerary/{VacationID}?AuthToken={AuthToken}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream RenderDebugItinerary(string VacationID, string AuthToken);

        /// <summary>
        /// Given a UserName, return that user's ID.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "/1/UserNameToUserID?q={UserName}", BodyStyle = WebMessageBodyStyle.Bare)]
        XElement GetUserIDFromUserName(string UserName);

        /// <summary>
        /// Given the start of a location string, return xml describing matching options.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "/1/Cities?q={CitySubstring}", BodyStyle = WebMessageBodyStyle.Bare)]
        XElement GetCities(string CitySubstring);

        /// <summary>
        /// Given the start of a location string, return xml describing matching options.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "/1/TimeZone?q={TimeZone}", BodyStyle = WebMessageBodyStyle.Bare)]
        XElement GetTimeZone(string TimeZone);

        /// <summary>
        /// Given a password, returns an authentication token to be used for subsequent communication.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "/1/Authentication?UserName={UserName}&AuthToken={AuthToken}&Password={Password}", BodyStyle = WebMessageBodyStyle.Bare)]
        XElement VerifyAuthToken(string UserName, string AuthToken, string Password);

        /// <summary>
        /// Deletes the object described by {ID}
        /// </summary>
        [OperationContract]
        [WebInvoke(Method = "DELETE", UriTemplate = "/1/{UserID}/Authentication", RequestFormat = WebMessageFormat.Xml)]
        void EndSession(string UserID);

        /// <summary>
        /// Creates a new user account 
        /// </summary>
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/1/CreateAccount", BodyStyle = WebMessageBodyStyle.Bare)]
        XElement CreateAccount(XmlElement in_xAccountInfo);

        /// <summary>
        /// Returns the contents of the object described by {ID}
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "/1/{UserID}/{ObjectName}/{ID}", BodyStyle = WebMessageBodyStyle.Bare)]
        XElement GetObject(string UserID, string ObjectName, string ID);

        /// <summary>
        /// Replaces the contents of the object described by {ID}
        /// </summary>
        [OperationContract]
        [WebInvoke(Method = "PUT", UriTemplate = "/1/{UserID}/{ObjectName}/{ID}", BodyStyle = WebMessageBodyStyle.Bare)]
        void PutObject(string UserID, string ObjectName, string ID, Stream in_xmlToPut);
        
        /// <summary>
        /// Deletes the object described by {ID}
        /// </summary>
        [OperationContract]
        [WebInvoke(Method = "DELETE", UriTemplate = "/1/{UserID}/{ObjectName}/{ID}", RequestFormat = WebMessageFormat.Xml)]
        void DeleteObject(string UserID, string ObjectName, string ID);
        
        /// <summary>
        /// Uploads a photo
        /// </summary>
        [OperationContract]
        [WebInvoke(Method = "PUT", UriTemplate = "/1/{UserName}/Photo/{ID}/Data", BodyStyle = WebMessageBodyStyle.Bare)]
        void SetImageData(string UserName, string ID, Stream in_sBlock);

        /// <summary>
        /// Downloads a photo at the given res.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "/1/{UserName}/Photo/{ID}/Data", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GetImageData(string UserName, string ID);

        /// <summary>
        /// Downloads a photo at the given res.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "/1/{UserName}/Photo/{ID}/Data/{Res}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GetImageDataRes(string UserName, string ID, string Res);


        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/1/Ping?text={in_strText}", BodyStyle = WebMessageBodyStyle.Bare)]
        string Ping(string in_strText, XmlElement in_xRequest);

        [OperationContract]
        [WebGet(UriTemplate = "/1/Ping", BodyStyle = WebMessageBodyStyle.Bare)]
        string GetPing();

        [OperationContract]
        [WebGet(UriTemplate = "/1/Logs?Limit={in_nLimit}&Severity={in_strSeverity}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GetLogs(string in_strSeverity, int in_nLimit);

        /// <summary>
        /// Creates a new user account 
        /// </summary>
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/1/ReportException", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream ReportException(Stream in_strMessage);
    }    
}
