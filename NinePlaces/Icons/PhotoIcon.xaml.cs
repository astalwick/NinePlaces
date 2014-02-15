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
using System.Windows.Data;

namespace NinePlaces.Icons
{
    public partial class PhotoIcon : DockableControlBase, ISelectable, IDockable, IDraggable, IInterfaceElementWithVM
    {
        public PhotoIcon()
        {
            InitializeComponent();
        }

        public PhotoIcon(IPhotoViewModel in_oIconVM)
            : this()
        {
            DockableVM = in_oIconVM;
            DataContext = in_oIconVM;
            DockableVM.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(DockableVM_PropertyChanged);
            LayoutUpdated += new EventHandler(PhotoIcon_LayoutUpdated);
            CloseButton.Click += new RoutedEventHandler(CloseButton_Click);
            KeyDown += new KeyEventHandler(PhotoIcon_KeyDown);
            image.KeyDown += new KeyEventHandler(PhotoIcon_KeyDown);
            InnerGrid.KeyDown += new KeyEventHandler(PhotoIcon_KeyDown);
            LayoutRoot.KeyDown += new KeyEventHandler(PhotoIcon_KeyDown);
            border.KeyDown += new KeyEventHandler(PhotoIcon_KeyDown);
            ClickAbsorbCanvas.MouseLeftButtonDown += new MouseButtonEventHandler(ClickAbsorbCanvas_MouseLeftButtonDown);

            IPhotoViewModel vm = in_oIconVM as IPhotoViewModel;
            if (vm != null && (vm.IsThumbnailAvailable || vm.IsPhotoRes ))
                LayoutRoot.Visibility = Visibility.Visible;
        }

        void ClickAbsorbCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if( !FullScreen )
            {
                ClickAbsorbCanvas.Visibility = Visibility.Collapsed;
                ClickAbsorbCanvas.IsHitTestVisible = false;
            }
            FullScreen = false;
            Selected = false;
        }

        void PhotoIcon_KeyDown(object sender, KeyEventArgs e)
        {
            if (FullScreen && e.Key == Key.Escape)
            {
                FullScreen = false;
                Selected = false;
                e.Handled = true;
            }
        }

        public override Brush BackgroundBrush
        {
            get { return null; }
            set { }
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            FullScreen = false;
            Selected = false;
        }

        private bool m_bFullScreen = false;
        public bool FullScreen
        {
            get
            {
                return m_bFullScreen;
            }
            protected set
            {
                if (m_bFullScreen != value)
                {
                    m_bFullScreen = value;

                    if (m_bFullScreen)
                    {
                        // remember old position.
                        if (rcSmall.IsEmpty)
                            rcSmall = new Rect(Canvas.GetLeft(this), Canvas.GetTop(this), IconWidth, IconHeight);

                        // new size
                        Size s = App.AppRootVisual.RenderSize;
                        GeneralTransform objGeneralTransform = App.Timeline.Control.TransformToVisual(this.Parent as UIElement);

                        Point ptXY = objGeneralTransform.Transform(new Point(15, 15));
                        Rect rcNew = new Rect(ptXY.X, ptXY.Y, App.Timeline.Control.ActualWidth - 30, App.Timeline.Control.ActualHeight - 30);

                        //
                        // now, account for aspect ratio of image.
                        //
                        double dWidth = GetPhotoWidthAtHeight(rcNew.Height);
                        double dHeight = rcNew.Height;
                        if (dWidth > rcNew.Width)
                        {
                            dHeight = GetPhotoHeightAtWidth(rcNew.Width);

                            Rect rcTemp = rcNew;
                            rcNew = new Rect(
                                rcNew.Left,
                                rcNew.Top + ((rcNew.Height - dHeight) / 2.0),
                                rcNew.Width,
                                dHeight);
                        }
                        else
                        {
                            // center in width
                            Rect rcTemp = rcNew;
                            rcNew = new Rect(
                                rcNew.Left + ((rcNew.Width - dWidth) / 2.0),
                                rcNew.Top,
                                dWidth,
                                rcNew.Height);
                        }

                        AnimateToRect(rcNew, rcSmall);
                    }
                    else
                    {
                        
                        Rect rcThis = new Rect(Canvas.GetLeft(this), Canvas.GetTop(this), IconWidth, IconHeight);
                        AnimateToRect(rcSmall, rcThis);
                    }
                }

            }
        }

        
        void PhotoIcon_LayoutUpdated(object sender, EventArgs e)
        {
            if (DockableVM != null && (DockableVM as IPhotoViewModel).Photo != null && !Selected)
            {
                double dWidth = (DockableVM as IPhotoViewModel).Photo.PixelWidth;
                double dHeight = (DockableVM as IPhotoViewModel).Photo.PixelHeight;

                IconWidth = dWidth / dHeight * IconHeight;
            }
        }

        private double GetPhotoHeightAtWidth(double in_dWidth)
        {
            double dWidth = (DockableVM as IPhotoViewModel).Photo.PixelWidth;
            double dHeight = (DockableVM as IPhotoViewModel).Photo.PixelHeight;

            return dHeight / dWidth * in_dWidth;
        }

        private double GetPhotoWidthAtHeight(double in_dHeight)
        {
            double dWidth = (DockableVM as IPhotoViewModel).Photo.PixelWidth;
            double dHeight = (DockableVM as IPhotoViewModel).Photo.PixelHeight;

            return dWidth / dHeight * in_dHeight;
        }

        void DockableVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

            if (e.PropertyName == "UploadStatus")
            {
                IPhotoViewModel vm = DockableVM as  IPhotoViewModel;
                if (vm.UploadStatus == 0)
                {
                    UploadingText.Visibility = Visibility.Visible;
                    UploadingText.Text = "UPLOAD\r\nQUEUED" ;
                }
                else if (vm.UploadStatus<100)
                {
                    UploadingText.Visibility = Visibility.Visible;
                    UploadingText.Text = "UPLOADING\r\n\r\n" + vm.UploadStatus + "% done";
                }
                else
                {
                    UploadingText.Visibility = Visibility.Collapsed;
                }
            }
            else if (e.PropertyName == "DownloadStatus")
            {
                IPhotoViewModel vm = DockableVM as IPhotoViewModel;
                if (vm.DownloadStatus < 100 && vm.DownloadStatus > 0)
                {
                    UploadingText.Visibility = Visibility.Visible;
                    UploadingText.Text = "LOADING";
                    UploadingText.FontSize = 18;
                }
                else
                {
                    UploadingText.Visibility = Visibility.Collapsed;
                }
            }
            else if (e.PropertyName == "Photo")
            {
                IPhotoViewModel vm = DockableVM as IPhotoViewModel;
                if (vm.IsThumbnailAvailable || vm.IsPhotoRes)
                    LayoutRoot.Visibility = Visibility.Visible;
            }
        }

        private Rect rcSmall = Rect.Empty;
        protected override void SelectionChanged()
        {
            IPhotoViewModel vm = DockableVM as  IPhotoViewModel;
            base.SelectionChanged();
            if (vm.IsUploaded)
            {
                if (Selected)
                {
                    DraggingEnabled = false;
                    if (!vm.IsPhotoRes)
                    {
                        (DockableVM as IPhotoViewModel).DownloadPhoto();
                    }

                    FullScreen = true;
                }
                else 
                {
                    FullScreen = false;
                }
            }
        }


        private Storyboard m_sbSelectedAnimation = null;
        private void AnimateToRect(Rect rcIn, Rect rcOld)
        {
            if (m_sbSelectedAnimation != null)
                m_sbSelectedAnimation.SkipToFill();

            //
            // create the position animation.
            //
            m_sbSelectedAnimation = new Storyboard();
            DoubleAnimation ptY = new DoubleAnimation();
            ptY.Duration = TimeSpan.FromSeconds(0.20);
            ptY.From = rcOld.Top;
            ptY.To = rcIn.Top;
            Storyboard.SetTarget(ptY, this);
            PropertyPath p = new PropertyPath("(Canvas.Top)");
            Storyboard.SetTargetProperty(ptY, p);
            m_sbSelectedAnimation.Children.Add(ptY);

            DoubleAnimation ptX = new DoubleAnimation();
            ptX.Duration = TimeSpan.FromSeconds(0.20);
            ptX.From = rcOld.Left;
            ptX.To = rcIn.Left;
            Storyboard.SetTarget(ptX, this);
            PropertyPath pX = new PropertyPath("(Canvas.Left)");
            Storyboard.SetTargetProperty(ptX, pX);
            m_sbSelectedAnimation.Children.Add(ptX);

            DoubleAnimation ptW = new DoubleAnimation();
            ptW.Duration = TimeSpan.FromSeconds(0.20);
            ptW.From = rcOld.Width;
            ptW.To = rcIn.Width;
            Storyboard.SetTarget(ptW, this);
            PropertyPath pW = new PropertyPath("IconWidth");
            Storyboard.SetTargetProperty(ptW, pW);
            m_sbSelectedAnimation.Children.Add(ptW);


            DoubleAnimation ptH = new DoubleAnimation();
            ptH.Duration = TimeSpan.FromSeconds(0.20);
            ptH.From = rcOld.Height;
            ptH.To = rcIn.Height;
            Storyboard.SetTarget(ptH, this);
            PropertyPath pH = new PropertyPath("IconHeight");
            Storyboard.SetTargetProperty(ptH, pH);
            m_sbSelectedAnimation.Children.Add(ptH);

            m_sbSelectedAnimation.Begin();

            m_sbSelectedAnimation.Completed += new EventHandler(m_sbSelectedAnimation_Completed);
        }

        void m_sbSelectedAnimation_Completed(object sender, EventArgs e)
        {
            if (FullScreen)
            {
                CloseButton.Visibility = Visibility.Visible;
                CloseButton.Focus();

                ClickAbsorbCanvas.IsHitTestVisible = true;
                ClickAbsorbCanvas.Visibility = Visibility.Visible;

                GeneralTransform objGeneralTransform = App.Timeline.Control.TransformToVisual(LayoutRoot as UIElement);
                Point ptXY = objGeneralTransform.Transform(new Point(0, 0));
                Canvas.SetLeft(ClickAbsorbCanvas, ptXY.X);
                Canvas.SetTop(ClickAbsorbCanvas, ptXY.Y);
                ClickAbsorbCanvas.Width = App.Timeline.Control.ActualWidth;
                ClickAbsorbCanvas.Height = App.Timeline.Control.ActualHeight;
            }
            else
            {
                CloseButton.Visibility = Visibility.Collapsed;
                ClickAbsorbCanvas.IsHitTestVisible = false;
                ClickAbsorbCanvas.Visibility = Visibility.Collapsed;
                rcSmall = Rect.Empty;
                DraggingEnabled = true;
            }
        }

        public override IconClass IconClass
        {
            get { return IconClass.Photo; }
        }
    }
}
