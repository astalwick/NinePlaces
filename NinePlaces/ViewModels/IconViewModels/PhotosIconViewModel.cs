using NinePlaces.Models;
using Common.Interfaces;

namespace NinePlaces.ViewModels.IconViewModels
{
    public class PhotosIconViewModel: IconViewModel
    {
        public PhotosIconViewModel(IHierarchicalPropertyModel in_oXMLModel, int in_nIconID)
        {
            IconType = IconTypes.Photos;

            Model = in_oXMLModel;
        }
    }
}
