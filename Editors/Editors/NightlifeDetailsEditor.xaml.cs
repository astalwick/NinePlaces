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
using BasicEditors;
using System.Windows.Controls.Primitives;

namespace Editors
{
    public partial class NightlifeDetailsEditor : BaseDetailsEditor, IInterfaceElement
    {
        public NightlifeDetailsEditor(IIconViewModel in_oIconVM)
        {
            // Required to initialize variables
            InitializeComponent();
            DataContext = in_oIconVM;
            IconClass = Common.Interfaces.IconClass.Activity;
        }

        public Control Control
        {
            get
            {
                return this as Control;
            }
        }
    }
}
