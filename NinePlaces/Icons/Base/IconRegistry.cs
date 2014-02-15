using NinePlaces.Icons;
using Common.Interfaces;
using NinePlaces.ViewModels;
using System.Windows.Media;
using System.Windows;
using System;

namespace NinePlaces
{
    /// <summary>
    /// Static helper class that will construct new icons for us.
    /// </summary>
    public static class IconRegistry
    {
        public static IconControlBase NewIcon(IconTypes in_oIcon)
        {
            return NewIcon(in_oIcon, null);
        }

        public static IIconViewModel NewIconVMFromModel(IconTypes in_oType, ITimelineElementModel in_oIconModel)
        {
            IconViewModel icB = null;

            if (in_oType == IconTypes.Undefined)
                throw new Exception();

            if (in_oType == IconTypes.GenericActivity)
                icB = new GenericIconViewModel(in_oIconModel);
            else if (IconTypeToClass(in_oType) == IconClass.Lodging)
            {
                if (in_oType == IconTypes.Hotel)
                    icB = new HotelIconViewModel(in_oIconModel);
                else if (in_oType == IconTypes.Camping)
                    icB = new CampingIconViewModel(in_oIconModel);
                else
                    icB = new LodgingViewModel(in_oType, in_oIconModel);
            }
            else if (IconTypeToClass(in_oType) == IconClass.Transportation)
            {
                if (in_oType == IconTypes.Flight)
                    icB = new FlightIconViewModel(in_oIconModel);
                else
                    icB = new TravelIconViewModel(in_oType, in_oIconModel);
            }
            else
            {
                switch (in_oType)
                {
                    // special behaviour overrides.
                    case IconTypes.MeetUp:
                        icB = new MeetUpIconViewModel(in_oIconModel);
                        break;
                    case IconTypes.SportingEvent:
                        icB = new SportingEventIconViewModel(in_oIconModel);
                        break;
                    default:
                        // everyone else is just a regular activity icon.
                        icB = new ActivityIconViewModel(in_oType, in_oIconModel);
                        break;
                }
            }
            return icB;
        }

        public static IIconViewModel NewIconVMFromModel(ITimelineElementModel in_oIconModel)
        {
            string strIconType = in_oIconModel.GetProperty("IconType");
            IconTypes icType = StringToIconType(strIconType);
            return NewIconVMFromModel(icType, in_oIconModel);
        }

        public static IIconViewModel NewTempIconVM(IconTypes in_oIconType)
        {
            return NewIconVMFromModel(in_oIconType, null);
        }

        public static IconControlBase NewIcon(IconTypes in_oIcon, IIconViewModel in_oViewModel)
        {

            IconControlBase icB = null;
            switch (in_oIcon)
            {
                case IconTypes.Flight:
                    icB = new FlightIcon(in_oViewModel);
                    break;
                case IconTypes.Car:
                    icB = new CarIcon(in_oViewModel);
                    break;
                case IconTypes.Train:
                    icB = new TrainIcon(in_oViewModel);
                    break;
                case IconTypes.Bus:
                    icB = new BusIcon(in_oViewModel);
                    break;
                case IconTypes.Hotel:
                    icB = new HotelIcon(in_oViewModel);
                    break;
                case IconTypes.Camping:
                    icB = new CampingIcon(in_oViewModel);
                    break;
                case IconTypes.Outdoor:
                    icB = new OutdoorIcon(in_oViewModel);
                    break;
                case IconTypes.Restaurant:
                    icB = new RestaurantIcon(in_oViewModel);
                    break;
                case IconTypes.Show:
                    icB = new ShowIcon(in_oViewModel);
                    break;
                case IconTypes.Sightseeing:
                    icB = new SightseeingIcon(in_oViewModel);
                    break;
                case IconTypes.MeetUp:
                    icB = new MeetUpIcon(in_oViewModel);
                    break;
                case IconTypes.Boat:
                    icB = new BoatIcon(in_oViewModel);
                    break;
                case IconTypes.Beach:
                    icB = new BeachIcon(in_oViewModel);
                    break;
                case IconTypes.Nightlife:
                    icB = new NightlifeIcon(in_oViewModel);
                    break;
                case IconTypes.SportingEvent:
                    icB = new SportingEventIcon(in_oViewModel);
                    break;
                case IconTypes.House:
                    icB = new HouseIcon(in_oViewModel);
                    break;
                case IconTypes.Shopping:
                    icB = new ShoppingIcon(in_oViewModel);
                    break;
                case IconTypes.GenericActivity:
                    icB = new GenericIcon(in_oViewModel);
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

            // fix a flicker.
            icB.Width = icB.Height = App.StaticIconWidthHeight;

            return icB;
        }

        public static IconClass IconTypeToClass(IconTypes in_oType)
        {
            if (in_oType == IconTypes.Flight || in_oType == IconTypes.Car || in_oType == IconTypes.Boat || in_oType == IconTypes.Train || in_oType == IconTypes.Bus)
                return IconClass.Transportation;
            if (in_oType == IconTypes.Hotel || in_oType == IconTypes.House || in_oType == IconTypes.Camping)
                return IconClass.Lodging;
            if (in_oType == IconTypes.GenericActivity)
                return IconClass.GenericActivity;

            return IconClass.Activity;
        }

        public static Brush BackgroundForClass(IconClass in_oClass)
        {
            switch (in_oClass)
            {
                case IconClass.Photo:
                    return Application.Current.Resources["GenericIconGradient"] as Brush;
                case IconClass.GenericActivity:
                    return Application.Current.Resources["GenericIconGradient"] as Brush;
                case IconClass.Activity:
                    return Application.Current.Resources["ActivityIconGradient"] as Brush;
                case IconClass.Transportation:
                    return Application.Current.Resources["TravelIconGradient"] as Brush;
                case IconClass.Lodging:
                    return Application.Current.Resources["LodgingIconGradient"] as Brush;
            }

            return null;
        }

        public static IconClass StringToIconClass(string in_strIconClass)
        {
            IconClass out_eClass;
            if (Enum.TryParse<IconClass>(in_strIconClass, out out_eClass))
                return out_eClass;

            return IconClass.Activity;
        }

        public static IconTypes StringToIconType(string in_strIconType)
        {
            IconTypes eType;
            if (!Enum.TryParse<IconTypes>(in_strIconType, true, out eType))
                return IconTypes.Undefined;

            return eType;
        }

        public static Color DarkColorFromClass(IconClass IconClass)
        {
            switch (IconClass)
            {
                case IconClass.Activity:
                    return ((SolidColorBrush)Application.Current.Resources["ActivityIconDarkColor"]).Color;
                case IconClass.Lodging:
                    return ((SolidColorBrush)Application.Current.Resources["LodgingIconDarkColor"]).Color;
                case IconClass.Transportation:
                    return ((SolidColorBrush)Application.Current.Resources["TravelIconDarkColor"]).Color;
                case IconClass.Photo:
                    return ((SolidColorBrush)Application.Current.Resources["MemoriesIconDarkColor"]).Color;
                case IconClass.GenericActivity:
                    return ((SolidColorBrush)Application.Current.Resources["GenericIconDarkColor"]).Color;
            }

            return new Color();
        }

        public static Color ColorFromClass(IconClass IconClass)
        {
            switch (IconClass)
            {
                case IconClass.Activity:
                    return ((SolidColorBrush)Application.Current.Resources["ActivityIconColor"]).Color;
                case IconClass.Lodging:
                    return ((SolidColorBrush)Application.Current.Resources["LodgingIconColor"]).Color;
                case IconClass.Transportation:
                    return ((SolidColorBrush)Application.Current.Resources["TravelIconColor"]).Color;
                case IconClass.Photo:
                    return ((SolidColorBrush)Application.Current.Resources["MemoriesIconColor"]).Color;
                case IconClass.GenericActivity:
                    return ((SolidColorBrush)Application.Current.Resources["GenericIconColor"]).Color;
            }

            return new Color();
        }
    }
}
