using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common.Interfaces;
using BasicEditors;
using Editors;
using System.Windows.Data;

namespace NinePlaces.Icons
{
    public partial class SportingEventIcon : ActivityIcon
    {
        public override IconTypes IconType
        {
            get { return IconTypes.SportingEvent; }
        }

        public SportingEventIcon()
        {
            InitializeComponent();
        }

        public SportingEventIcon(IIconViewModel in_oIconVM)
            : this()
        {
            IconVM = in_oIconVM;
        }

        public override void Click()
        {
            if (Docked)
            {
                if (DetailsEditor == null)
                {
                    SportingEventDetailsEditor bd = new SportingEventDetailsEditor(IconVM);
                    BaseCanvas.Children.Add(bd);
                    bd.ParentWidth = IconWidth;

                    DetailsEditor = bd;
                }
                DetailsEditor.Show();
            }
        }
    }
}
