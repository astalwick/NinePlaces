using System;
using System.Windows.Controls;
using System.Windows.Media;
using Common.Interfaces;
using BasicEditors;
using System.Windows.Data;
using NinePlaces.Icons;
using Editors;

namespace NinePlaces
{
    public partial class FlightIcon : TravelIcon
    {
        public override IconTypes IconType
        {
            get { return IconTypes.Flight; }
        }

        public FlightIcon()
        {
            InitializeComponent();
        }

        public FlightIcon(IIconViewModel in_oIconVM)
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
                    FlightDetailsEditor bd = new FlightDetailsEditor(IconVM);
                    BaseCanvas.Children.Add(bd);
                    bd.ParentWidth = IconWidth;

                    DetailsEditor = bd;
                }
                DetailsEditor.Show();
            }
        }
    }
}
