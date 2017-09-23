using System.ComponentModel;

using Microsoft.Phone.Controls;

namespace ImageBoardBrowser
{
    public class BasePage : PhoneApplicationPage, INotifyPropertyChanged
    {
        internal BasePage()
        {
            this.Loaded += this.BasePageLoaded;

            this.DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModel ViewModel
        {
            get
            {
                return App.ViewModel;
            }
        }

        private void BasePageLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.SupportedOrientations = App.ViewModel.IsOrientationLockActivated ? SupportedPageOrientation.Portrait : SupportedPageOrientation.PortraitOrLandscape;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (App.ViewModel.IsOrientationLockActivated)
            {
                if (this.Orientation == PageOrientation.PortraitUp)
                {
                    this.SupportedOrientations = SupportedPageOrientation.Portrait;
                }
            }
            else
            {
                this.SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;
            }
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            base.OnOrientationChanged(e);

            if (App.ViewModel.IsOrientationLockActivated)
            {
                if (e.Orientation == PageOrientation.PortraitUp)
                {
                    this.SupportedOrientations = SupportedPageOrientation.Portrait;
                }
            }
            else
            {
                this.SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
