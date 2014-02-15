using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Xml.Linq;
using Common.Interfaces;
using Common;
using System.IO;
using System.Windows.Threading;
using System.Windows;

namespace NinePlaces.Models.REST
{
    public class RESTIconModel : RESTBaseModel, IIconModel
    {
        public string IconType
        {
            get
            {
                if (Properties.ContainsKey("IconType"))
                    return Properties["IconType"];
                return string.Empty;
            }
        }

        public RESTIconModel(RESTVacationModel in_oParentModel, RESTSaveManager in_oSaveMgr) : base( in_oParentModel, in_oSaveMgr )
        {
            NodeType = PersistenceElements.Icon;
        }
    }
}
