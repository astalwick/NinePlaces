using System;
using System.Windows.Controls;
using System.Windows.Media;
using Common.Interfaces;
using BasicEditors;
using System.Windows.Data;
using Editors;

namespace NinePlaces.Icons
{
    public partial class MeetUpIcon : ActivityIcon
    {
        public MeetUpIcon()
        {
            InitializeComponent();
        }

        public MeetUpIcon(IIconViewModel in_oIconVM)
            : this()
        {
            IconVM = in_oIconVM;
        }

        public override IconTypes IconType
        {
            get { return IconTypes.MeetUp; }
        }

        public override void Click()
        {
            if (Docked)
            {
                if (DetailsEditor == null)
                {
                    MeetUpDetailsEditor bd = new MeetUpDetailsEditor(IconVM);
                    BaseCanvas.Children.Add(bd);
                    bd.ParentWidth = IconWidth;

                    DetailsEditor = bd;
                }
                DetailsEditor.Show();
            }
        }
    }
}
