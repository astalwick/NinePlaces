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
using NinePlaces.Models.REST;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace NinePlaces.ViewModels
{

    public class WhereAmIElement : IWhereAmIElement
    {
        public DateTime Start { get; set; }
        public string Location { get; set; }
        public int UniqueID { get; set; }

        public ITimelineElementModel Model { get; set; }
        public WhereAmIElement( ITimelineElementModel in_oModel)
        {
            Model = in_oModel;
            Update();
        }

        public bool Update()
        {
            bool bChanged = false;
            UniqueID = Model.UniqueID;

            DateTime dtStart;
            if (DateTime.TryParseExact(Model.GetProperty("DockTime"), "yyyy-MM-ddTHH:mm:ss.ff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtStart))
            {
                if (Start != dtStart)
                {
                    bChanged = true;
                    Start = dtStart;
                }
            }

            string strLocation = Location;

            if (Model.HasProperty("ArrivalCity") && !string.IsNullOrEmpty(Model.GetProperty("ArrivalCity")))
                Location = Model.GetProperty("ArrivalCity");

            if (string.IsNullOrEmpty(Location) && Model.HasProperty("City") && !string.IsNullOrEmpty(Model.GetProperty("City")))
                Location = Model.GetProperty("City");

            // fall back to destination name.
            if (string.IsNullOrEmpty(Location) && Model.HasProperty("DestinationName") && !string.IsNullOrEmpty(Model.GetProperty("DestinationName")))
                Location = Model.GetProperty("DestinationName");

            if (strLocation != Location)
                bChanged = true;
            
            return bChanged;
        }
    }

    public class WhereAmIViewModel : IWhereAmIViewModel
    {
        public event EventHandler WhereAmIChanged;

        public List<IWhereAmIElement> Elements
        {
            get
            {
                return (from u in m_arSortedByTime select u as IWhereAmIElement).ToList();
            }
        }

        private List<WhereAmIElement> m_arSortedByTime = new List<WhereAmIElement>();
        private Dictionary<int, WhereAmIElement> m_dictRegisteredElements = new Dictionary<int, WhereAmIElement>();
        private ITimelineElementModel Model = null;
        private int m_nVacationUniqueID = 0;
        public WhereAmIViewModel(int in_nVacationUniqueID)
        {
            m_nVacationUniqueID = in_nVacationUniqueID;
            App.AssemblyLoadManager.LoadXap("REST_XML_Model.xap", new EventHandler(wcDownloadXap_OpenReadCompleted));
        }

        void wcDownloadXap_OpenReadCompleted(object sender, EventArgs e)
        {
            InitializeModel();
        }

        public void InitializeModel()
        {
            if (Model == null)
            {
                Model = RESTTimelineModel.RootTimelineModel.FindDescendentModel(m_nVacationUniqueID);
                Model.ChildGroupChanged += new PropertyGroupChangedEventHandler(Model_ChildGroupChanged);
                if (Model.Children.ContainsKey("Icon") && Model.Children["Icon"] != null && Model.Children["Icon"].Count >= 1)
                {
                    IEnumerable<ITimelineElementModel> arModels = Model.Children["Icon"].Cast<ITimelineElementModel>();
                    bool bResort = false;
                    foreach (ITimelineElementModel im in arModels)
                    {
                        IconClass ic = IconClass.Undefined;
                        if (im is IIconModel)
                            ic = IconRegistry.IconTypeToClass(IconRegistry.StringToIconType((im as IIconModel).IconType));
                        if (im is IIconModel && (ic == IconClass.Transportation || ic == IconClass.GenericActivity))
                        {
                            // it's a travel/generic icon.  lets register it.
                            WhereAmIElement oNewElem = new WhereAmIElement(im as ITimelineElementModel);

                            m_dictRegisteredElements.Add(im.UniqueID, oNewElem);
                            m_arSortedByTime.Add(oNewElem);
                            im.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(im_PropertyChanged);
                            bResort = true;
                        }
                        else
                        {
                            im.LoadComplete += new EventHandler(im_LoadComplete);
                        }
                    }

                    if (bResort)
                        Resort();
                }
            }
        }

        void Model_ChildGroupChanged(object sender, PropertyGroupChangedEventArgs e)
        {
            if (e.Action == PropertyGroupChangedAction.Remove)
            {
                ITimelineElementModel m = e.PropertyContainer as ITimelineElementModel;
                if (m_dictRegisteredElements.ContainsKey(m.UniqueID))
                {
                    // a child has been removed, and it IS a travel icon. 
                    // we need to update our where-am-i info.
                    m_arSortedByTime.Remove(m_dictRegisteredElements[m.UniqueID]);
                    m_dictRegisteredElements.Remove(m.UniqueID);
                    Resort();

                    if (WhereAmIChanged != null)
                        WhereAmIChanged.Invoke(this, new EventArgs());
                }
            }
            else if (e.Action == PropertyGroupChangedAction.Add)
            {
                ITimelineElementModel m = e.PropertyContainer as ITimelineElementModel;
                if (m is IIconModel)
                    NewIconAdded(m as IIconModel);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        void im_LoadComplete(object sender, EventArgs e)
        {
            if (sender is IIconModel)
                NewIconAdded(sender as IIconModel);
        }

        void im_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DockTime" || e.PropertyName == "ArrivalCity" || e.PropertyName == "City" || e.PropertyName == "DestinationName" 
                || e.PropertyName == "TimeZone" || e.PropertyName == "TimeZoneDSTOffset" || e.PropertyName == "TimeZoneOffset" )
            {
                ITimelineElementModel im = sender as ITimelineElementModel;
                WhereAmIElement w = m_dictRegisteredElements[im.UniqueID];

                if (w.Update())
                {
                    if (e.PropertyName == "DockTime")
                    {
                        Resort();
                    }

                    if (WhereAmIChanged != null)
                        WhereAmIChanged.Invoke(this, new EventArgs());
                }
            }
        }
        
        void Resort()
        {
            m_arSortedByTime = (from u in m_arSortedByTime
                        orderby u.Start
                        select u).ToList();  
        }

        void NewIconAdded(IIconModel in_oIcon)
        {
            IconClass ic = IconRegistry.IconTypeToClass(IconRegistry.StringToIconType((in_oIcon as IIconModel).IconType));

            if (ic == IconClass.Transportation || ic == IconClass.GenericActivity)
            {
                // a child has been added, and it IS a travel icon. 
                // we need to update our where-am-i info.

                ITimelineElementModel ih = in_oIcon as ITimelineElementModel;

                // it's a travel icon.  lets register it.
                WhereAmIElement oNewElem = new WhereAmIElement(ih);
                m_dictRegisteredElements.Add(ih.UniqueID, oNewElem);
                m_arSortedByTime.Add(oNewElem);

                ih.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(im_PropertyChanged);

                Resort();

                if (WhereAmIChanged != null)
                    WhereAmIChanged.Invoke(this, new EventArgs());
            }
        }

        public string CityFromDockTime( DateTime in_dtTime )
        {
            string strCity = string.Empty;

            foreach (WhereAmIElement oNewElem in m_arSortedByTime)
            {
                if (oNewElem.Start > in_dtTime)
                    return strCity;
                strCity = oNewElem.Location;
            }
            return strCity;
        }
    }
}
