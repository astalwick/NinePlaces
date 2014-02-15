using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common.Interfaces;

namespace NinePlaces
{
    public abstract class IconControlBase : DockableControlBase, IIcon
    {
        public abstract IconTypes IconType { get; }

        public IIconViewModel IconVM
        {
            get
            {
                return DockableVM as IIconViewModel;
            }
            set
            {
                DockableVM = value as IDockableViewModel;
            }
        }

        protected virtual Color IconColor
        {
            get
            {
				// If we're on a dockline, always respect the color from the dockline
                if (DockLine == null)
                    return IconRegistry.ColorFromClass(IconClass);
                else
                    return IconRegistry.ColorFromClass(DockLine.Class);
            }
        }

        protected virtual Color DarkColor
        {
            get
            {
                // If we're on a dockline, always respect the color from the dockline
                if (DockLine == null)
                    return IconRegistry.DarkColorFromClass(IconClass);
                else
                    return IconRegistry.DarkColorFromClass(DockLine.Class);
            }
        }

        protected override void IconControl_LayoutUpdated(object sender, EventArgs e)
        {
            if (TimelineConnector.Color != IconColor)
            {
                TimelineConnector.Color = IconColor;
                Duration.Color = IconColor;
                Duration.DarkColor = DarkColor;
            }

            IconScale.ScaleX = IconScale.ScaleY = (BaseCanvas.ActualWidth / App.StaticIconWidthHeight);
            if (Docked)
            {
                PositionTimelineConnector();
                UpdateDurationIndicator();
            }

            base.IconControl_LayoutUpdated(sender, e);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (!Docked)
            {
                HoverText.BackgroundBrush = BackgroundBrush = IconRegistry.BackgroundForClass(IconClass);

                //
                // we're not docked, so we shouldn't reflect the zoomed icon size.
                //
                IconWidth = App.StaticIconWidthHeight;
                IconHeight = App.StaticIconWidthHeight;
            }
            else
            {
                HoverText.BackgroundBrush = BackgroundBrush = IconRegistry.BackgroundForClass(DockLine.Class);
                //
                // we *are* docked, so set the icon size to whatever it's supposed
                // to be, relative to our zoom.
                //
                IconWidth = App.IconWidthHeight;
                IconHeight = App.IconWidthHeight;

                UpdateDurationIndicator();
            }
        }
    }
}
