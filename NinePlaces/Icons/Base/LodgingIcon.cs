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
    public abstract class LodgingIcon : IconControlBase
    {
        public LodgingIcon()
        {
        }

        public LodgingIcon(IIconViewModel in_oIconVM)
            : this()
        {
            IconVM = in_oIconVM;
        }

        public override IconClass IconClass
        {
            get
            {
                return IconClass.Lodging;
            }
        }
    }
}
