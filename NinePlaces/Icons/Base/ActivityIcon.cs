using System.Windows;
using System.Windows.Media;
using Common.Interfaces;

namespace NinePlaces
{
    public abstract class ActivityIcon : IconControlBase
    {
        public ActivityIcon()
        {
        }

        public ActivityIcon(IIconViewModel in_oIconVM)
            : this()
        {
            IconVM = in_oIconVM;
        }

        public override IconClass IconClass
        {
            get
            {
                return IconClass.Activity;
            }
        }
    }
}
