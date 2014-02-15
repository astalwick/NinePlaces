using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Xml.Linq;
using System.Globalization;
using System.Web.Hosting;
using System.Text;

namespace NinePlaces_Service
{
    public interface IRenderableEvent
    {
        string RenderToHTML();
    }

    public class TimezoneObject
    {
        
        public Dictionary<int, DateTime> Cutovers { get; set; }
        public double Offset { get; set; }
        public double DSTOffset { get; set; }

        public TimezoneObject(string in_strID)
        {
            XElement xCurrentTimeZone = Global.GetTimeZoneXml(in_strID);
            Cutovers = CutoversFromXML(xCurrentTimeZone);
            Offset = OffsetFromXML(xCurrentTimeZone);
            DSTOffset = DSTOffsetFromXML(xCurrentTimeZone);
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

    }

    public class ItineraryObj
    {
        TimelineObject m_oVacation = null;
        
        public ItineraryObj( Authentication in_aAuth, string in_strVacationID )
        {
            m_oVacation = new TimelineObject(in_aAuth, in_aAuth.Auth.UserID, in_strVacationID);
        }

        List<IRenderableEvent> arEvents = new List<IRenderableEvent>();
        public void Load()
        {
            TimezoneObject tz = new TimezoneObject("America/Montreal");

            m_oVacation.QueryYourself();
            IEnumerable<TimelineObject> arSortedObjects = from child in m_oVacation.Children where child.Type == ObjectType.Icon || child.Type == ObjectType.Generic orderby child.Properties["DockTime"] select child;
            foreach (TimelineObject tChild in arSortedObjects)
            {
                tChild.QueryYourself();
                arEvents.Add(ConstructEventObject(tChild, tz));

                if (tChild.Properties.ContainsKey("TimeZone"))
                {
                    tz = new TimezoneObject(tChild.Properties["TimeZone"]);
                }
            }
        }


        public string RenderToHTML()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IRenderableEvent r in arEvents)
            {
                sb.Append(r.RenderToHTML());
            }

            string strToRender = File.ReadAllText(HostingEnvironment.ApplicationPhysicalPath + "App_Data\\HTML\\body.thtml");

            return strToRender.Replace("%%ITEMS%%", sb.ToString());
        }

        public IRenderableEvent ConstructEventObject(TimelineObject in_tObj, TimezoneObject in_tz)
        {
            switch (in_tObj.Properties["IconType"])
            {
                case "Beach":
                    return new BeachObject(in_tObj, in_tz);
                case "Nightlife":
                    return new NightlifeObject(in_tObj, in_tz);
                case "SportingEvent":
                    return new SportingEventObject(in_tObj, in_tz);
                case "Train":
                    return new TrainObject(in_tObj, in_tz);
                case "Bus":
                    return new BusObject(in_tObj, in_tz);
                case "Boat":
                    return new BoatObject(in_tObj, in_tz);
                case "Camping":
                    return new CampingObject(in_tObj, in_tz);
                case "Car":
                    return new CarObject(in_tObj, in_tz);
                case "Flight":
                    return new FlightObject(in_tObj, in_tz);
                case "GenericActivity":
                    return new GenericObject(in_tObj, in_tz);
                case "Hotel":
                    return new HotelObject(in_tObj, in_tz);
                case "House":
                    return new HouseObject(in_tObj, in_tz);
                case "MeetUp":
                    return new MeetUpObject(in_tObj, in_tz);
                case "Outdoor":
                    return new OutdoorObject(in_tObj, in_tz);
                case "Restaurant":
                    return new RestaurantObject(in_tObj, in_tz);
                case "Shopping":
                    return new ShoppingObject(in_tObj, in_tz);
                case "Show":
                    return new ShowObject(in_tObj, in_tz);
                case "Sightseeing":
                    return new SightseeingObject(in_tObj, in_tz);

            }

            throw new Exception("Unknown object being constructed in RenderItinerary --- " + in_tObj.Properties["IconType"]);
        }
    }

    public abstract class BaseObject : IRenderableEvent
    {
        protected List<string> m_arAttributes1 = new List<string>();
        protected List<string> m_arAttributes2 = new List<string>();
        protected List<string> m_arValues1 = new List<string>();
        protected List<string> m_arValues2 = new List<string>();

        protected TimelineObject m_tObject = null;
        protected TimelineObject IconObject { get { return m_tObject; } }
        protected Dictionary<string, string> Properties { get { return m_tObject.Properties; } }
        protected DateTime m_dtLocalTime;

        protected TimezoneObject m_tz = null;

        public BaseObject(TimelineObject in_tObject, TimezoneObject in_tz)
        {
            m_tObject = in_tObject;
            m_tz = in_tz;
        }

        public DateTime DatePropertyToUtcDate(string in_dt)
        {
            if (!IconObject.Properties.ContainsKey(in_dt))
                return DateTime.MinValue;
            DateTime dtRet;
            DateTime.TryParseExact(IconObject.Properties[in_dt], "yyyy-MM-ddTHH:mm:ss.ff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtRet);
            return dtRet;
        }


        public DateTime DatePropertyToLocalDate(string in_dt)
        {
            if (!IconObject.Properties.ContainsKey(in_dt))
                return DateTime.MinValue;
            DateTime dtRet = DatePropertyToUtcDate(in_dt);
            dtRet = dtRet.AddHours(m_tz.IsDST(m_tz.Cutovers, m_tz.Offset, m_tz.DSTOffset, dtRet) ? m_tz.DSTOffset : m_tz.Offset);
            return dtRet;
        }

        public virtual string EventName { get { return string.Empty; } }
        public DateTime Date
        {
            get
            {
                return DatePropertyToLocalDate("DockTime"); 
            }
        }

        TimezoneObject m_tzEnd = null;


        public virtual DateTime EndTime
        {
            get
            {
                TimeSpan tsDuration = new TimeSpan(6, 0, 0);
                string strDuration = Property("Duration");
                if (!string.IsNullOrEmpty(strDuration))
                {
                    TimeSpan.TryParse(strDuration, out tsDuration);
                }

                if (m_tzEnd == null)
                {
                    string strTimeZone = Property("TimeZone");
                    if (string.IsNullOrEmpty(strTimeZone))
                        m_tzEnd = m_tz;
                    else
                    {
                        m_tzEnd = new TimezoneObject(strTimeZone);
                    }
                }

                DateTime dtStart = DatePropertyToUtcDate("DockTime");
                DateTime dtEnd = dtStart.Add(tsDuration);
                dtEnd = dtEnd.AddHours(m_tzEnd.IsDST(m_tzEnd.Cutovers, m_tzEnd.Offset, m_tzEnd.DSTOffset, dtEnd) ? m_tzEnd.DSTOffset : m_tzEnd.Offset); ;

                return dtEnd;
            }
        }


        public DateTime LocalDateTime
        {
            get
            {
                return m_dtLocalTime;
            }
        }

        public virtual string Title
        {
            get
            {
                return string.Empty;
            }
        }

        public string Property(string in_strName)
        {
            if (Properties.ContainsKey(in_strName))
                return Properties[in_strName];
            return string.Empty;
        }
        public abstract void AssembleProps();

        public virtual string EventIconURL { get { return "Hotel.jpg"; } }
        public virtual string RenderToHTML()
        {
            AssembleProps();

            string strToRender = File.ReadAllText(HostingEnvironment.ApplicationPhysicalPath + "App_Data\\HTML\\eventitem.thtml");
            string strAttr = File.ReadAllText(HostingEnvironment.ApplicationPhysicalPath + "App_Data\\HTML\\attribute.thtml");
            string strValue = File.ReadAllText(HostingEnvironment.ApplicationPhysicalPath + "App_Data\\HTML\\value.thtml");

            string strToRet = strToRender.Replace("%%TITLE%%", Title).Replace("%%DATE%%", Date.ToString("MMMM d yyyy, h:mm tt")).Replace("%%ICONJPG%%", "/svc/images/" + EventIconURL); ;

            string strAttributes1 = string.Empty;
            foreach (string strAttribute in m_arAttributes1)
                strAttributes1 += strAttr.Replace("%%ATTRIBUTE%%", string.IsNullOrEmpty(strAttribute) ? "&nbsp;" : strAttribute);

            string strAttributes2 = string.Empty;
            foreach (string strAttribute in m_arAttributes2)
                strAttributes2 += strAttr.Replace("%%ATTRIBUTE%%", string.IsNullOrEmpty(strAttribute) ? "&nbsp;" : strAttribute);

            string strValues2 = string.Empty;
            foreach (string strVal in m_arValues2)
                strValues2 += strValue.Replace("%%VALUE%%", string.IsNullOrEmpty(strVal) ? "&nbsp;" : strVal);

            string strValues1 = string.Empty;
            foreach (string strVal in m_arValues1)
                strValues1 += strValue.Replace("%%VALUE%%", string.IsNullOrEmpty(strVal) ? "&nbsp;" : strVal);

            strToRet = strToRet.Replace("%%ATTRIBUTES1%%", strAttributes1)
                .Replace("%%ATTRIBUTES2%%", strAttributes2)
                .Replace("%%VALUES1%%", strValues1)
                .Replace("%%VALUES2%%", strValues2);

            return strToRet;
        }

        public virtual string Address1
        {
            get
            {
                if (Properties.ContainsKey("StreetAddress"))
                    return Properties["StreetAddress"];

                return string.Empty;
            }
        }

        public virtual string Address2
        {
            get
            {
                string strAddress2 = string.Empty;

                if (Properties.ContainsKey("City"))
                    strAddress2 = Properties["City"];

                if (Properties.ContainsKey("Province"))
                    strAddress2 += ", " + Properties["Province"];

                if (Properties.ContainsKey("PostalCode"))
                    strAddress2 += ", " + Properties["PostalCode"];

                return strAddress2;
            }
        }

        public virtual string Address3
        {
            get
            {
                if (Properties.ContainsKey("Country"))
                    return Properties["Country"];

                return string.Empty ;
            }
        }

    }

    public abstract class ActivityObject : BaseObject
    {
        public ActivityObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public string ActivityName
        {
            get
            {
                if (Properties.ContainsKey("ActivityName"))
                    return Properties["ActivityName"];
                return string.Empty; ;
            }
        }

        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("ActivityName"))
                    return EventName + ": " + Properties["ActivityName"];
                return EventName;
            }
        }
    }

    public abstract class TravelObject : BaseObject
    {
        public TravelObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }
    }

    public abstract class LodgingObject : BaseObject
    {
        public LodgingObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }


        public override string Title
        {
            get
            {
                if (!string.IsNullOrEmpty(LodgingName))
                    return EventName + ": " + LodgingName;
                return EventName;
            }

        }
        public virtual string LodgingName
        {
            get
            {
                if (Properties.ContainsKey("LodgingName"))
                    return Properties["LodgingName"];
                return string.Empty; 
            }
        }
    }
    
    public class BeachObject : ActivityObject
    {
        public BeachObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Beach.jpg"; } }
        public override string EventName { get { return "Beach"; } }

        public string BeachName
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return Properties["DestinationName"];
                return string.Empty;
            }
        }
        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Beach Name:");
            m_arAttributes1.Add("Time:");

            m_arValues1.Clear();
            m_arValues1.Add(BeachName);
            m_arValues1.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));

            m_arAttributes2.Clear();
            m_arAttributes2.Add("");

            m_arValues2.Clear();
            m_arValues2.Add("");
        }
    }


    public class SportingEventObject : ActivityObject
    {
        public SportingEventObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "SportingEvent.jpg"; } }
        public override string EventName { get { return "SportingEvent"; } }

        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("ActivityName"))
                    return EventName + ": " + Properties["ActivityName"];
                return EventName;
            }
        }


        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Event Type:");
            m_arAttributes1.Add("Playing:");
            m_arAttributes1.Add("Game Time:");

            m_arValues1.Clear();
            m_arValues1.Add(ActivityName);
            m_arValues1.Add(Property("SportingEventPlaying"));
            m_arValues1.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));

            m_arAttributes2.Clear();
            m_arAttributes2.Add("Address:");

            m_arValues2.Clear();
            m_arValues2.Add(Address1);
            m_arValues2.Add(Address2);
            m_arValues2.Add(Address3);
        }
    }



    public class NightlifeObject : ActivityObject
    {
        public NightlifeObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Nightlife.jpg"; } }
        public override string EventName { get { return "Nightlife"; } }

        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Location:");
            m_arAttributes1.Add("Time:");

            m_arValues1.Clear();
            m_arValues1.Add(Property("DestinationName"));
            m_arValues1.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));


            m_arAttributes2.Clear();
            m_arAttributes2.Add("Address:");

            m_arValues2.Clear();
            m_arValues2.Add(Address1);
            m_arValues2.Add(Address2);
            m_arValues2.Add(Address3);
        }
    }


    public class HotelObject : LodgingObject
    {
        public HotelObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Hotel.jpg"; } }
        public override string EventName { get { return "Hotel"; } }
        
        public string RoomNumber
        {
            get
            {
                if (IconObject.Properties.ContainsKey("RoomNumber"))
                    return IconObject.Properties["RoomNumber"];
                return string.Empty;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Hotel Name:");
            m_arAttributes1.Add("Check-in Time:");
            m_arAttributes1.Add("Check-out Time:");
            m_arAttributes1.Add("Room Number:");

            m_arValues1.Clear();
            m_arValues1.Add(LodgingName);
            m_arValues1.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));
            m_arValues1.Add(EndTime == DateTime.MinValue ? string.Empty : EndTime.ToString("MMMM d, h:mm tt"));
            m_arValues1.Add(RoomNumber);

            m_arAttributes2.Clear();
            m_arAttributes2.Add("Address:");

            m_arValues2.Clear();
            m_arValues2.Add(Address1);
            m_arValues2.Add(Address2);
            m_arValues2.Add(Address3);
        }
    }

    
    public class OutdoorObject : ActivityObject
    {
        public OutdoorObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Outdoor.jpg"; } }
        public override string EventName { get { return "Outdoor"; } }
        
        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Outdoor Activity:");
            m_arAttributes1.Add("Time:");

            m_arValues1.Clear();
            m_arValues1.Add(ActivityName);
            m_arValues1.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));

            m_arAttributes2.Clear();
            m_arAttributes2.Add("");

            m_arValues2.Clear();
            m_arValues2.Add("");
        }
    }

    
    public class ShowObject : ActivityObject
    {
        public ShowObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Show.jpg"; } }
        public override string EventName { get { return "Show"; } }
        
        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }



        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Show Name:");
            m_arAttributes1.Add("Show Time:");

            m_arValues1.Clear();
            m_arValues1.Add(ActivityName);
            m_arValues1.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));


            m_arAttributes2.Clear();
            m_arAttributes2.Add("Address:");

            m_arValues2.Clear();
            m_arValues2.Add(Address1);
            m_arValues2.Add(Address2);
            m_arValues2.Add(Address3);
        }
    }

    
    public class SightseeingObject : ActivityObject
    {
        public SightseeingObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Sightseeing.jpg"; } }
        public override string EventName { get { return "Sightseeing"; } }
        
        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Sightseeing Location:");
            m_arAttributes1.Add("Time:");


            m_arValues1.Clear();
            m_arValues1.Add(Property("DestinationName"));
            m_arValues1.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));


            m_arAttributes2.Clear();
            m_arAttributes2.Add("");

            m_arValues2.Clear();
            m_arValues2.Add("");
        }
    }

    
    public class MeetUpObject : ActivityObject
    {
        public MeetUpObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "MeetUp.jpg"; } }
        public override string EventName { get { return "Meet-Up"; } }
        
        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Meet-Up With:" );
            m_arAttributes1.Add("Time:");


            m_arValues1.Clear();
            m_arValues1.Add(Property("Person"));
            m_arValues1.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));


            m_arAttributes2.Clear();
            m_arAttributes2.Add("Address:");

            m_arValues2.Clear();
            m_arValues2.Add(Address1);
            m_arValues2.Add(Address2);
            m_arValues2.Add(Address3);
        }
    }

    
    public class RestaurantObject : ActivityObject
    {
        public RestaurantObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Restaurant.jpg"; } }
        public override string EventName { get { return "Restaurant"; } }
        
        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Restaurant Name:");
            m_arAttributes1.Add("Reservation Time:");

            m_arValues1.Clear();
            m_arValues1.Add(Property("DestinationName"));
            m_arValues1.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));


            m_arAttributes2.Clear();
            m_arAttributes2.Add("Address:");

            m_arValues2.Clear();
            m_arValues2.Add(Address1);
            m_arValues2.Add(Address2);
            m_arValues2.Add(Address3);
        }
    }


    public class ShoppingObject : ActivityObject
    {
        public ShoppingObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Shopping.jpg"; } }
        public override string EventName { get { return "Shopping"; } }
        
        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Shopping Location:");

            m_arValues1.Clear();
            m_arValues1.Add(Property("DestinationName"));


            m_arAttributes2.Clear();
            m_arAttributes2.Add("Address:");

            m_arValues2.Clear();
            m_arValues2.Add(Address1);
            m_arValues2.Add(Address2);
            m_arValues2.Add(Address3);
        }
    }


    public class CampingObject : LodgingObject
    {
        public CampingObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Camping.jpg"; } }
        public override string EventName { get { return "Camping"; } }

        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public string SiteNumber
        {
            get
            {
                if (Properties.ContainsKey("SiteNumber"))
                    return  Properties["SiteNumber"];
                return string.Empty; ;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Camping Location:");
            m_arAttributes1.Add("Arrival Time:");
            m_arAttributes1.Add("Departure Time:");

            m_arValues1.Clear();
            m_arValues1.Add(LodgingName);
            m_arValues1.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));
            m_arValues1.Add(EndTime == DateTime.MinValue ? string.Empty : EndTime.ToString("MMMM d, h:mm tt"));

            m_arAttributes2.Clear();
            m_arAttributes2.Add("Site Number:");

            m_arValues2.Clear();
            m_arValues2.Add(SiteNumber);
        }
    }

    public class HouseObject : LodgingObject
    {
        public HouseObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "House.jpg"; } }
        public override string EventName { get { return "House"; } }

        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("House Name:");
            m_arAttributes1.Add("Arrival Time:");
            m_arAttributes1.Add("Departure Time:");
            
            m_arValues1.Clear();
            m_arValues1.Add(LodgingName);
            m_arValues1.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));
            m_arValues1.Add(EndTime == DateTime.MinValue ? string.Empty : EndTime.ToString("MMMM d, h:mm tt"));
            
            m_arAttributes2.Clear();
            m_arAttributes2.Add("Address:");

            m_arValues2.Clear();
            m_arValues2.Add(Address1);
            m_arValues2.Add(Address2);
            m_arValues2.Add(Address3);
        }
    }

    public class FlightObject : TravelObject
    {
        public FlightObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Flight.jpg"; } }
        public override string EventName { get { return "Flight"; } }

        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Departure City:");
            m_arAttributes1.Add("Arrival City:");
            m_arAttributes1.Add("");
            m_arAttributes1.Add("Airline:");

            m_arValues1.Clear();
            m_arValues1.Add(Property("DepartureCity"));
            m_arValues1.Add(Property("ArrivalCity"));
            m_arValues1.Add("");
            m_arValues1.Add(Property("Airline"));

            m_arAttributes2.Clear();
            m_arAttributes2.Add("Departure Time:");
            m_arAttributes2.Add("Arrival Time:");
            m_arAttributes2.Add("");
            m_arAttributes2.Add("Flight Number:");

            m_arValues2.Clear();
            m_arValues2.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));
            m_arValues2.Add(EndTime == DateTime.MinValue ? string.Empty : EndTime.ToString("MMMM d, h:mm tt"));
            m_arValues2.Add("");
            m_arValues2.Add(Property("FlightNumber"));

        }
    }

    public class CarObject : TravelObject
    {
        public CarObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Car.jpg"; } }
        public override string EventName { get { return "Road-trip"; } }

        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }


        public virtual string DestinationAddress1
        {
            get
            {
                if (Properties.ContainsKey("ArrivalStreetAddress"))
                    return Properties["ArrivalStreetAddress"];

                return string.Empty;
            }
        }

        public virtual string DestinationAddress2
        {
            get
            {
                string strAddress2 = string.Empty;

                if (Properties.ContainsKey("ArrivalCity"))
                    strAddress2 = Properties["ArrivalCity"];

                if (Properties.ContainsKey("ArrivalProvince"))
                    strAddress2 += ", " + Properties["ArrivalProvince"];

                if (Properties.ContainsKey("ArrivalPostalCode"))
                    strAddress2 += ", " + Properties["ArrivalPostalCode"];

                return strAddress2;
            }
        }

        public virtual string DestinationAddress3
        {
            get
            {
                if (Properties.ContainsKey("ArrivalCountry"))
                    return Properties["ArrivalCountry"];

                return string.Empty;
            }
        }

        public override void AssembleProps()
        {

            m_arAttributes1.Clear();
            m_arAttributes1.Add("Departure City:");
            m_arAttributes1.Add("Arrival City:");

            m_arValues1.Clear();
            m_arValues1.Add(Property("DepartureCity"));
            m_arValues1.Add(Property("ArrivalCity"));

            m_arAttributes2.Clear();
            m_arAttributes2.Add("Departure Time:");
            m_arAttributes2.Add("Arrival Time:");
            m_arAttributes2.Add("Destination Address:");
            m_arAttributes2.Add("");
            m_arAttributes2.Add("");

            m_arValues2.Clear();
            m_arValues2.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));
            m_arValues2.Add(EndTime == DateTime.MinValue ? string.Empty : EndTime.ToString("MMMM d, h:mm tt"));
            m_arValues2.Add(DestinationAddress1);
            m_arValues2.Add(DestinationAddress2);
            m_arValues2.Add(DestinationAddress3);
        }
    }


    public class TrainObject : TravelObject
    {
        public TrainObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Train.jpg"; } }
        public override string EventName { get { return "Train"; } }

        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }


        public virtual string DestinationAddress1
        {
            get
            {
                if (Properties.ContainsKey("ArrivalStreetAddress"))
                    return Properties["ArrivalStreetAddress"];

                return string.Empty;
            }
        }

        public virtual string DestinationAddress2
        {
            get
            {
                string strAddress2 = string.Empty;

                if (Properties.ContainsKey("ArrivalCity"))
                    strAddress2 = Properties["ArrivalCity"];

                if (Properties.ContainsKey("ArrivalProvince"))
                    strAddress2 += ", " + Properties["ArrivalProvince"];

                if (Properties.ContainsKey("ArrivalPostalCode"))
                    strAddress2 += ", " + Properties["ArrivalPostalCode"];

                return strAddress2;
            }
        }

        public virtual string DestinationAddress3
        {
            get
            {
                if (Properties.ContainsKey("ArrivalCountry"))
                    return Properties["ArrivalCountry"];

                return string.Empty;
            }
        }

        public override void AssembleProps()
        {

            m_arAttributes1.Clear();
            m_arAttributes1.Add("Departure City:");
            m_arAttributes1.Add("Arrival City:");

            m_arValues1.Clear();
            m_arValues1.Add(Property("DepartureCity"));
            m_arValues1.Add(Property("ArrivalCity"));

            m_arAttributes2.Clear();
            m_arAttributes2.Add("Departure Time:");
            m_arAttributes2.Add("Arrival Time:");
            m_arAttributes2.Add("Destination Address:");
            m_arAttributes2.Add("");
            m_arAttributes2.Add("");

            m_arValues2.Clear();
            m_arValues2.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));
            m_arValues2.Add(EndTime == DateTime.MinValue ? string.Empty : EndTime.ToString("MMMM d, h:mm tt"));
            m_arValues2.Add(DestinationAddress1);
            m_arValues2.Add(DestinationAddress2);
            m_arValues2.Add(DestinationAddress3);
        }
    }


    public class BusObject : TravelObject
    {
        public BusObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Bus.jpg"; } }
        public override string EventName { get { return "Bus"; } }

        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }


        public virtual string DestinationAddress1
        {
            get
            {
                if (Properties.ContainsKey("ArrivalStreetAddress"))
                    return Properties["ArrivalStreetAddress"];

                return string.Empty;
            }
        }

        public virtual string DestinationAddress2
        {
            get
            {
                string strAddress2 = string.Empty;

                if (Properties.ContainsKey("ArrivalCity"))
                    strAddress2 = Properties["ArrivalCity"];

                if (Properties.ContainsKey("ArrivalProvince"))
                    strAddress2 += ", " + Properties["ArrivalProvince"];

                if (Properties.ContainsKey("ArrivalPostalCode"))
                    strAddress2 += ", " + Properties["ArrivalPostalCode"];

                return strAddress2;
            }
        }

        public virtual string DestinationAddress3
        {
            get
            {
                if (Properties.ContainsKey("ArrivalCountry"))
                    return Properties["ArrivalCountry"];

                return string.Empty;
            }
        }

        public override void AssembleProps()
        {

            m_arAttributes1.Clear();
            m_arAttributes1.Add("Departure City:");
            m_arAttributes1.Add("Arrival City:");

            m_arValues1.Clear();
            m_arValues1.Add(Property("DepartureCity"));
            m_arValues1.Add(Property("ArrivalCity"));

            m_arAttributes2.Clear();
            m_arAttributes2.Add("Departure Time:");
            m_arAttributes2.Add("Arrival Time:");
            m_arAttributes2.Add("Destination Address:");
            m_arAttributes2.Add("");
            m_arAttributes2.Add("");

            m_arValues2.Clear();
            m_arValues2.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));
            m_arValues2.Add(EndTime == DateTime.MinValue ? string.Empty : EndTime.ToString("MMMM d, h:mm tt"));
            m_arValues2.Add(DestinationAddress1);
            m_arValues2.Add(DestinationAddress2);
            m_arValues2.Add(DestinationAddress3);
        }
    }


    public class BoatObject : TravelObject
    {
        public BoatObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public override string EventIconURL { get { return "Boat.jpg"; } }
        public override string EventName { get { return "Boat"; } }

        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("Departure City:");
            m_arAttributes1.Add("Arrival City:");

            m_arValues1.Clear();
            m_arValues1.Add(Property("DepartureCity"));
            m_arValues1.Add(Property("ArrivalCity"));

            m_arAttributes2.Clear();
            m_arAttributes2.Add("Departure Time:");
            m_arAttributes2.Add("Arrival Time:");


            m_arValues2.Clear();
            m_arValues2.Add(Date == DateTime.MinValue ? string.Empty : Date.ToString("MMMM d, h:mm tt"));
            m_arValues2.Add(EndTime == DateTime.MinValue ? string.Empty : EndTime.ToString("MMMM d, h:mm tt"));

        }
    }


    public class GenericObject : BaseObject
    {
        public GenericObject(TimelineObject in_tObject, TimezoneObject in_tz)
            : base(in_tObject, in_tz)
        {
        }

        public string CurrentClass { get { return Property("CurrentClass"); } }
        public override string EventIconURL { get { return "Generic_Activity.jpg"; } }
        public override string EventName { get { return "Generic"; } }

        public override string Title
        {
            get
            {
                if (Properties.ContainsKey("DestinationName"))
                    return EventName + ": " + Properties["DestinationName"];
                return EventName;
            }
        }

        public override void AssembleProps()
        {
            m_arAttributes1.Clear();
            m_arAttributes1.Add("");

            m_arValues1.Clear();
            m_arValues1.Add("");

            m_arAttributes2.Clear();
            m_arAttributes2.Add("");

            m_arValues2.Clear();
            m_arValues2.Add("");
        }
    }
}