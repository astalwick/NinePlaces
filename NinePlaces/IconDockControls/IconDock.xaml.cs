using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Common.Interfaces;
using System.Windows.Data;
using NinePlaces.Localization;

namespace NinePlaces.IconDockControls
{
    public interface IIconDock
    {
        event IconDockEventHandler IconCreated;
    }

    /// <summary>
    /// The IconDock itself.  
    /// </summary>
	public partial class IconDock : UserControl, IIconDock
	{
        public event IconDockEventHandler IconCreated;
		public IconDock()
		{
			// Required to initialize variables
			InitializeComponent();
            Loaded += new RoutedEventHandler(IconDock_Loaded);
            
            // Set up the transportation icon group.
            IconDockGroup iTranspo = new IconDockGroup();
            iTranspo.IconCreated += new IconDockEventHandler(Group_IconCreated);

            Binding bBinding = new Binding("StringLibrary.Transportation") { Source = this.Resources["LocalizedStrings"], Mode = BindingMode.OneWay };
            iTranspo.Title.SetBinding(TextBlock.TextProperty, bBinding);
            iTranspo.Icons.Add(IconTypes.Car);
            iTranspo.Icons.Add(IconTypes.Flight);
            iTranspo.Icons.Add(IconTypes.Boat);
            iTranspo.Icons.Add(IconTypes.Train);
            iTranspo.Icons.Add(IconTypes.Bus);

            // Set up the lodging icon group.
            IconDockGroup iLodging = new IconDockGroup();
            iLodging.IconCreated += new IconDockEventHandler(Group_IconCreated);

            bBinding = new Binding("StringLibrary.Lodging") { Source = this.Resources["LocalizedStrings"], Mode = BindingMode.OneWay };
            iLodging.Title.SetBinding(TextBlock.TextProperty, bBinding);
            iLodging.Icons.Add(IconTypes.Hotel);
            iLodging.Icons.Add(IconTypes.Camping);
            iLodging.Icons.Add(IconTypes.House);
            
            // Set up the Activities icons group
            IconDockGroup iActivities = new IconDockGroup();
            iActivities.IconCreated += new IconDockEventHandler(Group_IconCreated);
            bBinding = new Binding("StringLibrary.Activities") { Source = this.Resources["LocalizedStrings"], Mode = BindingMode.OneWay };
            iActivities.Title.SetBinding(TextBlock.TextProperty, bBinding);
            iActivities.Icons.Add(IconTypes.Beach);
            iActivities.Icons.Add(IconTypes.Outdoor);
            iActivities.Icons.Add(IconTypes.Show);
            iActivities.Icons.Add(IconTypes.Sightseeing);
            iActivities.Icons.Add(IconTypes.MeetUp);
            iActivities.Icons.Add(IconTypes.Restaurant);
            iActivities.Icons.Add(IconTypes.Shopping);
            iActivities.Icons.Add(IconTypes.Nightlife);
            iActivities.Icons.Add(IconTypes.SportingEvent);
            IconDockGroup iOther = new IconDockGroup();
            iOther.IconCreated += new IconDockEventHandler(Group_IconCreated);
            bBinding = new Binding("StringLibrary.OtherActivies") { Source = this.Resources["LocalizedStrings"], Mode = BindingMode.OneWay };
            iOther.Title.SetBinding(TextBlock.TextProperty, bBinding);
            iOther.Icons.Add(IconTypes.GenericActivity);

            // Add the icon groups to the IconDock container
            IconGroupContainer.Children.Insert(0, iOther);
            IconGroupContainer.Children.Insert(0, iActivities);
            IconGroupContainer.Children.Insert(0, iLodging);
            IconGroupContainer.Children.Insert(0, iTranspo);
		}

        void Group_IconCreated(object sender, IconDockEventArgs args)
        {
            if (IconCreated != null)
                IconCreated.Invoke(sender, args);
        }

#warning The 'loading' indicator should not be part of the icon dock.
        //
        //
        // TODO: use a Popup for this.  It shouldn't be part of the canvas, and certainly shouldn't be
        // part of the icondock.  It should be a popup that is initiated by the timeline and is 
        // displayed over the whole page!
        //
        //

        void IconDock_Loaded(object sender, RoutedEventArgs e)
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this) && App.Timeline != null && !App.Timeline.TimelineLoaded)
            {
                /*App.Timeline.TimelineLoadedEvent += new EventHandler(Timeline_TimelineLoaded);
                FadeStoryBoard.RepeatBehavior = RepeatBehavior.Forever;
                FadeStoryBoard.Begin();*/

                HideLoading.Begin();
            }
        }

        void Timeline_TimelineLoaded(object sender, EventArgs e)
        {
            /*FadeStoryBoard.Stop();
            HideLoading.Begin();*/
        }
	}
}