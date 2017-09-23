using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ImageBoard.Controls
{
    public class SafePicture : ContentControl
    {
        public static readonly DependencyProperty ImageUriProperty = DependencyProperty.Register(
            "ImageUri",
            typeof(Uri),
            typeof(SafePicture),
            new PropertyMetadata(ImageUriPropertyChanged));

        public static readonly DependencyProperty RefererProperty = DependencyProperty.Register(
            "Referer",
            typeof(string),
            typeof(SafePicture),
            new PropertyMetadata(null));

        public SafePicture()
        {
            this.Loaded += this.SafePictureLoaded;
            this.Unloaded += this.SafePictureUnloaded;
        }

        public Uri ImageUri
        {
            get
            {
                return this.GetValue(ImageUriProperty) as Uri;
            }

            set
            {
                this.SetValue(ImageUriProperty, value);
            }
        }

        public string Referer
        {
            get
            {
                return this.GetValue(RefererProperty) as string;
            }

            set
            {
                this.SetValue(RefererProperty, value);
            }
        }

        protected Image Image
        {
            get
            {
                return this.Content as Image;
            }
        }

        public static void ImageUriPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var image = target as SafePicture;

            if (image != null)
            {
                image.Load();
            }
        }

        protected void Load()
        {
            var uri = this.ImageUri;
            var referer = this.Referer;

            if (uri == null)
            {
                if (this.Image != null)
                {
                    this.Image.Source = null;
                }

                return;
            }

            if (referer == null)
            {
                var bitmap = new BitmapImage { UriSource = uri };

                if (this.Image != null)
                {
                    this.Image.Source = bitmap;
                }
            }
            else
            {
                var webclient = new WebClient();

                webclient.Headers[HttpRequestHeader.Referer] = referer;

                webclient.OpenReadCompleted += WebclientOpenReadCompleted;

                webclient.OpenReadAsync(uri);
            }
        }

        private void WebclientOpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            var bitmap = new BitmapImage();
            bitmap.SetSource(e.Result);

            this.Image.Source = bitmap;
        }

        private void SafePictureLoaded(object sender, RoutedEventArgs e)
        {
            var image = this.Image;

            if (image != null && image.Source == null)
            {
                this.Load();
            }
        }

        private void SafePictureUnloaded(object sender, RoutedEventArgs e)
        {
            var image = this.Content as Image;

            if (image != null)
            {
                image.Source = null;
            }
        }
    }
}
