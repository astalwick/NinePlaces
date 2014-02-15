using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common.Interfaces;

namespace Editors
{
	public partial class TodoList : UserControl
	{
		public TodoList()
		{
			// Required to initialize variables
			InitializeComponent();
            AddItem.Click += new RoutedEventHandler(AddItem_Click);

		}

        private IListViewModel m_oActiveList = null;
        public IListViewModel ActiveList
        {
            get
            {
                return m_oActiveList;
            }
            set
            {
                if (m_oActiveList != value)
                {
                    m_oActiveList = value;
                    DataContext = m_oActiveList;

                    AddItem.Visibility = m_oActiveList == null ? Visibility.Collapsed : Visibility.Visible;
                }
            }
        }

        void AddItem_Click(object sender, RoutedEventArgs e)
        {
            AddNewItem();
        }

        public void AddNewItem()
        {
            ActiveList.NewListItem("New item", false);
        }
	}
}