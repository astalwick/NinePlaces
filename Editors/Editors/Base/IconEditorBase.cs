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

namespace NinePlaces.Editors.Base
{
    public class IconEditorBase : UserControl
    {
        private int m_nCurrentExpandState = -1;     // remembers the current expand state.
        protected int CurrentExpandState
        {
            get
            {
                return m_nCurrentExpandState;
            }
            set
            {
                if (m_nCurrentExpandState != value)
                {
                    m_nCurrentExpandState = value;

                    if (m_nCurrentExpandState > ExpandStates - 1)
                        // whoa, too high!
                        m_nCurrentExpandState = ExpandStates - 1;
                    else if (m_nCurrentExpandState < 0)
                        // whoa, too low!
                        m_nCurrentExpandState = 0;
                    
                    // change the visualstate.
                    bool bStateChangeSucceeded = false;
                    if (m_nCurrentExpandState == 0)
                        bStateChangeSucceeded = VisualStateManager.GoToState(this, "Small", true);
                    else if (m_nCurrentExpandState == 1)
                        bStateChangeSucceeded = VisualStateManager.GoToState(this, "Medium", true);
                    else if (m_nCurrentExpandState == 2)
                        bStateChangeSucceeded = VisualStateManager.GoToState(this, "Large", true);

                    if (ContractButton != null && ExpandButton != null)
                    {
                        // if we have contract/expand buttons, we need to update them for
                        // our current expand states.  (eg, in small mode, we can't contract
                        // so we shouldn't show that button).
                        if (ExpandStates == 1)
                        {
                            ExpandButton.Visibility = Visibility.Collapsed;
                            ContractButton.Visibility = Visibility.Collapsed;
                        }
                        else if (m_nCurrentExpandState == ExpandStates - 1)
                        {
                            ContractButton.Visibility = Visibility.Visible;
                            ExpandButton.Visibility = Visibility.Collapsed;
                        }
                        else if (m_nCurrentExpandState == 0)
                        {
                            ExpandButton.Visibility = Visibility.Visible;
                            ContractButton.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            ExpandButton.Visibility = Visibility.Visible;
                            ContractButton.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        // number of expand states in total
        protected int ExpandStates
        {
            get;
            set;
        }

        // we maintain a reference to our parent.  it helps.
        private IconControlBase m_oIconParent = null;
        protected IconControlBase ParentIcon
        {
            get
            {
                return m_oIconParent;
            }
        }

        // not used currently, but will be in the future.
        public TimeSpan Duration { get; set; }
        
        // our contract button!
        private Button m_btnContract = null;
        protected Button ContractButton
        {
            get
            {
                return m_btnContract;
            }
            set
            {
                m_btnContract = value;
                ContractButton.Click += new RoutedEventHandler(ContractButton_Click);
            }
        }

        // our expand button!
        private Button m_btnExpand = null;
        protected Button ExpandButton
        {
            get
            {
                return m_btnExpand;
            }
            set
            {
                m_btnExpand = value;
                ExpandButton.Click += new RoutedEventHandler(ExpandButton_Click);
            }
        }

        // the consumer of this base class is allowed to specify one textbox as
        // the 'current date time' textbox.  the textbox that represents (and stays
        // in sync with) the icon's current time on the timeline.
        private DateTimeTextBox m_tbIconTime = null;
        protected DateTimeTextBox IconTimeTextBox
        {
            get
            {
                return m_tbIconTime;
            }
            set
            {
                m_tbIconTime = value;
                m_tbIconTime.DisplayDateTime = ParentIcon.DockTime;
                m_tbIconTime.DisplayDateChanged += new EventHandler(IconTimeTextBox_DisplayDateChanged);
            }
        }

        // are we open
        public virtual bool IsOpen
        {
            get
            {
                return Visibility == Visibility.Visible;
            }
            set
            {
                if ((bool)value)
                {
                    Visibility = Visibility.Visible;
                    Focus();
                }
                else
                {
                    ParentIcon.Focus();
                    // we need to tell everyone to close their calendars so that we don't
                    // have extra stuff littering the interface.
                    CloseCalendars();

                    Visibility = Visibility.Collapsed;
                }
            }
        }

        public IconEditorBase()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                throw new Exception();
            }
        }

        public IconEditorBase(IconControlBase in_oIconOwner)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                throw new Exception();
            }

            // keep track of our parent icon.
            m_oIconParent = in_oIconOwner as IconControlBase;
            // also - we want to know when our parent icon's time has changed!
            m_oIconParent.DockTimeChanged += new EventHandler(ParentIcon_DockTimeChanged);

            KeyDown += new KeyEventHandler(IconEditorBase_KeyDown);

            // mousebuttondown and up are important to us.
            MouseLeftButtonDown += new MouseButtonEventHandler(IconEditorMouseLeftButtonDown);
            MouseLeftButtonUp += new MouseButtonEventHandler(IconEditorMouseLeftButtonUp);

            Visibility = Visibility.Collapsed;
        }

        void IconEditorBase_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                CloseCalendars();
                IsOpen = false;
                e.Handled = true;
            }
        }

        protected virtual void IconEditorMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // gotta absorbe the click so that the iconcontrol doesn't get it.
            CloseCalendars();
            e.Handled = true;
        }

        protected virtual void IconEditorMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // gotta absorb the click so that the iconcontrol doesn't get it.
            e.Handled = true;
        }

        void IconTimeTextBox_DisplayDateChanged(object sender, EventArgs e)
        {
            // if the displaydate has changed from some source other than the 
            // parent icon, then we should set the parenticon to the new time,
            // and animate ourselves over there.
            if (ParentIcon.DockTime.Ticks != IconTimeTextBox.DisplayDateTime.Ticks)
            {
                ParentIcon.DockTime = IconTimeTextBox.DisplayDateTime;
                ParentIcon.AnimateToDockTime();
            }
        }

        protected virtual void ParentIcon_DockTimeChanged(object sender, EventArgs e)
        {
            if (IconTimeTextBox != null)
            {
                // ok our parent's dock time has changed - we need to update
                // any our 'current date time' textbox, if there is one.
                if (ParentIcon.DockTime.Ticks != IconTimeTextBox.DisplayDateTime.Ticks)
                {
                    IconTimeTextBox.DisplayDateTime = ParentIcon.DockTime;
                    CloseCalendars();
                }
            }
        }

        protected virtual void CloseCalendars()
        {
            if (IconTimeTextBox != null)
                IconTimeTextBox.IsCalendarOpen = false;
        }

        protected virtual void ContractButton_Click(object sender, RoutedEventArgs e)
        {
            // this counts as a 'clse me' message.
            CloseCalendars();

            // contract - we go up one state level.
            VisualStateManager.GoToState(ExpandButton, "Normal", false);
            VisualStateManager.GoToState(ContractButton, "Normal", false);

            CurrentExpandState--;
        }

        protected virtual void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            CloseCalendars();

            VisualStateManager.GoToState(ExpandButton, "Normal", false);
            VisualStateManager.GoToState(ContractButton, "Normal", false);

            CurrentExpandState++;
        }
    }
}
