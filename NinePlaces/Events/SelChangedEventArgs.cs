
namespace NinePlaces
{
    public delegate void SelChangedEventHandler(object sender, SelChangedEventArgs args);
    public class SelChangedEventArgs
    {
        public SelChangedEventArgs(bool in_bSelected)
        {
            Selected = in_bSelected;
        }

        public bool Selected { get; set; }

    }
}
