using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCommon.Util
{
    /// <summary>
    /// Provides utility methods for working with image files, including format detection from raw byte data.
    /// </summary>
    public class ImageHelper
    {

        /// <summary>
        /// Represents supported image file formats.
        /// </summary>
        public enum ImageFormat
        {
            /// <summary>Bitmap image format.</summary>
            Bmp,
            /// <summary>JPEG image format.</summary>
            Jpeg,
            /// <summary>GIF image format.</summary>
            Gif,
            /// <summary>TIFF image format.</summary>
            Tiff,
            /// <summary>PNG image format.</summary>
            Png,
            /// <summary>Unrecognized image format.</summary>
            Unknown
        }

        /// <summary>
        /// Detects the image format by inspecting the file header (magic bytes) of the byte array.
        /// </summary>
        /// <param name="bytes">The raw byte data of the image file.</param>
        /// <returns>The detected <see cref="ImageFormat"/>, or <see cref="ImageFormat.Unknown"/> if unrecognized.</returns>
        /// <remarks>
        /// Supports BMP, GIF, PNG, TIFF (both byte orders), and JPEG (standard and Canon variants)
        /// by comparing the leading bytes against known file signatures.
        /// </remarks>
        public static ImageFormat GetImageFormat(byte[] bytes)
        {
            // see http://www.mikekunz.com/image_file_header.html  
            var bmp = Encoding.ASCII.GetBytes("BM");     // BMP
            var gif = Encoding.ASCII.GetBytes("GIF");    // GIF
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            var tiff = new byte[] { 73, 73, 42 };         // TIFF
            var tiff2 = new byte[] { 77, 77, 42 };         // TIFF
            var jpeg = new byte[] { 255, 216, 255, 224 }; // jpeg
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon

            if (bmp.SequenceEqual(bytes.Take(bmp.Length)))
                return ImageFormat.Bmp;

            if (gif.SequenceEqual(bytes.Take(gif.Length)))
                return ImageFormat.Gif;

            if (png.SequenceEqual(bytes.Take(png.Length)))
                return ImageFormat.Png;

            if (tiff.SequenceEqual(bytes.Take(tiff.Length)))
                return ImageFormat.Tiff;

            if (tiff2.SequenceEqual(bytes.Take(tiff2.Length)))
                return ImageFormat.Tiff;

            if (jpeg.SequenceEqual(bytes.Take(jpeg.Length)))
                return ImageFormat.Jpeg;

            if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length)))
                return ImageFormat.Jpeg;

            return ImageFormat.Unknown;
        }
    }
}
