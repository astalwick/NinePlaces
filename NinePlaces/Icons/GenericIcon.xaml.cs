using System.Windows;
using System.Windows.Media;
using Common.Interfaces;
using Editors;
using NinePlaces.ViewModels;


namespace NinePlaces
{
    public partial class GenericIcon : IconControlBase
    {
        public override IconTypes IconType
        {
            get { return IconTypes.GenericActivity; }
        }

        public IconClass CurrentClass
        {
            get
            {
                return (IconVM as IMutableIconProperties).CurrentClass;
            }
        }

        public override IconClass IconClass
        {
            get
            {
                return IconClass.GenericActivity;
            }
        }

        public override DockLine DockLine
        {
	        get 
	        { 
		         return base.DockLine;
	        }
	        set 
	        { 
		        base.DockLine = value;

                if (value != null)
                    (IconVM as IMutableIconProperties).CurrentClass = value.Class;
	        }
        }
        public GenericIcon()
        {
            InitializeComponent();
        }
        public GenericIcon(IIconViewModel in_oIconVM)
            : this()
        {
            IconVM = in_oIconVM;
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //
            // We set out brush template only when we don't have a dockline
            // If we have a dockline, DockControlBase has already set this for us
            if (DockLine == null)
                BackgroundBrush = Application.Current.Resources["GenericIconGradient"] as Brush;
        }
        public override void Click()
        {
            if (Docked)
            {
                if (DetailsEditor == null)
                {
                    GenericDetailsEditor bd = new GenericDetailsEditor(IconVM);
                    BaseCanvas.Children.Add(bd);
                    bd.ParentWidth = IconWidth;

                    DetailsEditor = bd;
                }
                DetailsEditor.Show();
            }
        }

        protected override void VM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // current class has changed.  this is not a bindable property - it means
            // resetting our dockline to something new.
            if (e.PropertyName == "CurrentClass" && !App.Dragging && (IconVM as IMutableIconProperties).CurrentClass != IconClass.GenericActivity)
            {
                foreach (DockLine d in App.Timeline.DockLines)
                {
                    if ((IconVM as IMutableIconProperties).CurrentClass == d.Class)
                        DockLine = d;
                }
            }
            else
            {
                base.VM_PropertyChanged(sender, e);
            }
        }
    }
}
