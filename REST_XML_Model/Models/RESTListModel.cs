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
using NinePlaces.Models;
using Common.Interfaces;
using NinePlaces.Models.REST;

namespace REST_XML_Model.Models
{
    public class RESTListModel: RESTBaseModel, IListModel
    {
        public RESTListModel(IHierarchicalPropertyModel in_oParentModel, RESTSaveManager in_oSaveMgr)
            : base(in_oParentModel, in_oSaveMgr)
        {
            NodeType = PersistenceElements.List;
        }

        protected override IPrivateTimelineElementModel ConstructChildModel(PersistenceElements in_eNodeType, bool in_bAlreadyLoaded)
        {
            RESTListItemModel xNewIconModel = new RESTListItemModel(this, m_oSaveMgr);
            xNewIconModel.OverrideUserID = OverrideUserID;
            xNewIconModel.Loaded = in_bAlreadyLoaded;
            return xNewIconModel as IPrivateTimelineElementModel;
        }
    }
}
