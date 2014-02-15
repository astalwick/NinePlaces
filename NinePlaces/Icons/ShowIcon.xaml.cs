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
    public partial class ShowIcon : ActivityIcon
    {
        public override IconTypes IconType
        {
            get { return IconTypes.Show; }
        }
        public ShowIcon()
        {
            InitializeComponent();
        }

        public ShowIcon(IIconViewModel in_oIconVM)
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
                    ShowDetailsEditor bd = new ShowDetailsEditor(IconVM);
                    BaseCanvas.Children.Add(bd);
                    bd.ParentWidth = IconWidth;

                    DetailsEditor = bd;
                }
                DetailsEditor.Show();
            }
        }
    }
}
