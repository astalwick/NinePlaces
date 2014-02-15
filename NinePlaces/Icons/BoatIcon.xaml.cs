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
using System.Windows.Data;
using NinePlaces.Icons;
using Editors;

namespace NinePlaces
{
    public partial class BoatIcon : TravelIcon
    {
        public override IconTypes IconType
        {
            get { return IconTypes.Boat; }
        }

        public BoatIcon()
        {
            InitializeComponent();
        }

        public BoatIcon(IIconViewModel in_oIconVM)
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
                    BoatDetailsEditor bd = new BoatDetailsEditor(IconVM);
                    BaseCanvas.Children.Add(bd);
                    bd.ParentWidth = IconWidth;

                    DetailsEditor = bd;
                }
                DetailsEditor.Show();
            }
        }
    }
}
