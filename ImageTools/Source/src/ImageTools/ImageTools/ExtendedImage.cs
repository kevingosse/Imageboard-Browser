// ===============================================================================
// Image.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using ImageTools.Helpers;
using ImageTools.IO;

namespace ImageTools
{
    /// <summary>
    /// Image class with stores the pixels and provides common functionality
    /// such as loading images from files and streams or operation like resizing or cutting.
    /// </summary>
    /// <remarks>The image data is alway stored in RGBA format, where the red, the blue, the
    /// alpha values are simple bytes.</remarks>
    [DebuggerDisplay("Image: {PixelWidth}x{PixelHeight}")]
    public sealed partial class ExtendedImage : ImageBase, IDisposable
    {
        public class ExtendedImageParameters
        {
            public bool IsStreaming { get; set; }
            public bool IsPreprocessed { get; set; }
        }

        #region Constants

        /// <summary>
        /// The default density value (dots per inch) in x direction. The default value is 75 dots per inch.
        /// </summary>
        public const double DefaultDensityX = 75;
        /// <summary>
        /// The default density value (dots per inch) in y direction. The default value is 75 dots per inch.
        /// </summary>
        public const double DefaultDensityY = 75;

        #endregion

        #region Invariant

#if !WINDOWS_PHONE
        private void ImageInvariantMethod()
        {
        }
#endif

        #endregion

        #region Fields

        private Uri _uriSource;

        private readonly object _lockObject = new object();

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the download is completed.
        /// </summary>
        public event OpenReadCompletedEventHandler DownloadCompleted;
        private void OnDownloadCompleted(OpenReadCompletedEventArgs e)
        {
            OpenReadCompletedEventHandler downloadCompletedHandler = DownloadCompleted;

            if (downloadCompletedHandler != null)
            {
                downloadCompletedHandler(this, e);
            }
        }

        /// <summary>
        /// Occurs when the download progress changed.
        /// </summary>
        public event DownloadProgressChangedEventHandler DownloadProgress;
        private void OnDownloadProgress(DownloadProgressChangedEventArgs e)
        {
            DownloadProgressChangedEventHandler downloadProgressHandler = DownloadProgress;

            if (downloadProgressHandler != null)
            {
                downloadProgressHandler(this, e);
            }
        }

        /// <summary>
        /// Occurs when the loading is completed.
        /// </summary>
        public event EventHandler LoadingCompleted;
        private void OnLoadingCompleted(EventArgs e)
        {
            EventHandler loadingCompletedHandler = LoadingCompleted;

            if (loadingCompletedHandler != null)
            {
                loadingCompletedHandler(this, e);
            }
        }

        /// <summary>
        /// Occurs when the loading of the image failed.
        /// </summary>
        public event EventHandler<UnhandledExceptionEventArgs> LoadingFailed;
        private void OnLoadingFailed(UnhandledExceptionEventArgs e)
        {
            EventHandler<UnhandledExceptionEventArgs> eventHandler = LoadingFailed;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        #endregion

        #region Properties

        public bool IsStreaming { get; set; }
        public bool IsPreprocessed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this image is loading at the moment.
        /// </summary>
        /// <value>
        /// true if this instance is image is loading at the moment; otherwise, false.
        /// </value>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// Gets or sets the resolution of the image in x direction. It is defined as 
        /// number of dots per inch and should be an positive value.
        /// </summary>
        /// <value>The density of the image in x direction.</value>
        public double DensityX { get; set; }

        /// <summary>
        /// Gets or sets the resolution of the image in y direction. It is defined as 
        /// number of dots per inch and should be an positive value.
        /// </summary>
        /// <value>The density of the image in y direction.</value>
        public double DensityY { get; set; }

        /// <summary>
        /// Gets the width of the image in inches. It is calculated as the width of the image 
        /// in pixels multiplied with the density. When the density is equals or less than zero 
        /// the default value is used.
        /// </summary>
        /// <value>The width of the image in inches.</value>
        public double InchWidth
        {
            get
            {
                double densityX = DensityX;

                if (densityX <= 0)
                {
                    densityX = DefaultDensityX;
                }

                return PixelWidth / densityX;
            }
        }

        /// <summary>
        /// Gets the height of the image in inches. It is calculated as the height of the image 
        /// in pixels multiplied with the density. When the density is equals or less than zero 
        /// the default value is used.
        /// </summary>
        /// <value>The height of the image in inches.</value>
        public double InchHeight
        {
            get
            {
                double densityY = DensityY;

                if (densityY <= 0)
                {
                    densityY = DefaultDensityY;
                }

                return PixelHeight / densityY;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this image is animated.
        /// </summary>
        /// <value>
        /// <c>true</c> if this image is animated; otherwise, <c>false</c>.
        /// </value>
        public bool IsAnimated
        {
            get { return _frames.Count > 0; }
        }

        private ImageFrameCollection _frames = new ImageFrameCollection();
        /// <summary>
        /// Get the other frames for the animation.
        /// </summary>
        /// <value>The list of frame images.</value>
        public ImageFrameCollection Frames
        {
            get
            {
                return _frames;
            }
        }

        private ImagePropertyCollection _properties = new ImagePropertyCollection();

        private Stream currentStream;

        /// <summary>
        /// Gets the list of properties for storing meta information about this image.
        /// </summary>
        /// <value>A list of image properties.</value>
        public ImagePropertyCollection Properties
        {
            get
            {
                return _properties;
            }
        }

        public string Referer { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Uri"/> source of the <see cref="ExtendedImage"/>.
        /// </summary>
        /// <value>The <see cref="Uri"/> source of the <see cref="ExtendedImage"/>. The
        /// default value is null (Nothing in Visual Basic).</value>
        /// <remarks>If the stream source and the uri source are both set, 
        /// the stream source will be ignored.</remarks>
        public Uri UriSource
        {
            get { return _uriSource; }
            set
            {
                lock (_lockObject)
                {
                    _uriSource = value;

                    if (UriSource != null)
                    {
                        LoadAsync(UriSource);
                    }
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedImage"/> class
        /// with the height and the width of the image.
        /// </summary>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        public ExtendedImage(int width, int height)
            : base(width, height)
        {
            DensityX = DefaultDensityX;
            DensityY = DefaultDensityY;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedImage"/> class
        /// by making a copy from another image.
        /// </summary>
        /// <param name="other">The other image, where the clone should be made from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is null
        /// (Nothing in Visual Basic).</exception>
        public ExtendedImage(ExtendedImage other)
            : base(other)
        {
            foreach (ImageFrame frame in other.Frames)
            {
                if (frame != null)
                {
                    if (!frame.IsFilled)
                    {
                        throw new ArgumentException("The image contains a frame that has not been loaded yet.");
                    }

                    Frames.Add(new ImageFrame(frame));
                }
            }

            DensityX = DefaultDensityX;
            DensityY = DefaultDensityY;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedImage"/> class.
        /// </summary>
        public ExtendedImage()
        {
            DensityX = DefaultDensityX;
            DensityY = DefaultDensityY;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Sets the source of the image to a specified stream.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> that contains the data for 
        /// this <see cref="ExtendedImage"/>. Cannot be null.</param>
        /// <remarks>
        /// The stream will not be closed or disposed when the loading
        /// is finished, so always use a using block or manually dispose
        /// the stream, when using the method. 
        /// The <see cref="ExtendedImage"/> class does not support alpha
        /// transparency in bitmaps. To enable alpha transparency, use
        /// PNG images with 32 bits per pixel.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/>
        /// is null (Nothing in Visual Basic).</exception>
        /// <exception cref="ImageFormatException">The image has an invalid
        /// format.</exception>
        /// <exception cref="NotSupportedException">The image cannot be loaded
        /// because loading images of this type are not supported yet.</exception>
        public void SetSource(Stream stream)
        {
            if (_uriSource == null)
            {
                LoadAsync(stream);
            }
        }

        private void Load(Stream stream)
        {
            //            var tempPictureFileName = this.TempPictureFileName;

            //            if (tempPictureFileName != null)
            //            {
            //                try
            //                {
            //                    using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            //                    {
            //                        if (isolatedStorage.FileExists(tempPictureFileName))
            //                        {
            //                            isolatedStorage.DeleteFile(tempPictureFileName);
            //                        }

            //                        using (var file = isolatedStorage.CreateFile(tempPictureFileName))
            //                        {
            //                            stream.CopyTo(file);
            //                        }
            //                    }
            //                }
            //                catch (Exception)
            //                {
            //#if DEBUG
            //                    throw;
            //#endif
            //                }

            //                try
            //                {
            //                    stream.Seek(0, SeekOrigin.Begin);
            //                }
            //                catch (Exception)
            //                {
            //#if DEBUG
            //                    throw;
            //#endif
            //                }
            //}

            try
            {
                if (!stream.CanRead)
                {
                    throw new NotSupportedException("Cannot read from the stream.");
                }

                var decoders = Decoders.GetAvailableDecoders();

                if (decoders.Count > 0)
                {
                    int maxHeaderSize = decoders.Max(x => x.HeaderSize);

                    if (maxHeaderSize > 0)
                    {
                        var decoder = decoders.FirstOrDefault(x => x.IsSupportedFileExtension("gif"));

                        if (decoder != null)
                        {
                            if (!IsPreprocessed)
                            {
                                var oldStream = Interlocked.Exchange(ref this.currentStream, stream);

                                if (oldStream != null)
                                {
                                    oldStream.Close();
                                    oldStream.Dispose();
                                }

                                this.StreamFrames = ((IStreamingImageDecoder)decoder).DecodeStream(this, stream);
                                this.IsFilled = true;
                            }
                            else
                            {
                                decoder.Decode(this, stream);
                            }

                            IsLoading = false;
                        }
                    }
                }

                if (IsLoading)
                {
                    IsLoading = false;

                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine("Image cannot be loaded. Available decoders:");

                    foreach (IImageDecoder decoder in decoders)
                    {
                        stringBuilder.AppendLine("-" + decoder);
                    }

                    throw new UnsupportedImageFormatException(stringBuilder.ToString());
                }
            }
            finally
            {
                if (this.IsPreprocessed)
                {
                    stream.Dispose();
                }
            }
        }

        protected IEnumerable<ImageBase> StreamFrames { get; set; }

        public IEnumerable<ImageBase> GetFrames()
        {
            return this.StreamFrames;
            //var decoders = Decoders.GetAvailableDecoders();

            //var gifDecoder = decoders.OfType<IStreamingImageDecoder>().First();

            //return gifDecoder.DecodeStream(this, this.currentStream);
        }

        public byte[] GetBinaryData()
        {
            var replicator = this.currentStream as DuplexStream;

            if (replicator != null)
            {
                return replicator.GetBinaryData();
            }

            return null;
        }

        private void LoadAsync(Stream stream)
        {
            IsLoading = true;

            ThreadPool.QueueUserWorkItem(objectState =>
                {
                    try
                    {
                        Load(stream);
                        OnLoadingCompleted(EventArgs.Empty);
                    }
                    catch (Exception e)
                    {
                        OnLoadingFailed(new UnhandledExceptionEventArgs(e, false));
                    }
                });
        }

        private void LoadAsync(Uri uri)
        {
            try
            {
                bool isHandled = false;

                if (!uri.IsAbsoluteUri)
                {
                    string fixedUri = uri.ToString();

                    fixedUri = fixedUri.Replace("\\", "/");

                    if (fixedUri.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                    {
                        fixedUri = fixedUri.Substring(1);
                    }

                    var resourceStream = Extensions.GetLocalResourceStream(new Uri(fixedUri, UriKind.Relative));

                    if (resourceStream != null)
                    {
                        LoadAsync(resourceStream);

                        isHandled = true;
                    }
                }

                if (!isHandled)
                {
                    IsLoading = true;

                    //var webClient = HttpWebRequest.CreateHttp(uri);

                    //webClient.AllowReadStreamBuffering = false;

                    //webClient.BeginGetResponse(Callback, webClient);

                    var webClient = new WebClient();
                    //var webClient = new SharpGIS.GZipWebClient();

                    webClient.AllowReadStreamBuffering = !this.IsStreaming;
                    webClient.DownloadProgressChanged += this.webClient_DownloadProgressChanged;

                    webClient.OpenReadCompleted += this.webClient_OpenReadCompleted;

                    var referer = this.Referer;

                    if (referer != null)
                    {
                        webClient.Headers[HttpRequestHeader.Referer] = referer;
                    }

                    webClient.OpenReadAsync(uri);
                }
            }
            catch (ArgumentException e)
            {
                OnLoadingFailed(new UnhandledExceptionEventArgs(e, false));
            }
            catch (InvalidOperationException e)
            {
                OnLoadingFailed(new UnhandledExceptionEventArgs(e, false));
            }
        }

        private void Callback(IAsyncResult result)
        {
            var request = (HttpWebRequest)result.AsyncState;

            var response = request.EndGetResponse(result);

            LoadAsync(response.GetResponseStream());
        }

        private void webClient_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            try
            {
                if (e.Error == null)
                {
                    Stream remoteStream = e.Result;

                    if (remoteStream != null)
                    {
                        var outputStream = new DuplexStream();

                        Downloader.Start(remoteStream, outputStream, () => this.OnDownloadCompleted(e));

                        LoadAsync(outputStream);

                        //if (this.IsStreaming)
                        //{
                        //    var outputStream = new DuplexStream();

                        //    Downloader.Start(remoteStream, outputStream, () => this.OnDownloadCompleted(e));

                        //    LoadAsync(outputStream);
                        //}
                        //else
                        //{
                        //    LoadAsync(remoteStream);
                        //}
                    }
                }
                else
                {
                    OnLoadingFailed(new UnhandledExceptionEventArgs(e.Error, false));
                }

                if (!this.IsStreaming)
                {
                    OnDownloadCompleted(e);
                }
            }
            catch (WebException ex)
            {
                OnLoadingFailed(new UnhandledExceptionEventArgs(ex, false));
            }
        }

        private void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnDownloadProgress(e);
        }

        #endregion Methods

        #region ICloneable Members

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public ExtendedImage Clone()
        {
            return new ExtendedImage(this);
        }

        #endregion

        public void Dispose()
        {
            var stream = this.currentStream;

            if (stream != null)
            {
                stream.Close();
                stream.Dispose();
            }

            this.LoadingCompleted = null;
            this.DownloadCompleted = null;
            this.DownloadProgress = null;
            this.LoadingFailed = null;
        }
    }
}
