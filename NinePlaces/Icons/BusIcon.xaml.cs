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
    public partial class BusIcon : TravelIcon
    {
        public override IconTypes IconType
        {
            get { return IconTypes.Bus; }
        }
        public BusIcon()
        {
            InitializeComponent();

        }

        public BusIcon(IIconViewModel in_oIconVM)
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
                    BusDetailsEditor bd = new BusDetailsEditor(IconVM);
                    BaseCanvas.Children.Add(bd);
                    bd.ParentWidth = IconWidth;

                    DetailsEditor = bd;
                }
                DetailsEditor.Show();
            }
        }
    }
}
