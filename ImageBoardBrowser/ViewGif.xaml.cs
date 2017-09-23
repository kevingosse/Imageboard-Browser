using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using ImageTools;
using ImageTools.IO.Gif;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;

namespace ImageBoardBrowser
{
    public partial class ViewGif : PhoneApplicationPage
    {
        public ViewGif()
        {
            this.InitializeComponent();

            ImageTools.IO.Decoders.AddDecoder<GifDecoder>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                // Get a dictionary of query string keys and values.
                var queryStrings = this.NavigationContext.QueryString;

                // Ensure that there is at least one key in the query string, and check whether the "token" key is present.
                if (queryStrings.ContainsKey("token"))
                {
                    // Retrieve the photo from the media library using the token passed to the app.
                    var library = new MediaLibrary();
                    var photoFromLibrary = library.GetPictureFromToken(queryStrings["token"]);

                    if (photoFromLibrary.Name.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase))
                    {
                        using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            string fullPath = ViewModel.SavedPicturesDirectory + "/" + photoFromLibrary.Name;

                            if (!isolatedStorage.FileExists(fullPath))
                            {
                                MessageBox.Show("The picture could not be found");
                                Application.Current.Terminate();
                                return;
                            }

                            // No using block, the ExtendedImage control will take care of closing the stream
                            var stream = isolatedStorage.OpenFile(fullPath, System.IO.FileMode.Open);

                            var image = new ExtendedImage { IsStreaming = true };

                            image.SetSource(stream);

                            this.ConvertedImage.Source = image;

                            this.ConvertedImage.Visibility = Visibility.Visible;
                            this.ClassicalImage.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        var stream = photoFromLibrary.GetImage();

                        var image = new BitmapImage { CreateOptions = BitmapCreateOptions.None };

                        image.ImageFailed += this.ClassicalImageImageFailed;

                        image.SetSource(stream);

                        this.ClassicalImage.Source = image;

                        this.ConvertedImage.Visibility = Visibility.Collapsed;
                        this.ClassicalImage.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ShowMessageBox("An error occured: " + ex.Message);
                Application.Current.Terminate();
                return;
            }
        }

        private void ConvertedImageLoadingCompleted(object sender, EventArgs e)
        {
            SystemTray.SetProgressIndicator(this, null);
        }

        private void ConvertedImageLoadingFailed(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("An error occured while loading the picture: " + e.ExceptionObject);
            Application.Current.Terminate();
        }

        private void ClassicalImageImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("An error occured while loading the picture: " + e.ErrorException);
            Application.Current.Terminate();
        }
    }
}