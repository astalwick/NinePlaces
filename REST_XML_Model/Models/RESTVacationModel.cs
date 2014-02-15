using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using Common.Interfaces;
using System.Collections.Generic;
using Common;
using System.Globalization;
using REST_XML_Model.Models;

namespace NinePlaces.Models.REST
{
    public class RESTVacationModel : RESTBaseModel, IIconContainerModel
    {
        DateTime m_dtFirstIcon = new DateTime(9999, 1, 1);
        public DateTime FirstIconDate
        {
            get
            {
                if (m_dtFirstIcon.Year == 9999)
                    UpdateFirstLastIconDate();

                return m_dtFirstIcon;
            }
        }

        DateTime m_dtLastIcon = new DateTime(1, 1, 1);
        public DateTime LastIconDate
        {
            get
            {
                if (m_dtLastIcon.Year == 1)
                    UpdateFirstLastIconDate();

                return m_dtLastIcon;
            }
        }

        public RESTVacationModel(IHierarchicalPropertyModel in_oParentModel, RESTSaveManager in_oSaveMgr)
            : base(in_oParentModel, in_oSaveMgr)
        {
            NodeType = PersistenceElements.Vacation;
        }

        public override bool DoneLoad()
        {
            UpdateFirstLastIconDate();
            return base.DoneLoad(); 
        }

        public override bool DoneSave()
        {
            UpdateFirstLastIconDate();
            return base.DoneSave();
        }

        private void UpdateFirstLastIconDate()
        {
            if (!Children.ContainsKey("Icon"))
                return;

            List<IHierarchicalPropertyModel> iIcons = Children["Icon"];

            if (iIcons.Count() == 0)
                return;

            long lFirstTicks = long.MaxValue;
            long lLastTicks = long.MinValue;
            foreach (IHierarchicalPropertyModel p in iIcons)
            {
                DateTime dtStart;
                if( DateTime.TryParseExact(p.Properties["DockTime"], "yyyy-MM-ddTHH:mm:ss.ff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtStart) )
                {
                    if (dtStart.Ticks < lFirstTicks)
                        lFirstTicks = dtStart.Ticks;
                    if (dtStart.Ticks > lLastTicks)
                        lLastTicks = dtStart.Ticks;
                }
            }

            m_dtLastIcon = new DateTime( lLastTicks );
            m_dtFirstIcon = new DateTime( lFirstTicks );
        }

        protected override IPrivateTimelineElementModel ConstructChildModel(PersistenceElements in_eNodeType, bool in_bAlreadyLoaded)
        {
            if (in_eNodeType == PersistenceElements.Icon)
            {
                RESTIconModel xNewIconModel = new RESTIconModel(this, m_oSaveMgr);
                xNewIconModel.OverrideUserID = OverrideUserID;
                xNewIconModel.Loaded = in_bAlreadyLoaded;
                return xNewIconModel as IPrivateTimelineElementModel;
            }
            else if (in_eNodeType == PersistenceElements.Photo)
            {
                RESTPhotoModel xNewPhotoModel = new RESTPhotoModel(this, m_oSaveMgr);
                xNewPhotoModel.OverrideUserID = OverrideUserID;
                xNewPhotoModel.Loaded = in_bAlreadyLoaded;
                return xNewPhotoModel as IPrivateTimelineElementModel;
            }
            else if (in_eNodeType == PersistenceElements.List)
            {
                RESTListModel xNewPhotoModel = new RESTListModel(this, m_oSaveMgr);
                xNewPhotoModel.OverrideUserID = OverrideUserID;
                xNewPhotoModel.Loaded = in_bAlreadyLoaded;
                return xNewPhotoModel as IPrivateTimelineElementModel;
            }

            throw new Exception();
        }
    }
}
