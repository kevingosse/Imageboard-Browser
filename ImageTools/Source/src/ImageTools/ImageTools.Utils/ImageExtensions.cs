// ===============================================================================
// ImageExtensions.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageTools.IO;
using ImageTools.IO.Png;

namespace ImageTools
{
    /// <summary>
    /// Contains some extension methods for improving the usablilty of 
    /// the image class.
    /// </summary>
    public static class ImageExtensions
    {
        /// <summary>
        /// Initializes the <see cref="ImageExtensions"/> class.
        /// </summary>
        static ImageExtensions()
        {
            Encoders.AddEncoder<PngEncoder>();
        }

        /// <summary>
        /// Converts the image to a png stream, which can be assigned to 
        /// a silverlight image control as image source.
        /// </summary>
        /// <param name="image">The image, which should be converted. Cannot be null
        /// (Nothing in Visual Basic).</param>
        /// <returns>The resulting stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null
        /// (Nothing in Visual Basic).</exception>
        public static Stream ToStream(this ExtendedImage image)
        {
            return ToStream(image, null);
        }

        /// <summary>
        /// Converts the image to a png stream, which can be assigned to
        /// a silverlight image control as image source and applies the specified
        /// filter before converting the image.
        /// </summary>
        /// <param name="image">The image, which should be converted. Cannot be null
        /// (Nothing in Visual Basic).</param>
        /// <param name="filter">The filter, which should be applied before converting the 
        /// image or null, if no filter should be applied to. Cannot be null.</param>
        /// <returns>The resulting stream.</returns>
        /// <exception cref="ArgumentNullException">
        /// 	<para><paramref name="image"/> is null (Nothing in Visual Basic).</para>
        /// 	<para>- or -</para>
        /// 	<para><paramref name="filter"/> is null (Nothing in Visual Basic).</para>
        /// </exception>
        public static Stream ToStream(this ExtendedImage image, IImageFilter filter)
        {
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                ExtendedImage temp = image;

                if (filter != null)
                {
                    temp = image.Clone();

                    filter.Apply(temp, image, temp.Bounds);
                }

                PngEncoder encoder = new PngEncoder();
                encoder.IsWritingUncompressed = true;
                encoder.Encode(temp, memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);
            }
            catch
            {
                if (memoryStream != null)
                {
                    memoryStream.Dispose();
                    memoryStream = null;
                }
                throw;
            }

            return memoryStream;
        }

        /// <summary>
        /// Converts the image to a stream, which can be assigned to
        /// a silverlight image control as image source and applies the specified
        /// filter before converting the image. The encoder to use will be created by
        /// the file extension name.
        /// </summary>
        /// <param name="image">The image, which should be converted. Cannot be null
        /// (Nothing in Visual Basic).</param>
        /// <param name="extension">The file extension to create the <see cref="IImageEncoder"/> that
        /// will be used to create an image stream.</param>
        /// <returns>The resulting stream.</returns>
        /// <exception cref="ArgumentException"><paramref name="extension"/> is empty or 
        /// contains only blanks.</exception>
        /// <exception cref="ArgumentNullException">
        /// 	<para><paramref name="image"/> is null (Nothing in Visual Basic).</para>
        /// 	<para>- or -</para>
        /// 	<para><paramref name="extension"/> is null (Nothing in Visual Basic).</para>
        /// </exception>
        public static Stream ToStreamByExtension(this ExtendedImage image, string extension)
        {
            IImageEncoder encoder = null;

            foreach (IImageEncoder availableEncoder in Encoders.GetAvailableEncoders())
            {
                if (availableEncoder != null && availableEncoder.IsSupportedFileExtension(extension))
                {
                    encoder = availableEncoder;
                    break;
                }
            }

            if (encoder == null)
            {
                encoder = new PngEncoder();
            }

            MemoryStream memoryStream = null;
            try
            {
                memoryStream = new MemoryStream();
                encoder.Encode(image, memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);
            }
            catch
            {
                if (memoryStream != null)
                {
                    memoryStream.Dispose();
                }
                throw;
            }
            return memoryStream;
        }

        /// <summary>
        /// Converts the image to a silverlight bitmap, which can be assigned
        /// to a image control.
        /// </summary>
        /// <param name="image">The image, which should be converted. Cannot be null
        /// (Nothing in Visual Basic).</param>
        /// <returns>The resulting bitmap.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null 
        /// (Nothing in Visual Basic).</exception>
        public static WriteableBitmap ToBitmap(this ImageBase image)
        {
            return ToBitmap(image, null);
        }

        /// <summary>
        /// Converts the image to a silverlight bitmap, which can be assigned
        /// to a image control.
        /// </summary>
        /// <param name="image">The image, which should be converted. Cannot be null
        /// (Nothing in Visual Basic).</param>
        /// <param name="filter">The filter, which should be applied before converting the
        /// image or null, if no filter should be applied to. Cannot be null.</param>
        /// <returns>The resulting bitmap.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is null 
        /// (Nothing in Visual Basic).</exception>
        public static WriteableBitmap ToBitmap(this ImageBase image, IImageFilter filter)
        {
            WriteableBitmap bitmap = new WriteableBitmap(image.PixelWidth, image.PixelHeight);

            ImageBase temp = image;

            if (filter != null)
            {
                temp = new ImageBase(image);

                filter.Apply(temp, image, temp.Bounds);
            }

            byte[] pixels = temp.Pixels;

            if (pixels != null)
            {
                int[] raster = bitmap.Pixels;

                if (raster != null)
                {
                    Buffer.BlockCopy(pixels, 0, raster, 0, pixels.Length);

                    for (int i = 0; i < raster.Length; i++)
                    {
                        int abgr = raster[i];
                        int a = (abgr >> 24) & 0xff;

                        float m = a / 255f;

                        int argb = a << 24 |
                            (int)(((abgr >>  0) & 0xff) * m) << 16 |
                            (int)(((abgr >>  8) & 0xff) * m) << 8 |
                            (int)(((abgr >> 16) & 0xff) * m);
                        raster[i] = argb;
                    }
                }
            }

            bitmap.Invalidate();

            return bitmap;            
        }

        /// <summary>
        /// Converts the content of a <see cref="Canvas"/> to an image.
        /// </summary>
        /// <param name="element">The <see cref="UIElement"/> to convert. Cannot be null.</param>
        /// <returns>The resulting image.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
        public static ExtendedImage ToImage(this UIElement element)
        {
            WriteableBitmap bitmap = new WriteableBitmap(element, new TranslateTransform());
            return ToImage(bitmap);
        }

        /// <summary>
        /// Converts a <see cref="WriteableBitmap"/> to an image.
        /// </summary>
        /// <param name="bitmap">The bitmap to convert. Cannot be null.</param>
        /// <returns>The resulting image.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bitmap"/> is null.</exception>
        public static ExtendedImage ToImage(this WriteableBitmap bitmap)
        {
            bitmap.Invalidate();

            ExtendedImage image = new ExtendedImage(bitmap.PixelWidth, bitmap.PixelHeight);
            try
            {
                byte[] pixels = image.Pixels;

                int[] raster = bitmap.Pixels;

                if (raster != null)
                {
                    for (int y = 0; y < image.PixelHeight; ++y)
                    {
                        for (int x = 0; x < image.PixelWidth; ++x)
                        {
                            int pixelIndex = bitmap.PixelWidth * y + x;
                            int pixel = raster[pixelIndex];

                            byte a = (byte)((pixel >> 24) & 0xFF);

                            float aFactor = a / 255f;

                            if (aFactor > 0)
                            {
                                byte r = (byte)(((pixel >> 16) & 0xFF) / aFactor);
                                byte g = (byte)(((pixel >>  8) & 0xFF) / aFactor);
                                byte b = (byte)(((pixel >>  0) & 0xFF) / aFactor);

                                pixels[pixelIndex * 4 + 0] = r;
                                pixels[pixelIndex * 4 + 1] = g;
                                pixels[pixelIndex * 4 + 2] = b;
                                pixels[pixelIndex * 4 + 3] = a;
                            }
                        }
                    }
                }
            }
            catch (SecurityException e)
            {
                throw new ArgumentException("Bitmap cannot accessed", e);
            }

            return image;
        }

#if !WINDOWS_PHONE
        /// <summary>
        /// Writes to specified image to the stream.
        /// </summary>
        /// <param name="image">The image to write to the stream. Cannot be null.</param>
        /// <param name="stream">The target stream. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">
        /// 	<para><paramref name="image"/> is null (Nothing in Visual Basic).</para>
        /// 	<para>- or -</para>
        /// 	<para><paramref name="stream"/> is null (Nothing in Visual Basic).</para>
        /// </exception>
        public static void WriteToStream(this ExtendedImage image, Stream stream)
        {
            PngEncoder encoder = new PngEncoder();

            encoder.Encode(image, stream);
        }
#endif

        /// <summary>
        /// Writes to specified image to the stream. The method loops through all encoders and 
        /// uses the encoder which supports the extension of the specified file name.
        /// </summary>
        /// <param name="image">The image to write to the stream. Cannot be null.</param>
        /// <param name="stream">The target stream. Cannot be null.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <exception cref="ArgumentNullException">
        /// 	<para><paramref name="image"/> is null (Nothing in Visual Basic).</para>
        /// 	<para>- or -</para>
        /// 	<para><paramref name="stream"/> is null (Nothing in Visual Basic).</para>
        /// </exception>
        public static void WriteToStream(this ExtendedImage image, Stream stream, string fileName)
        {
            string path = Path.GetExtension(fileName);

            if (path == null || string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("The file name is not valid and contains no extension.");
            }

            string pathExtension = path.Substring(1);

            IImageEncoder encoder = null;
            foreach (IImageEncoder availableEncoder in Encoders.GetAvailableEncoders())
            {
                if (availableEncoder != null && availableEncoder.IsSupportedFileExtension(pathExtension))
                {
                    encoder = availableEncoder;
                    break;
                }
            }

            if (encoder == null)
            {
                throw new UnsupportedImageFormatException("Specified file extension is not supported.");
            }

            encoder.Encode(image, stream);
        }
    }
}
