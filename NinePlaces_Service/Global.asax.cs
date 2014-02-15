using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.IO;
using System.Web.Hosting;
using System.Xml.Linq;
using System.Configuration;
using System.Net;

using PublicDomain.ZoneInfo;

namespace NinePlaces_Service
{
    public class Global : System.Web.HttpApplication
    {
        public static Dictionary<string, RegionDetails> Regions { get; private set; }
        public static List<CityDetails> Cities = new List<CityDetails>();

        protected void Application_Start(object sender, EventArgs e)
        {
            IPAddress oServer = IPAddress.Parse(ConfigurationManager.AppSettings["CacheServer"]);
            int nPort = Convert.ToInt32(11213);
            IPEndPoint oEndPoint = new IPEndPoint(oServer, nPort);
            DBManager.AddServer(oEndPoint);

            Database.LoadFiles( HostingEnvironment.ApplicationPhysicalPath + "App_Data\\tzdata");

            Regions = new Dictionary<string, RegionDetails>();

            string strPath = HostingEnvironment.ApplicationPhysicalPath + "App_Data\\LocationDB\\admin1Codes.txt";
            using (TextReader tr = new StreamReader(strPath))
            {
                string strLine = string.Empty;
                while (!string.IsNullOrEmpty(strLine = tr.ReadLine()))
                {
                    RegionDetails rd = new RegionDetails(strLine);  
                    Regions.Add(rd.Identifier, rd);
                }
            }

            strPath = HostingEnvironment.ApplicationPhysicalPath + "App_Data\\LocationDB\\cities1000.txt";
            using (TextReader tr = new StreamReader(strPath))
            {
                string strLine = string.Empty;
                while (!string.IsNullOrEmpty(strLine = tr.ReadLine()))
                {
                    CityDetails cd = new CityDetails(strLine);
                    Cities.Add(cd);
                }
            }

            TESTTimeZones();
        }

        public void TESTTimeZones()
        {
            List<CityDetails> arToRemove = new List<CityDetails>();
            foreach (CityDetails cd in Cities)
            {
                try
                {
                    Zone z = Database.GetZone(cd.TimeZone);
                }
                catch (Exception ex)
                {
                    arToRemove.Add(cd);
                    
                    System.Diagnostics.Debug.WriteLine("TZ missing: '" + cd.TimeZone + "' --- '" + cd.CityName +"'");
                }
            }

            foreach (CityDetails cdToRemove in arToRemove)
            {
                Cities.Remove(cdToRemove);
            }
        }

        public static XElement GetTimeZoneXml(string in_strTimeZone)
        {
            Zone z = Database.GetZone(in_strTimeZone);
            if( z != null )
            {
                return ZoneToXml(z);
            }

            return new XElement("error");
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }


        public static XElement ZoneToXml(Zone in_z)
        {
            List<DateTime> OffsetCutovers =
                in_z.GetCutoverWindows(
                    new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2020, 12, 31, 23, 59, 59, DateTimeKind.Utc));

            XElement xTimeZone = new XElement("TimeZone");

            XElement xCutovers = new XElement("Cutovers",
                    new XAttribute("StartDate", new DateTime(2005, 1, 1, 0, 0, 0, DateTimeKind.Local).ToString("yyyy-MM-ddTHH:mm:ss")),
                    new XAttribute("EndDate", new DateTime(2015, 12, 31, 23, 59, 59, DateTimeKind.Local).ToString("yyyy-MM-ddTHH:mm:ss")));

            foreach (DateTime dtCutover in OffsetCutovers)
            {
                xCutovers.Add(new XElement("Cutover",
                    dtCutover.ToString("yyyy-MM-ddTHH:mm:ssZ")));
            }

            in_z.GetUtcOffset(new DateTime(2005, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            xTimeZone.Add(
                new XElement("DSTOffset", in_z.GetUtcOffset(new DateTime(2005, 6, 1, 0, 0, 0, DateTimeKind.Utc)).TotalHours),
                new XElement("Offset", in_z.GetUtcOffset(new DateTime(2005, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalHours),
                new XElement("Identifier", in_z.Name),
                xCutovers);

            return xTimeZone;
        }
    }

    public class CityDetails
    {
        public string CityName { get; private set; }
        public long Population { get; private set; }
        public string UnicodeCityName { get; private set; }
        public string CountryCode { get; private set; }
        public string AdminCode { get; private set; }
        public string TimeZone { get; set; }
        public RegionDetails RegionDetails { get; private set; }

        public CityDetails(string in_strDetailText)
        {
            RegionDetails = null;
            string[] info = in_strDetailText.Split(new char[] { '\t' });
            string strGeoNameID = info[0];
            UnicodeCityName = info[1];
            CityName = info[2];
            string strAlternates = info[3];
            string strLat = info[4];
            string strLong = info[5];
            string strFeatureClass = info[6];
            string strFeatureCode = info[7];
            CountryCode = info[8];
            string strCC2 = info[9];
            AdminCode = info[10];
            string strAdmin2code = info[11];
            string strAdmin3code = info[12];
            string strAdmin4ode = info[13];
            string strPopulation = info[14];
            string strElevation = info[15];
            string strGTopo30 = info[16];
            TimeZone = info[17];
            string strMod = info[18];

            long lPop = 0;
            if (long.TryParse(strPopulation, out lPop))
                Population = lPop;
            else
                Population = 0;

            if (Global.Regions.ContainsKey(CountryCode + "." + AdminCode))
                RegionDetails = Global.Regions[CountryCode + "." + AdminCode];
        }

        public XElement ToXml()
        {
            string strCountry = CountryCode;
            RegionDetails rCountry;
            if (Global.Regions.TryGetValue(CountryCode + ".00", out rCountry))
            {
                strCountry = rCountry.DisplayName;
            }

            string strProv = AdminCode;
            if (RegionDetails != null)
                strProv = RegionDetails.DisplayName;
            
            XElement xResp = new XElement("City",
                new XElement("UnicodeName", UnicodeCityName),
                new XElement("CountryCode", strCountry),
                new XElement("CityName", CityName),
                new XElement("AdminRegion", strProv),
                new XElement("TimeZone",
                    new XElement("Identifier", TimeZone)),
                new XElement("Population", Population.ToString()));


            if (RegionDetails != null)
                xResp.Add(RegionDetails.ToXml());
                        
            return xResp;
        }

    }

    public class RegionDetails
    {
        public string Identifier { get; private set; }
        public string DisplayName { get; private set; }

        public RegionDetails(string in_strDetailText)
        {
            string[] info = in_strDetailText.Split(new char[] { '\t' });
            Identifier = info[0];
            DisplayName = info[1].Replace(" (general)", "");
        }

        public XElement ToXml()
        {
            return new XElement("Region", DisplayName);
        }
    }
}