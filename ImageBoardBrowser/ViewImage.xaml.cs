using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Windows.Storage;
using Coding4Fun.Phone.Controls;
using GIFSurface;
using ImageBoard.Parsers.Common;

using ImageTools;
using ImageTools.Controls;
using ImageTools.IO.Gif;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;

using Telerik.Windows.Controls;

namespace ImageBoardBrowser
{
    public partial class ViewImage : PhoneApplicationPage, INotifyPropertyChanged
    {
        public ViewImage()
        {
            this.ApplicationBarSteps = new List<Tuple<int, double>>
            {
                Tuple.Create(0, 0.3),
                Tuple.Create(110, 0.4),
                Tuple.Create(200, 0.8)

                //Tuple.Create(40, 0.3),
                //Tuple.Create(150, 0.4),
                //Tuple.Create(240, 0.8)
            };

            this.CurrentApplicationBarStep = this.ApplicationBarSteps.First();

            this.InitializeComponent();

            this.DrawingSurface = new DrawingSurface { Name = "DrawingSurface" };
            
            this.DrawingSurface.SizeChanged += this.DrawingSurfaceSizeChanged;
            
            this.ContentPanel.Children.Add(this.DrawingSurface);

            this.DataContext = this;

            this.SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;

            ImageTools.IO.Decoders.AddDecoder<GifDecoder>();
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModel ViewModel
        {
            get
            {
                return App.ViewModel;
            }
        }

        public int SelectedImageIndex { get; protected set; }

        public int ImageCount { get; protected set; }

        public bool IsOrientationLockActivated
        {
            get
            {
                return this.SupportedOrientations != SupportedPageOrientation.PortraitOrLandscape;
            }

            set
            {
                if (value)
                {
                    this.SupportedOrientations = (this.Orientation & PageOrientation.Portrait) != 0 ? SupportedPageOrientation.Portrait : SupportedPageOrientation.Landscape;
                }
                else
                {
                    this.SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;
                }

                this.ViewModel.IsOrientationLockActivatedForPictures = value;
            }
        }

        protected Uri CurrentUri { get; set; }

        protected List<Tuple<int, double>> ApplicationBarSteps { get; private set; }

        protected Tuple<int, double> CurrentApplicationBarStep { get; set; }

        protected double InitialScale { get; set; }

        protected BitmapImage Source { get; set; }

        protected Message NextMessage { get; set; }

        protected Message PreviousMessage { get; set; }

        protected bool IsDiaporamaRunning { get; set; }

        protected Uri PageUri { get; set; }

        protected DiaporamaTimer DiaporamaTimer { get; set; }

        protected DateTime? NextImageTime { get; set; }

        private DrawingSurface DrawingSurface { get; set; }

        private AnimatedWrapper AnimatedWrapper { get; set; }

        protected void EnableApplicationBar(bool isSaveButtonEnabled)
        {
            this.MenuSave.IsEnabled = isSaveButtonEnabled;
            this.MenuPlay.IsEnabled = true;
        }

        protected void DisableApplicationBar()
        {
            this.MenuSave.IsEnabled = false;
            this.MenuPlay.IsEnabled = false;
        }

        protected void LoadImage()
        {
            this.DisableApplicationBar();

            this.PreviewListBox.BringIntoView(App.ViewModel.Context.Message);
            int index = 0;
            int count = 0;

            foreach (var element in this.ViewModel.FilteredMessages)
            {
                count++;

                if (element.Id == this.ViewModel.Context.Message.Id)
                {
                    index = count;
                }
            }

            this.ImageCount = count;
            this.SelectedImageIndex = index;

            this.RaisePropertyChanged("ImageCount");
            this.RaisePropertyChanged("SelectedImageIndex");

            this.NextImageTime = null;

            this.HideErrors();

            GC.Collect();

            this.NextMessage = this.GetNextMessage();
            this.PreviousMessage = this.GetPreviousMessage();

            var indicator = new ProgressIndicator
            {
                IsVisible = true,
                Value = 0,
                IsIndeterminate = false,
                Text = "Loading..."
            };

            SystemTray.SetProgressIndicator(this, indicator);

            var url = App.ViewModel.Context.Message.ImageLink;

            if (url.EndsWith(".gif"))
            {
                this.CurrentUri = new Uri(url);

                var converter = new ImageConverter();

                if (App.ViewModel.Context.Message.Referer != null)
                {
                    converter.Referer = App.ViewModel.Context.Message.Referer;
                }

                this.HideControls();

                bool isStreaming = this.ViewModel.IsGifStreamingEnabled;

                var parameters = new ExtendedImage.ExtendedImageParameters { IsPreprocessed = false, IsStreaming = isStreaming };

                var image = (ExtendedImage)converter.Convert(url, typeof(ExtendedImage), parameters, null);

                image.DownloadProgress += (_, eventArgs) => this.ImageDownloadProgress(new Uri(url), eventArgs.ProgressPercentage);
                image.DownloadCompleted += this.GifImageDownloadCompleted;

                if (isStreaming)
                {
                    var source = this.ConvertedImage.Source;

                    if (source != null)
                    {
                        source.Dispose();
                    }

                    this.ConvertedImage.Source = image;
                }

                this.ConvertedImage.Visibility = Visibility.Visible;
            }
            else if (url.EndsWith(".webm"))
            {
                this.LoadWebM(url);
            }
            else if (App.ViewModel.Context.Message.Referer != null)
            {
                var webclient = new WebClient();

                webclient.Headers[HttpRequestHeader.Referer] = App.ViewModel.Context.Message.Referer;

                var uri = new Uri(url);
                webclient.DownloadProgressChanged += (s, e) => this.ImageDownloadProgress(uri, e.ProgressPercentage);
                webclient.OpenReadCompleted += (sender, e) =>
                {
                    this.HideControls();

                    this.ClassicalImage.Visibility = Visibility.Visible;

                    var exception = e.Error;

                    try
                    {
                        if (exception == null)
                        {
                            var image = new BitmapImage();

                            image.DownloadProgress += this.ImageDownloadProgress;
                            image.CreateOptions = BitmapCreateOptions.None;

                            image.SetSource(e.Result);

                            this.ClassicalImageImageOpened(image, uri);
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }

                    if (exception != null)
                    {
                        this.ClassicalImageImageFailed(null, exception);
                    }
                };

                this.CurrentUri = uri;

                webclient.OpenReadAsync(uri);
            }
            else
            {
                var image = new BitmapImage();

                image.DownloadProgress += this.ImageDownloadProgress;
                image.CreateOptions = BitmapCreateOptions.None;

                image.ImageOpened += this.ClassicalImageImageOpened;
                image.ImageFailed += this.ClassicalImageImageFailed;

                var uri = new Uri(url);

                this.CurrentUri = uri;

                image.UriSource = uri;

                this.HideControls();
                
                this.ClassicalImage.Visibility = Visibility.Visible;
            }
        }

        protected void PreviewLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Dispatcher.BeginInvoke(() => this.PreviewListBox.BringIntoView(this.ViewModel.Context.Message));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        protected void GifImageDownloadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            this.DisposeImage(this.ClassicalImage.Source as BitmapImage);
            this.ClassicalImage.Source = null;

            var image = (ExtendedImage)sender;

            if (image.UriSource != this.CurrentUri)
            {
                image.Dispose();
                return;
            }

            this.ConvertedImageLoadingCompleted(image, EventArgs.Empty);
        }

        protected void ImageDownloadProgress(object sender, DownloadProgressEventArgs eventArgs)
        {
            var image = (BitmapImage)sender;

            this.ImageDownloadProgress(image.UriSource, eventArgs.Progress);
        }

        private void ImageDownloadProgress(Uri uri, double progress)
        {
            if (uri != this.CurrentUri)
            {
                return;
            }

            var progressIndicator = SystemTray.GetProgressIndicator(this);

            if (progressIndicator != null)
            {
                this.Dispatcher.BeginInvoke(() => progressIndicator.Value = progress / 100.0);
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (App.ViewModel.Messages == null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
                return;
            }

            this.IsOrientationLockActivated = this.ViewModel.IsOrientationLockActivatedForPictures;
            this.RaisePropertyChanged("IsOrientationLockActivated");

            this.PageUri = this.NavigationService.Source;

            base.OnNavigatedTo(e);

            this.LoadImage();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            GC.Collect();

            this.StopDiaporama();

            var source = this.ConvertedImage.Source;

            if (source != null)
            {
                source.Dispose();
            }

            this.DisposeImage(this.ClassicalImage.Source as BitmapImage);

            this.ClassicalImage.Source = null;
            this.ConvertedImage.Source = null;
            this.Source = null;

            this.ConvertedImage.Stop();
        }

        private void ClassicalImageImageFailed(object sender, Exception e)
        {
            this.ScheduleNextImage();

            this.OnError(e);
        }

        private void ClassicalImageImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            this.ClassicalImageImageFailed(sender, e.ErrorException);
        }

        private void DisposeImage(BitmapImage image)
        {
            if (image != null)
            {
                try
                {
                    image.DownloadProgress -= this.ImageDownloadProgress;
                    image.ImageOpened -= this.ClassicalImageImageOpened;
                    image.ImageFailed -= this.ClassicalImageImageFailed;

                    using (var ms = new MemoryStream(new byte[] { 0x0 }))
                    {
                        image.SetSource(ms);
                    }
                }
                catch
                {
                }
                finally
                {
                    image.UriSource = null;
                }
            }
        }

        private void ClassicalImageImageOpened(object sender, RoutedEventArgs e)
        {
            var image = (BitmapImage)sender;

            var uri = image.UriSource;

            this.ClassicalImageImageOpened(image, uri);
        }

        private void ClassicalImageImageOpened(BitmapImage image, Uri uri)
        {
            if (uri != this.CurrentUri)
            {
                this.DisposeImage(image);
                return;
            }

            var size = image.PixelHeight * image.PixelWidth * 4;

            if (size > 30000000)
            {
                decimal ratio = (decimal)image.PixelWidth / image.PixelHeight;

                int newWidth;
                int newHeight;

                if (image.PixelWidth > image.PixelHeight)
                {
                    newWidth = 1500;
                    newHeight = (int)(newWidth / ratio);
                }
                else
                {
                    newHeight = 1500;
                    newWidth = (int)(newHeight * ratio);
                }

                image.DecodePixelHeight = newHeight;
                image.DecodePixelWidth = newWidth;
            }

            this.CleanClassicalImage();
            
            this.ClassicalImage.Source = image;

            this.ContentTranslate.X = 0;
            this.ClassicalImage.Zoom = 1;
            this.ClassicalImage.Pan = new Point(0, 0);

            this.CleanConvertedImage(true);
            this.CleanWebM();

            this.ScheduleNextImage();

            SystemTray.SetProgressIndicator(this, null);
            this.EnableApplicationBar(true);
        }

        private void ScheduleNextImage()
        {
            this.NextImageTime = DateTime.UtcNow.AddSeconds(App.ViewModel.SlideShowDelay);
        }

        private Message GetPreviousMessage()
        {
            return App.ViewModel.Messages.Reverse()
                .SkipWhile(m => m.Id != App.ViewModel?.Context?.Message.Id)
                .Skip(1)
                .FirstOrDefault(m => !string.IsNullOrEmpty(m.ImageLink));
        }

        private Message GetNextMessage()
        {
            return App.ViewModel.Messages
                .SkipWhile(m => m.Id != App.ViewModel?.Context?.Message.Id)
                .Skip(1)
                .FirstOrDefault(m => !string.IsNullOrEmpty(m.ImageLink));
        }

        private async void MenuSaveClick(object sender, EventArgs e)
        {
            try
            {
                if (App.ViewModel.Context.Message.ImageLink.EndsWith(".webm"))
                {
                    string fileName = App.ViewModel.Context.Message.ImageLink.Split('/').Last();

                    var file = await KnownFolders.VideosLibrary.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

                    string path = ViewModel.SavedPicturesDirectory + "/temp.webm";

                    using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!isolatedStorage.FileExists(path))
                        {
                            MessageBox.Show("An error occured while trying to read the webm file. Please refresh the page and try again");
                            return;
                        }

                        using (var sourceFile = isolatedStorage.OpenFile(path, FileMode.Open))
                        {
                            using (var stream = await file.OpenStreamForWriteAsync())
                            {
                                await sourceFile.CopyToAsync(stream);
                            }
                        }
                    }
                }
                else if (App.ViewModel.Context.Message.ImageLink.EndsWith(".gif"))
                {
                    var source = this.ConvertedImage.Source;

                    if (source != null)
                    {
                        string fileName = App.ViewModel.Context.Message.ImageLink.Split('/').Last();

                        using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (!isolatedStorage.DirectoryExists(ViewModel.SavedPicturesDirectory))
                            {
                                isolatedStorage.CreateDirectory(ViewModel.SavedPicturesDirectory);
                            }

                            string path = ViewModel.SavedPicturesDirectory + "/" + fileName;

                            if (isolatedStorage.FileExists(path))
                            {
                                MessageBox.Show("That picture has already been saved to the device");
                                return;
                            }

                            var data = source.GetBinaryData();

                            if (data != null)
                            {
                                using (var file = isolatedStorage.CreateFile(path))
                                {
                                    file.Write(data, 0, data.Length);
                                }

                                using (var file = isolatedStorage.OpenFile(path, FileMode.Open))
                                {
                                    using (var library = new MediaLibrary())
                                    {
                                        library.SavePicture(fileName, file);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    var source = (BitmapImage)this.ClassicalImage.Source;

                    var element = source;

                    var wb = new WriteableBitmap(element);

                    string fileName = App.ViewModel.Context.Message.ImageLink.Split('/').Last();

                    using (var stream = new MemoryStream())
                    {
                        wb.SaveJpeg(stream, wb.PixelWidth, wb.PixelHeight, 0, 100);

                        using (var library = new MediaLibrary())
                        {
                            stream.Position = 0;

                            library.SavePicture(fileName, stream);
                        }
                    }

                    try
                    {
                        using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            string escapedFileName = fileName.Replace('.', '_') + ".jpg";

                            if (isolatedStorage.FileExists(escapedFileName))
                            {
                                isolatedStorage.DeleteFile(escapedFileName);
                            }
                        }
                    }
                    catch (Exception)
                    {
#if DEBUG
                        throw;
#endif
                    }
                }
            }
            catch (Exception)
            {
                try
                {
                    MessageBox.Show("An error occured when trying to save the picture. Please ensure that your phone has enough storage space.");
                }
                catch (Exception)
                {
                }

                return;
            }

            try
            {
                var toast = new ToastPrompt { Title = "Image successfully saved" };

                toast.Show();
            }
            catch (Exception)
            {
            }
        }

        private void MenuPlayClick(object sender, EventArgs e)
        {
            if (this.IsDiaporamaRunning)
            {
                this.StopDiaporama();
            }
            else
            {
                this.StartDiaporama();
            }
        }

        private void CleanConvertedImage(bool stop)
        {
            if (stop)
            {
                this.ConvertedImage.Stop();
            }

            var source = this.ConvertedImage.Source;

            if (source != null)
            {
                source.Dispose();
            }

            if (stop)
            {
                this.ConvertedImage.Source = null;
            }
        }

        private void CleanClassicalImage()
        {
            var previousImage = this.ClassicalImage.Source as BitmapImage;

            this.ClassicalImage.Source = null;
            
            this.DisposeImage(previousImage);
        }

        private void CleanWebM()
        {
            if (this.AnimatedWrapper != null)
            {
                this.AnimatedWrapper.Unload();
            }
        }

        private void StartDiaporama()
        {
            this.MenuPlay.RestStateImageSource = new BitmapImage(new Uri("/Icons/appbar.transport.pause.rest.png", UriKind.Relative));
            this.IsDiaporamaRunning = true;

            this.ScheduleNextImage();

            if (this.DiaporamaTimer != null)
            {
                this.DiaporamaTimer.Stop();
                this.DiaporamaTimer = null;
            }

            if (this.DiaporamaTimer == null)
            {
                this.DiaporamaTimer = new DiaporamaTimer(this.LoadImageForDiaporama, () => this.NextImageTime);
            }
        }

        private void LoadImageForDiaporama()
        {
            this.NextImageTime = null;

            var nextMessage = this.NextMessage;

            if (nextMessage == null)
            {
                if (App.ViewModel.IsSlideShowLoopActivated)
                {
                    nextMessage = App.ViewModel.Messages.FirstOrDefault();
                }
                else
                {
                    this.Dispatcher.BeginInvoke(this.StopDiaporama);
                    return;
                }
            }

            this.Dispatcher.BeginInvoke(() =>
            {
                App.ViewModel.Context.Message = nextMessage;
                this.LoadImage();
            });
        }

        private void StopDiaporama()
        {
            this.MenuPlay.RestStateImageSource = new BitmapImage(new Uri("/Icons/appbar.transport.play.rest.png", UriKind.Relative));

            if (this.DiaporamaTimer != null)
            {
                this.DiaporamaTimer.Stop();
                this.DiaporamaTimer = null;
            }

            this.IsDiaporamaRunning = false;
        }

        private void ConvertedImageLoadingCompleted(ExtendedImage sender, EventArgs e)
        {
            if (this.NavigationService.Source != this.PageUri)
            {
                try
                {
                    this.CleanConvertedImage(true);
                }
                catch (Exception)
                {
                }

                try
                {
                    this.CleanWebM();
                }
                catch (Exception)
                {
                }

                return;
            }

            var image = (ExtendedImage)sender;

            if (image.IsStreaming)
            {
                if (this.ConvertedImage.Source != sender)
                {
                    ((ExtendedImage)sender).Dispose();
                    return;
                }
            }
            else
            {
                this.CleanConvertedImage(false);
                this.CleanWebM();

                this.ConvertedImage.Source = image;
            }

            this.ClassicalImage.Zoom = 1;
            this.ContentTranslate.X = 0;

            this.ScheduleNextImage();

            SystemTray.SetProgressIndicator(this, null);
            this.EnableApplicationBar(true);
        }

        private void ConvertedImageLoadingFailed(object sender, UnhandledExceptionEventArgs e)
        {
            this.ScheduleNextImage();

            this.OnError((Exception)e.ExceptionObject);
        }

        private void ButtonRetryClick(object sender, RoutedEventArgs e)
        {
            this.LoadImage();
        }

        private void OnError(Exception e)
        {
            SystemTray.SetProgressIndicator(this, null);

            this.TextErrorDetail.Text = e.Message;

            this.ErrorPanel.Visibility = Visibility.Visible;

            try
            {
                this.ConvertedImage.Stop();
            }
            catch
            {
            }

            var source = this.ConvertedImage.Source;

            if (source != null)
            {
                source.Dispose();
            }

            this.ConvertedImage.Source = null;
            this.ClassicalImage.Source = null;

            this.HideControls();
            
            this.EnableApplicationBar(false);
        }

        private void MenuConfigurationClick(object sender, EventArgs e)
        {
            this.NavigationService.Navigate("/Configuration.xaml?view=slideshow");
        }

        private void MenuOpenWebBrowserClick(object sender, EventArgs e)
        {
            Helper.OpenWebBrowser(new Uri(App.ViewModel.Context.Message.ImageLink));
        }

        private void ContentPanelManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            var trigger = this.ActualWidth / 3;

            if (Math.Abs(e.CumulativeManipulation.Translation.X) < trigger)
            {
                this.ContentTranslate.X = e.CumulativeManipulation.Translation.X;
            }
        }

        private void ContentPanelManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (e.TotalManipulation.Scale.X != 0 || e.TotalManipulation.Scale.Y != 0)
            {
                return;
            }

            if (this.ClassicalImage.Zoom != 1)
            {
                return;
            }

            var trigger = this.ActualWidth / 3;

            if (e.TotalManipulation.Translation.X < (trigger * -1))
            {
                if (this.NextMessage != null && this.MenuPlay.IsEnabled)
                {
                    App.ViewModel.Context.Message = this.NextMessage;
                    this.MessagesListBox.BringIntoView(this.NextMessage);
                    this.LoadImage();
                }
            }
            else if (e.TotalManipulation.Translation.X > trigger)
            {
                if (this.PreviousMessage != null && this.MenuPlay.IsEnabled)
                {
                    App.ViewModel.Context.Message = this.PreviousMessage;
                    this.MessagesListBox.BringIntoView(this.PreviousMessage);
                    this.LoadImage();
                }
            }

            this.PictureChangeAnimation.Begin();
        }

        private void PreviewManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            this.AppBarTranslateTransform.Y += e.DeltaManipulation.Translation.Y;

            var translation = Math.Abs(this.AppBarTranslateTransform.Y);

            if (translation < this.ApplicationBarSteps.First().Item1)
            {
                this.AppBarTranslateTransform.Y = this.ApplicationBarSteps.First().Item1 * -1;
            }
            else if (translation > this.ApplicationBarSteps.Last().Item1)
            {
                this.AppBarTranslateTransform.Y = this.ApplicationBarSteps.Last().Item1 * -1;
            }
            else if (this.AppBarTranslateTransform.Y > 0)
            {
                this.AppBarTranslateTransform.Y = 0;
            }
            
            e.Handled = true;
        }

        private void PreviewManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            const int Offset = 20;

            Tuple<int, double> nextStep;

            var translation = Math.Abs(this.AppBarTranslateTransform.Y);

            if (translation > this.CurrentApplicationBarStep.Item1)
            {
                nextStep = this.ApplicationBarSteps.FirstOrDefault(s => s.Item1 + Offset > translation);
            }
            else
            {
                nextStep = this.ApplicationBarSteps
                    .OrderByDescending(s => s.Item1)
                    .FirstOrDefault(s => s.Item1 - Offset < translation);
            }

            if (nextStep == null)
            {
                nextStep = this.CurrentApplicationBarStep;
            }

            this.RestorePreviewHeightAnimation.To = nextStep.Item1 * -1;
            this.RestorePreviewOpacityAnimation.To = nextStep.Item2 * -1;

            this.CurrentApplicationBarStep = nextStep;

            this.RestorePreviewStoryboard.Begin();

            e.Handled = true;
        }

        private void PreviewItemTap(object sender, ListBoxItemTapEventArgs e)
        {
            var message = e.Item.DataContext as Message;

            if (message != null)
            {
                App.ViewModel.Context.Message = message;

                if (!this.MessagesListBox.IsItemInViewport(message))
                {
                    this.MessagesListBox.BringIntoView(message);
                }

                this.LoadImage();
            }
        }

        private void PreviewInnerGridTap(object sender, RoutedEventArgs e)
        {
            var step = this.ApplicationBarSteps.First();

            var translation = Math.Abs(this.AppBarTranslateTransform.Y);

            for (int i = 0; i < this.ApplicationBarSteps.Count - 1; i++)
            {
                if (translation <= this.ApplicationBarSteps[i].Item1)
                {
                    step = this.ApplicationBarSteps[i + 1];
                    break;
                }
            }

            this.RestorePreviewHeightAnimation.To = step.Item1 * -1;
            this.RestorePreviewOpacityAnimation.To = step.Item2 * -1;

            this.CurrentApplicationBarStep = step;

            this.RestorePreviewStoryboard.Begin();
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            var eventHandler = this.PropertyChanged;

            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void ContentPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.SelectedMessageDetail.Visibility = Visibility.Visible;
            this.ShowMessageOverlay.Begin();
        }

        private void SelectedMessageDetail_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.HideMessageOverlay.Begin();
            e.Handled = true;
        }

        private void RadContextMenu_Opening(object sender, ContextMenuOpeningEventArgs e)
        {
            e.Cancel = true;
            this.SelectedMessageDetail.Visibility = Visibility.Visible;
            this.ShowMessageOverlay.Begin();
        }

        private void HideControls()
        {
            this.ConvertedImage.Visibility = Visibility.Collapsed;
            this.ClassicalImage.Visibility = Visibility.Collapsed;

            this.DrawingSurface.Visibility = Visibility.Collapsed;
        }

        private void HideErrors()
        {
            this.ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void MessageListScrolled(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var messages = this.ViewModel.Messages;

            if (messages == null)
            {
                return;
            }

            var scrollBar = sender as ScrollBar;

            if (scrollBar != null && scrollBar.Maximum == 0)
            {
                return;
            }

            var index = (int)e.NewValue;

            if (messages.Count > index)
            {
                var message = messages[index];

                if (!string.IsNullOrEmpty(message.ImageLink) && message != this.ViewModel.Context.Message)
                {
                    this.ViewModel.Context.Message = messages[index];
                    this.LoadImage();
                }
            }
        }

        private void PageOrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            this.PreviewInnerGrid.Width = this.ActualWidth;
        }

        private void LoadWebM(string url)
        {
            var webclient = new WebClient();

            var uri = new Uri(url);
            webclient.DownloadProgressChanged += (s, e) => this.ImageDownloadProgress(uri, e.ProgressPercentage);
            webclient.OpenReadCompleted += async (sender, e) =>
            {
                this.HideControls();
                this.HideErrors();

                this.DrawingSurface.Visibility = Visibility.Visible;

                var exception = e.Error;

                try
                {
                    if (exception == null)
                    {
                        var data = new byte[e.Result.Length];

                        e.Result.Read(data, 0, (int)e.Result.Length);

                        using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (!isolatedStorage.DirectoryExists(ViewModel.SavedPicturesDirectory))
                            {
                                isolatedStorage.CreateDirectory(ViewModel.SavedPicturesDirectory);
                            }

                            string path = ViewModel.SavedPicturesDirectory + "/temp.webm";

                            if (isolatedStorage.FileExists(path))
                            {
                                isolatedStorage.DeleteFile(path);
                            }

                            using (var file = isolatedStorage.CreateFile(path))
                            {
                                file.Write(data, 0, data.Length);
                            }
                        }

                        WebMImage webm;

                        try
                        {
                            webm = await Task.Run(() => new WebMImage(data));
                        }
                        catch
                        {
                            return;
                        }

                        this.CleanWebM();

                        this.AnimatedWrapper.SetSource(webm);
                        this.AnimatedWrapper.ShouldAnimate = true;

                        var source1 = this.ConvertedImage.Source;

                        source1?.Dispose();

                        this.CleanConvertedImage(true);
                        this.CleanClassicalImage();

                        SystemTray.SetProgressIndicator(this, null);
                        this.EnableApplicationBar(true);

                        this.ScheduleNextImage();
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception != null)
                {
                    this.ClassicalImageImageFailed(null, exception);
                }
            };

            this.CurrentUri = uri;

            webclient.OpenReadAsync(uri);
        }

        private void DrawingSurfaceSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.InitializeRenderer(e.NewSize);
        }

        private void InitializeRenderer(Size size)
        {
            bool contentSet = true;

            if (this.AnimatedWrapper == null)
            {
                this.AnimatedWrapper = new AnimatedWrapper();
                contentSet = false;
            }

            this.UpdateRendererSize(size);

            if (!contentSet)
            {
                this.DrawingSurface.SetContentProvider(this.AnimatedWrapper.CreateContentProvider());
            }
        }

        private void UpdateRendererSize(Size size)
        {
            // Set window bounds in dips
            this.AnimatedWrapper.WindowBounds = new Windows.Foundation.Size(
                (float)size.Width,
                (float)size.Height);

            // Set native resolution in pixels
            this.AnimatedWrapper.NativeResolution = new Windows.Foundation.Size(
                (float)Math.Floor(size.Width * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f),
                (float)Math.Floor(size.Height * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f));

            // Set render resolution to the full native resolution
            this.AnimatedWrapper.RenderResolution = this.AnimatedWrapper.NativeResolution;
        }
    }
}