using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NinePlaces.Managers;
using NinePlaces.ViewModels;
using Common.Interfaces;
using NinePlaces.Undo;
namespace NinePlaces
{/*
    public interface ITitle : IInterfaceElementWithVM
    {
        event EventHandler LastChildRemoved;
        event EventHandler VacationTimeChanged;
        DateTime FirstIconDate {get;}
        DateTime LastIconDate { get; }
    }*/

    public partial class VacationTitle : UserControl/*, ITitle*/
    {
        protected ITimeline m_oParentTimeline = null;

        public VacationTitle(ITimeline in_oParentTimeline, IVacationViewModel in_oVacationVM)
        {
            InitializeComponent();
            m_oParentTimeline = in_oParentTimeline;
            DataContext = VacationVM = in_oVacationVM;

            WhereAmI.VacationID = in_oVacationVM.UniqueID;

            Loaded += new RoutedEventHandler(VacationTitle_Loaded);
            VacationVM.Removed += new EventHandler(VacationVM_Removed);
            VacationVM.ChildAdded += new VMChildChangedEventHandler(VacationVM_ChildAdded);
            VacationVM.ChildRemoved += new VMChildChangedEventHandler(VacationVM_ChildRemoved);
            VacationVM.FirstLastIconUpdated += new EventHandler(VacationVM_FirstLastIconUpdated);
        }

        void VacationVM_Removed(object sender, EventArgs e)
        {
            Disconnect();
        }

        void VacationTitle_Loaded(object sender, RoutedEventArgs e)
        {
            m_oParentTimeline.DurationChanged += new EventHandler(UpdateWidthEvent);
            m_oParentTimeline.TimelineSizeChanged += new SizeChangedEventHandler(UpdateWidthEvent);
            m_oParentTimeline.StartTimeChanged += new EventHandler(UpdateWidthEvent);
            UpdateWhereAmI();

        }
        void VacationVM_ChildAdded(object sender, VMChildChangedEventArgs args)
        {
            UpdateWhereAmI();
        }

        void VacationVM_ChildRemoved(object sender, VMChildChangedEventArgs args)
        {
            if (VacationVM.Children.Count == 0)
            {
                if (LastChildRemoved != null)
                    LastChildRemoved.Invoke(this, new EventArgs());
                Disconnect();
            }
        }

        public void Disconnect()
        {
            VacationVM.ChildRemoved -= new VMChildChangedEventHandler(VacationVM_ChildRemoved);
            VacationVM.FirstLastIconUpdated -= new EventHandler(VacationVM_FirstLastIconUpdated);
            VacationVM.ChildAdded -= new VMChildChangedEventHandler(VacationVM_ChildAdded);

            m_oParentTimeline.TimelineSizeChanged -= new SizeChangedEventHandler(UpdateWidthEvent);
            m_oParentTimeline.DurationChanged -= new EventHandler(UpdateWidthEvent);
            m_oParentTimeline.StartTimeChanged -= new EventHandler(UpdateWidthEvent);

            m_oParentTimeline = null;
        }

        void UpdateWidthEvent(object sender, EventArgs e)
        {
            UpdateWhereAmI();
        }

        public event EventHandler VacationTimeChanged;
        public event EventHandler LastChildRemoved;

        protected void VacationVM_FirstLastIconUpdated(object sender, EventArgs e)
        {
            if (VacationTimeChanged != null)
                VacationTimeChanged.Invoke(this, new EventArgs());

            UpdateWhereAmI();
        }

        private void UpdateWhereAmI()
        {
            if (VacationVM == null || VacationVM.Children.Count == 0)
                return;

            // check if we're offscreen.
            if (m_oParentTimeline.StartTime > VacationVM.LastIconDate)
                return;
            if (m_oParentTimeline.StartTime + m_oParentTimeline.Duration < VacationVM.FirstIconDate)
                return;

            double dPadding = App.IconWidthHeight / 2.0 + 10 ;

            double dX1 = m_oParentTimeline.TimeToPosition(VacationVM.FirstIconDate);
            double dX2 = m_oParentTimeline.TimeToPosition(VacationVM.LastIconDate);

            // that's kinda fucked.
            if (dX1 > dX2)
                return;

            // deal with exceptionally zoomed in timelines - we can't really  have a rectangle with a width of 250000px canwe?
            if (dX1 < -1000)
                dX1 = -1000;
            if (dX2 > m_oParentTimeline.Control.ActualWidth + 1000)
                dX2 = m_oParentTimeline.Control.ActualWidth + 1000;

            GeneralTransform aobjGeneralTransform = ((UIElement)m_oParentTimeline.Control).TransformToVisual(LayoutRoot as UIElement);
            Point pt1 = aobjGeneralTransform.Transform(new Point(dX1,m_oParentTimeline.Control.ActualHeight));
            Point pt2 = aobjGeneralTransform.Transform(new Point(dX2, m_oParentTimeline.Control.ActualHeight));

            double dWidth = (pt2.X - pt1.X);
            double dLeft = pt1.X;
            double dHeight = pt2.Y;

            WhereAmI.Width = dWidth + (dPadding * 2);
            Canvas.SetLeft(WhereAmI, dLeft - dPadding);

            WhereAmI.Margin = new Thickness(dPadding, 0, dPadding, 0); ;
            WhereAmI.UpdateWidths();
        }

        public IViewModel VM
        {
            get
            {
                return VacationVM as IViewModel;
            }
            set
            {
                if (value == null)
                    VacationVM = null;
                else
                {
                    System.Diagnostics.Debug.Assert(value is IVacationViewModel);
                    VacationVM = value as IVacationViewModel;
                }
            }
        }

        private IVacationViewModel m_oVacationVM = null;
        protected IVacationViewModel VacationVM
        {
            get
            {
                return m_oVacationVM;
            }
            private set
            {
                if (value != m_oVacationVM)
                {
                    m_oVacationVM = value as IVacationViewModel;
                }
            }
        }

        #region IInterfaceElement Members

        public Control Control
        {
            get { return this as Control; }
        }

        #endregion

        #region ITitle Members

        public DateTime FirstIconDate
        {
            get { return VacationVM.FirstIconDate; }
        }

        public DateTime LastIconDate
        {
            get { return VacationVM.LastIconDate; }
        }

        #endregion
    }
}
