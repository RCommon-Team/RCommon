using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Provides extension methods for <see cref="Stream"/> including reading all bytes and async copy operations.
    /// </summary>
    public static class StreamExtensions
    {

        /// <summary>
        /// Reads all bytes from the stream into a byte array.
        /// Resets the stream position to the beginning if the stream supports seeking.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>A byte array containing the entire contents of the stream.</returns>
        public static byte[] GetAllBytes(this Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Asynchronously reads all bytes from the stream into a byte array.
        /// Resets the stream position to the beginning if the stream supports seeking.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that resolves to a byte array containing the entire contents of the stream.</returns>
        public static async Task<byte[]> GetAllBytesAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            using (var memoryStream = new MemoryStream())
            {
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }
                await stream.CopyToAsync(memoryStream, cancellationToken);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Asynchronously copies the stream to a destination stream with cancellation support.
        /// Resets the stream position to the beginning if the stream supports seeking.
        /// </summary>
        /// <param name="stream">The source stream to copy from.</param>
        /// <param name="destination">The destination stream to copy to.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous copy operation.</returns>
        public static Task CopyToAsync(this Stream stream, Stream destination, CancellationToken cancellationToken)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            return stream.CopyToAsync(
                destination,
                81920, //this is already the default value, but needed to set to be able to pass the cancellationToken
                cancellationToken
            );
        }
    }
}
