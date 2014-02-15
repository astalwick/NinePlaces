using System;
using System.ComponentModel;
using System.Xml.Linq;
using Common.Interfaces;
using NinePlaces.Models;

namespace NinePlaces.Models.REST
{
    public class RESTTimelineModel : RESTBaseModel
    {
        
        public static ITimelineElementModel RootTimelineModel = null;

        // random number for unique ids.
        private static RESTSaveManager gSaveMgr = new RESTSaveManager();

        public RESTTimelineModel()
            : base(gSaveMgr)
        {
            UniqueID = 123;     // to remove! 
            NodeType = PersistenceElements.Timeline;

            // we should make sure that we're not holding on to anything.
            Empty();
            RootTimelineModel = this;
        }

        protected override IPrivateTimelineElementModel ConstructChildModel(PersistenceElements in_eNodeType, bool in_bAlreadyLoaded)
        {
            RESTVacationModel xNewIconModel = new RESTVacationModel(this, m_oSaveMgr);
            xNewIconModel.OverrideUserID = OverrideUserID;
            xNewIconModel.Loaded = in_bAlreadyLoaded;
            return xNewIconModel as IPrivateTimelineElementModel;
        }
    }
}
