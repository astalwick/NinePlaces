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
    public class MyTabItem : TabItem
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ContentControl cc = (ContentControl)this.GetTemplateChild("HeaderTopSelected");
            cc.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            cc = (ContentControl)this.GetTemplateChild("HeaderTopUnselected");
            cc.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        }
    }

    public partial class BeachDetailsEditor : BaseDetailsEditor, IInterfaceElement
    {
        public BeachDetailsEditor(IIconViewModel in_oIconVM)
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
