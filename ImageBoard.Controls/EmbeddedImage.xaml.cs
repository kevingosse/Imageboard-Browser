using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;

using ImageTools;
using ImageTools.Controls;
using ImageTools.IO.Gif;

namespace ImageBoard.Controls
{
    public partial class EmbeddedImage
    {
        public static readonly DependencyProperty ImageUriProperty = DependencyProperty.Register(
            "ImageUri",
            typeof(Uri),
            typeof(EmbeddedImage),
            new PropertyMetadata(ImageUriPropertyChanged));

        public static readonly DependencyProperty RefererProperty = DependencyProperty.Register(
            "Referer",
            typeof(string),
            typeof(EmbeddedImage),
            new PropertyMetadata(null));

        public EmbeddedImage()
        {
            this.Loaded += EmbeddedImageLoaded;
            this.Unloaded += this.EmbeddedImageUnloaded;

            this.InitializeComponent();

            ImageTools.IO.Decoders.AddDecoder<GifDecoder>();
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

        public static void ImageUriPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var image = target as EmbeddedImage;

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
                this.ButtonRetry.Visibility = Visibility.Collapsed;
                this.ImageError.Visibility = Visibility.Collapsed;
                this.ProgressBar.Visibility = Visibility.Collapsed;

                this.ImageContent.Visibility = Visibility.Collapsed;
                this.ConvertedImageContent.Visibility = Visibility.Collapsed;

                var source = this.ImageContent.Source;

                this.ImageContent.Source = null;

                this.DisposeImage(source as BitmapImage);

                this.ConvertedImageContent.Source = null;

                return;
            }

            this.ButtonRetry.Visibility = Visibility.Collapsed;
            this.ImageError.Visibility = Visibility.Collapsed;
            this.ProgressBar.Visibility = Visibility.Visible;

            this.ProgressBar.Value = 0;

            if (uri.ToString().EndsWith(".gif"))
            {
                this.ConvertedImageContent.Visibility = Visibility.Visible;
                this.ImageContent.Visibility = Visibility.Collapsed;

                var converter = new ImageConverter();

                converter.Referer = referer;

                var image = (ExtendedImage)converter.Convert(uri, typeof(ExtendedImage), null, null);
                image.DownloadProgress += (_, eventArgs) => this.BitmapDownloadProgress(eventArgs.ProgressPercentage);
                image.LoadingFailed += (_, eventArgs) => this.Dispatcher.BeginInvoke(() => this.BitmapImageFailed(eventArgs.ExceptionObject as Exception));
                image.LoadingCompleted += (_, __) => this.Dispatcher.BeginInvoke(this.BitmapImageOpened);
                this.ConvertedImageContent.Source = image;
            }
            else if (referer != null)
            {
                this.ConvertedImageContent.Visibility = Visibility.Collapsed;
                this.ImageContent.Visibility = Visibility.Visible;

                var webclient = new WebClient();

                webclient.Headers[HttpRequestHeader.Referer] = referer;

                webclient.OpenReadCompleted += this.WebclientOpenReadCompleted;

                webclient.OpenReadAsync(uri);
            }
            else
            {
                this.ConvertedImageContent.Visibility = Visibility.Collapsed;
                this.ImageContent.Visibility = Visibility.Visible;

                var bitmap = new BitmapImage();
                bitmap.DownloadProgress += this.OnDownloadProgress;
                bitmap.ImageFailed += this.OnImageFailed;
                bitmap.ImageOpened += this.OnImageOpened;
                bitmap.UriSource = uri;

                this.ImageContent.Source = bitmap;
            }
        }

        private void EmbeddedImageLoaded(object sender, RoutedEventArgs e)
        {
            if (this.ImageContent.Source == null)
            {
                this.Load();
            }
        }

        private void WebclientOpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                var bitmap = new BitmapImage();
                bitmap.DownloadProgress += this.OnDownloadProgress;
                bitmap.ImageFailed += this.OnImageFailed;
                bitmap.ImageOpened += this.OnImageOpened;

                try
                {
                    bitmap.SetSource(e.Result);
                }
                catch (Exception ex)
                {
                    this.BitmapImageFailed(ex);
                    return;
                }

                this.ImageContent.Source = bitmap;
            }
            else
            {
                this.BitmapImageFailed(e.Error);
            }
        }

        private void OnImageOpened(object sender, RoutedEventArgs eventArgs)
        {
            this.BitmapImageOpened();
        }

        private void OnImageFailed(object sender, ExceptionRoutedEventArgs eventArgs)
        {
            this.BitmapImageFailed(eventArgs.ErrorException);
        }

        private void OnDownloadProgress(object sender, DownloadProgressEventArgs eventArgs)
        {
            this.BitmapDownloadProgress(eventArgs.Progress);
        }

        private void BitmapImageOpened()
        {
            this.ButtonRetry.Visibility = Visibility.Collapsed;
            this.ImageError.Visibility = Visibility.Collapsed;
            this.ProgressBar.Visibility = Visibility.Collapsed;
        }

        private void BitmapImageFailed(Exception ex)
        {
            Debug.WriteLine("BitmapImageFailed " + ex);

            this.ButtonRetry.Visibility = Visibility.Visible;
            this.ImageError.Visibility = Visibility.Visible;
            this.ProgressBar.Visibility = Visibility.Collapsed;
        }

        private void BitmapDownloadProgress(int progress)
        {
            this.ProgressBar.Value = progress;
        }

        private void ButtonRetryTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.Load();
            e.Handled = true;
        }

        private void EmbeddedImageUnloaded(object sender, RoutedEventArgs e)
        {
            //this.ConvertedImageContent.Stop();

            var source = this.ImageContent.Source;

            this.ImageContent.Source = null;

            this.DisposeImage(source as BitmapImage);

            this.ConvertedImageContent.Stop();

            var oldSource = this.ConvertedImageContent.Source;
            this.ConvertedImageContent.Source = null;

            if (oldSource != null)
            {
                oldSource.Dispose();
            }
        }

        private void DisposeImage(BitmapImage image)
        {
            if (image != null)
            {
                try
                {
                    image.DownloadProgress -= this.OnDownloadProgress;
                    image.ImageFailed -= this.OnImageFailed;
                    image.ImageOpened -= OnImageOpened;

                    using (var ms = new MemoryStream(new byte[] { 0x0 }))
                    {
                        image.SetSource(ms);
                    }

                    image.UriSource = null;
                }
                catch (Exception)
                {
                    image.UriSource = null;
                }
            }
        }
    }
}
