using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Phone.Controls;

namespace ImageBoardBrowser
{
    public partial class SplashScreen : PhoneApplicationPage
    {
        public SplashScreen()
        {
            this.InitializeComponent();
        }

        private void PhoneApplicationPageLoaded(object sender, RoutedEventArgs e)
        {
            this.FadeInAnimation.Begin();

            Dispatcher.BeginInvoke(() =>
            {
                ((App)Application.Current).Init();

                try
                {
                    using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!isolatedStorage.DirectoryExists(ViewModel.TempDirectory))
                        {
                            isolatedStorage.CreateDirectory(ViewModel.TempDirectory);
                        }

                        ViewModel.ClearFiles(ViewModel.TempDirectory, "/*");
                        ViewModel.ClearFiles(null, "*.jpg");
                        
                        if (isolatedStorage.DirectoryExists(ViewModel.SavedPicturesDirectory))
                        {
                            var files = new List<string>(isolatedStorage.GetFileNames(ViewModel.SavedPicturesDirectory + "/*"));

                            Task.Factory.StartNew(() => ViewModel.CleanPictures(files));
                        }
                    }
                }
                catch (Exception)
                {
#if DEBUG
                    throw;
#endif
                }

                this.NavigationService.Navigate("/SelectSite.xaml");
            });
        }
    }
}