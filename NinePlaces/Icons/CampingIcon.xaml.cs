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
    public partial class CampingIcon : LodgingIcon
    {
        public override IconTypes IconType
        {
            get { return IconTypes.Camping; }
        }
        public CampingIcon()
        {
            InitializeComponent();
        }

        public CampingIcon(IIconViewModel in_oIconVM)
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
                    CampingDetailsEditor bd = new CampingDetailsEditor(IconVM);
                    bd.ParentWidth = IconWidth;
                    BaseCanvas.Children.Add(bd);
                    DetailsEditor = bd;
                }
                DetailsEditor.Show();
            }
        }
    }
}
