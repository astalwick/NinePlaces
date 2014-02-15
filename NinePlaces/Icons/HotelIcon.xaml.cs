using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Common.Interfaces;
using BasicEditors;
using System.Windows.Data;
using NinePlaces.Icons;
using Editors;

namespace NinePlaces
{
    public partial class HotelIcon : LodgingIcon
    {
        public override IconTypes IconType
        {
            get { return IconTypes.Hotel; }
        }

        public HotelIcon()
        {
            InitializeComponent();
        }

        public HotelIcon(IIconViewModel in_oIconVM)
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
                    HotelDetailsEditor bd = new HotelDetailsEditor(IconVM);
                    BaseCanvas.Children.Add(bd);
                    bd.ParentWidth = IconWidth;

                    DetailsEditor = bd;
                }
                DetailsEditor.Show();
            }
        }
    }
}
