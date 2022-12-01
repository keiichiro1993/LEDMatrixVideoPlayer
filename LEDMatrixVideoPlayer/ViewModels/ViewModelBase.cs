using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace LEDMatrixVideoPlayer.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _IsLoading = false;
        public bool IsLoading
        {
            get { return _IsLoading; }
            set
            {
                _IsLoading = value;
                RaisePropertyChanged();
                RaisePropertyChanged("LoadingPanelVisibility");
            }
        }

        private string _LoadingMessage = "Please wait...";
        public string LoadingMessage
        {
            get { return _LoadingMessage; }
            set
            {
                _LoadingMessage = value;
                RaisePropertyChanged();
            }
        }

        public Visibility LoadingPanelVisibility
        {
            get { return IsLoading ? Visibility.Visible : Visibility.Collapsed; }
        }
    }
}
