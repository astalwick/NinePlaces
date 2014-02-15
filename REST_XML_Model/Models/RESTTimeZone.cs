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
using System.Collections.Generic;
using Common;

using System.Xml;
using System.Xml.Linq;

namespace NinePlaces.Models
{

    public class TimeZone : ITimeZone
    {


        /// <summary>
        ///          static dictionary container for all timezones.  timezones have only one instance, and as they're
        ///         queried, they get populated in here.
        /// </summary>
        private static Dictionary<string, ITimeZone> g_dictNameToTimeZone = new Dictionary<string, ITimeZone>();


        /// <summary>
        /// Given a timezone identifier, this will return a TimeZoneObj object - 
        /// creating it, if necessary.
        /// </summary>
        /// <param name="in_strName"></param>
        /// <returns></returns>
        public static ITimeZone GetTimeZone(string in_strName)
        {
            ITimeZone t;
            lock (g_dictNameToTimeZone)
            {
                if (g_dictNameToTimeZone.TryGetValue(in_strName, out t))
                    return t;
            }

            //
            // ah, we're new.
            //
            t = new TimeZone(in_strName);

            lock (g_dictNameToTimeZone)
            {
                g_dictNameToTimeZone.Add(in_strName, t);
                return t;
            }
        }

        internal static ITimeZone GetTimeZoneFromXML(XElement in_xTimeZoneXML)
        {
            string strName = in_xTimeZoneXML.Element("Identifier").Value;

            ITimeZone t;
            lock (g_dictNameToTimeZone)
            {
                if (g_dictNameToTimeZone.TryGetValue(strName, out t))
                    return t;
            }

            //
            // ah, we're new.
            //
            t = new TimeZone(in_xTimeZoneXML);

            lock (g_dictNameToTimeZone)
            {
                g_dictNameToTimeZone.Add(strName, t);
                return t;
            }
        }

        /// <summary>
        /// Have we gotten the DST info for this timezone?
        /// </summary>
        public bool Loaded { get; set; }

        /// <summary>
        /// Offset from UTC for DST
        /// </summary>
        public double DSTOffset { get; private set; }
        /// <summary>
        /// Offset from UTC for standard time.
        /// </summary>
        public double Offset { get; private set; }

        /// <summary>
        /// Timezone Identifier - eg, "America/Montreal"
        /// </summary>
        public string TimeZoneID { get; private set; }

        /// <summary>
        /// All DST *start* dates, keyed by year.
        /// </summary>
        public Dictionary<int, DateTime> DSTStartByYear { get; private set; }

        /// <summary>
        /// All DST *end* dates, keyed by year.
        /// </summary>
        public Dictionary<int, DateTime> DSTEndByYear { get; private set; }

        /// <summary>
        /// Event fired when new timezone info is retrieved from the 
        /// server (specifically, more DST info).
        /// </summary>
        public event EventHandler TimeZoneInfoUpdated;

        public TimeZone(string in_strTimeZoneIdentifier)
        {
            Loaded = false;
            TimeZoneID = in_strTimeZoneIdentifier;
            DSTOffset = 0.0;
            Offset = 0.0;
            DSTEndByYear = new Dictionary<int, DateTime>();
            DSTStartByYear = new Dictionary<int, DateTime>();
            FetchTimeZone();
        }

        internal TimeZone(XElement in_xTimeZoneXML)
        {
            DSTEndByYear = new Dictionary<int, DateTime>();
            DSTStartByYear = new Dictionary<int, DateTime>();
            ConstructFromXML(in_xTimeZoneXML);
        }

        public bool TimeZoneEquals(ITimeZone in_tzToCompare)
        {
            if (in_tzToCompare == null)
                return false;

            if (in_tzToCompare.TimeZoneID.CompareTo(TimeZoneID) == 0)       // if the timezoneid matches, that's good enough.
                return true;

            if (!Loaded || !in_tzToCompare.Loaded)
                return false;

            return (in_tzToCompare.DSTOffset == DSTOffset &&
                in_tzToCompare.Offset == Offset);
        }

        /// <summary>
        /// Go to the server to get more DST cutover info.
        /// Currently, the server only provides a slice.  If you need more, you have to ask.
        /// 
        /// NOTE: NOT IMPLEMENTED
        /// </summary>
        /// <param name="in_dtStart"></param>
        /// <param name="in_dtEnd"></param>
        public void FetchDSTCutoverRange(DateTime in_dtStart, DateTime in_dtEnd)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Downloads the timezone information from the server.
        /// </summary>
        public void FetchTimeZone()
        {
            if (Loaded)
                return;

            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);

            wc.DownloadStringAsync(new Uri(Helpers.GetAPIURL() +  "TimeZone?q=" + TimeZoneID.Replace("/", "%2F")));

        }

        private void ConstructFromXML(XElement in_xTZ)
        {
            // we care about offset, dstoffset, identifer, and cutovers.
            DSTOffset = Convert.ToDouble(in_xTZ.Element("DSTOffset").Value);
            Offset = Convert.ToDouble(in_xTZ.Element("Offset").Value);
            TimeZoneID = in_xTZ.Element("Identifier").Value;

            foreach (XElement x in in_xTZ.Element("Cutovers").Elements("Cutover"))
            {
                DateTime dtCutOver = DateTime.Parse(x.Value, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                // ok, it can.  if it's month > 6, then it must be an END dst cutover.
                // other wise, it marks the BEGINNING of DST
                if (dtCutOver.Month <= 6)
                    DSTStartByYear.TryAddValue(dtCutOver.Year, dtCutOver);
                else
                    DSTEndByYear.TryAddValue(dtCutOver.Year, dtCutOver);
            }
            Loaded = true;
        }

        /// <summary>
        /// DownloadComplete notification handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // oh noews.

                Log.WriteLine("DownloadString failed - DST info could not be retrieved for " + TimeZoneID, LogEventSeverity.Error);
                return;
            }

            XElement x = XElement.Parse(e.Result);
            ConstructFromXML(x);

            // We don't want to add a reference to Linq for no reason, so we use the 
            // bundled XmlReader.  Faster anyway...
            /*using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(e.Result)))
            {
                // iterate through the elements...
                while (reader.IsStartElement() || reader.Read())
                {
                    // we only care about start elements (open tags).
                    if (reader.IsStartElement())
                    {
                        if (reader.LocalName == "DSTOffset")
                        {
                            // aha, found the DST offset.
                            DSTOffset = reader.ReadElementContentAsDouble();
                        }
                        else if (reader.LocalName == "Offset")
                        {
                            // aha, found the standard tiem offset.
                            Offset = reader.ReadElementContentAsDouble();
                        }
                        else if (reader.LocalName == "Identifier")
                        {
                            // aha, found timezone identifier.  
                            string strID = reader.ReadElementContentAsString();
                            if (strID != TimeZoneID)
                            {
                                Log.WriteLine("Timezone mismatch - " + strID + " != " + TimeZoneID);

                                throw new Exception("Timezone mismatch");
                            }
                        }
                        else if (reader.LocalName == "Cutover")
                        {
                            // ok, we've found a dst cuutover.  read the string.
                            string strValue = reader.ReadElementContentAsString();
                            // now, can it be parsed into a date time?
                            DateTime dtCutOver = DateTime.Parse(strValue, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                            // ok, it can.  if it's month > 6, then it must be an END dst cutover.
                            // other wise, it marks the BEGINNING of DST
                            if (dtCutOver.Month <= 6)
                                DSTStartByYear.TryAddValue(dtCutOver.Year, dtCutOver);
                            else
                                DSTEndByYear.TryAddValue(dtCutOver.Year, dtCutOver);
                        }
                        else
                        {
                            if (!reader.Read())
                                break;
                        }
                    }
                }
            }*/

            Loaded = true;

            if (TimeZoneInfoUpdated != null)
                TimeZoneInfoUpdated.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Determines whether or not a given date range contains a DST change day. 
        /// Returns the direction (forward or backward) if so.
        /// </summary>
        /// <param name="in_dtStartTime"></param>
        /// <param name="in_dtEndTime"></param>
        /// <param name="in_bLocal"></param>
        /// <returns></returns>
        public DSTChangeDirection ContainsDSTChange(DateTime in_dtStartTime, DateTime in_dtEndTime, bool in_bLocal = false)
        {
            DateTime dtStart;
            if (DSTStartByYear.TryGetValue(in_dtStartTime.Year, out dtStart) && in_dtStartTime < dtStart.AddHours(in_bLocal ? 0 : -Offset) && in_dtEndTime >= dtStart.AddHours(in_bLocal ? 0 : -Offset))
                return DSTChangeDirection.DSTJumpForward;

            if (DSTEndByYear.TryGetValue(in_dtStartTime.Year, out dtStart) && in_dtStartTime < dtStart.AddHours(in_bLocal ? 0 : -DSTOffset) && in_dtEndTime >= dtStart.AddHours(in_bLocal ? 0 : -DSTOffset))
                return DSTChangeDirection.DSTJumpBackward;

            return DSTChangeDirection.DSTNone;
        }

        /// <summary>
        /// Given a time, returns the offset at that time (accounting for DST).
        /// </summary>
        /// <param name="in_dtTime"></param>
        /// <param name="in_bLocal"></param>
        /// <returns></returns>
        public double OffsetAtTime(DateTime in_dtTime, bool in_bLocal = false)
        {
            DateTime dtTest;

            // if we're going from winter to summer (Offset to DSTOffset), 
            // the date changes from 2AM to 3AM.  jumps one hour.
            // if we're going from summer to winter (DSTOffset to Offset)
            // the date changes from 2AM to 1AM.
            if (!DSTStartByYear.TryGetValue(in_dtTime.Year, out dtTest) || in_dtTime < dtTest.AddHours(in_bLocal ? 0 : -Offset) ||
                !DSTEndByYear.TryGetValue(in_dtTime.Year, out dtTest) || in_dtTime > dtTest.AddHours(in_bLocal ? 0 : -DSTOffset))
                return Offset;  // winter
            else
                return DSTOffset;  // summer
        }
    }
}
