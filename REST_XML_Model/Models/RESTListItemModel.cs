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
    public class RESTListItemModel : RESTBaseModel, IListItemModel
    {
        public RESTListItemModel(RESTListModel in_oParentModel, RESTSaveManager in_oSaveMgr)
            : base(in_oParentModel, in_oSaveMgr)
        {
            NodeType = PersistenceElements.ListItem;
        }
    }
}
