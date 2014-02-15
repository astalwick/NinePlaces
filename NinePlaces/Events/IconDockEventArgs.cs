
namespace NinePlaces
{
    public delegate void IconDockEventHandler(object sender, IconDockEventArgs args);
    public class IconDockEventArgs
    {
        /// <summary>
        /// Blank Constuctor
        /// </summary>
        public IconDockEventArgs()
        {
        }

        public IconDockEventArgs(IIcon in_oIcon)
        {
            this.Icon = in_oIcon;
        }


        /// <summary>
        /// Gets or sets the horizontal change of the drag
        /// </summary>
        public IIcon Icon
        {
            get;
            set;
        }

    }
}
