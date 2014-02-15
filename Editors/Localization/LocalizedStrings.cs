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
using Common;
using System.ComponentModel;
using BasicEditors;

namespace Localization
{
    public class LocalizedStrings : INotifyPropertyChanged
    {
        public LocalizedStrings()
        {
            CrossAssemblyNotifyContainer.LocalizationChange += new EventHandler(CrossAssemblyNotifyContainer_LocalizationChange);
        }

        void CrossAssemblyNotifyContainer_LocalizationChange(object sender, EventArgs e)
        {
            OnPropertyChanged("StringLibrary");
        }

        private static StringLibrary sl = new StringLibrary();
        public StringLibrary StringLibrary { get { return sl; } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
