using System;
using System.Windows.Controls;
using System.Windows.Media;
using Common.Interfaces;
using BasicEditors;
using System.Windows.Data;

namespace NinePlaces
{
    public partial class PhotosIcon : IconControlBase
    {
        public override Canvas BaseCanvas { get; set; }
        protected override Grid BaseGrid { get; set; }

        public override IconTypes IconType
        {
            get { return IconTypes.Photos; }
        }
        public PhotosIcon()
        {
            InitializeComponent();

            // wire up the NECESSARY components
            BaseCanvas = StdBaseCanvas;
            BaseGrid = StdBaseGrid;

            // handle some of the useful events.
            LayoutUpdated += new EventHandler(PhotosIcon_LayoutUpdated);
        }

        public PhotosIcon(IIconViewModel in_oIconVM)
            : this()
        {
            IconVM = in_oIconVM;
        }

        void PhotosIcon_LayoutUpdated(object sender, EventArgs e)
        {
            //
            // we need to adjust the dimensions of our child stroke.
            //

            if (Docked)
            {
                BGOpacityScale.ScaleX = BGOpacityScale.ScaleY = (BaseCanvas.ActualWidth / App.StaticIconWidthHeight);
                BGScale.ScaleX = BGScale.ScaleY = (BaseCanvas.ActualWidth / App.StaticIconWidthHeight);
                IconScale.ScaleX = IconScale.ScaleY = (BaseCanvas.ActualWidth / App.StaticIconWidthHeight);
            }
        }

        protected override Color IconColor
        {
            get
            {
                return ((SolidColorBrush)StdBGRect.Fill).Color;
            }
        }


        public override void Click()
        {
            if (Docked)
            {
                if (Orbs.Count == 0)
                {
                    IOrbControl o = Orbs.CreateOrb(Color.FromArgb(255, 244, 204, 82));
                    o.SlideOutTitleControl = new QuickEditControl(new Binding("StringLibrary.UploadPhotos") { Source = App.LocalizedStrings });
                    o.SlideOutEditor = new PhotosDetailsEditor(IconVM) as IDetailsEditor;

                }
                Orbs.Shown = !Orbs.Shown;
            }
        }
    }
}
