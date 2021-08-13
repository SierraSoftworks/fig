namespace Fig.Common
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public static class FilesystemHelpers
    {
        /// <summary>
        /// Gets a <see cref="Stream"/> for the provided file which allows the contents of the file to be read.
        /// </summary>
        /// <param name="fileInfo">The name of the file to retrieve a read stream for.</param>
        /// <param name="pollingInterval">The interval between subsequent attempts to open the file.</param>
        /// <param name="cancellationToken">The cancellation token which can be used to abort the opening of this file.</param>
        /// <returns>A task which will eventually resolve to a read stream for the requested file.</returns>
        /// <remarks>
        /// This method will wait until the file is available for reading and may not return immediately.
        /// </remarks>
        /// <exception cref="TaskCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled before the file can be opened for reading.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file cannot be found on the local filesystem.</exception>
        public static async Task<Stream> GetFileReadStreamAsync(FileInfo fileInfo, TimeSpan pollingInterval, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (IOException ex)
                {
                    // Windows uses the HResult code 32 to indicate that this is a sharing violation (i.e. the file is in use).
                    // see: https://docs.microsoft.com/en-us/dotnet/standard/io/handling-io-errors#handling-ioexception
                    if ((ex.HResult & 0x0000FFFF) == 32)
                    {
                        await Task.Delay(pollingInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="Stream"/> for the provided file which allows the contents of the file to be written.
        /// </summary>
        /// <param name="fileInfo">The name of the file to retrieve a read stream for.</param>
        /// <param name="pollingInterval">The interval between subsequent attempts to open the file.</param>
        /// <param name="cancellationToken">The cancellation token which can be used to abort the opening of this file.</param>
        /// <returns>A task which will eventually resolve to a read stream for the requested file.</returns>
        /// <remarks>
        /// This method will wait until the file is available for reading and may not return immediately.
        /// </remarks>
        /// <exception cref="TaskCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled before the file can be opened for writing.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file cannot be found on the local filesystem.</exception>
        public static async Task<Stream> GetFileWriteStreamAsync(FileInfo fileInfo, TimeSpan pollingInterval, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return new FileStream(fileInfo.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException ex)
                {
                    // Windows uses the HResult code 32 to indicate that this is a sharing violation (i.e. the file is in use).
                    // see: https://docs.microsoft.com/en-us/dotnet/standard/io/handling-io-errors#handling-ioexception
                    if ((ex.HResult & 0x0000FFFF) == 32)
                    {
                        await Task.Delay(pollingInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="Stream"/> for the provided file which allows the contents of the file to be written in append mode.
        /// </summary>
        /// <param name="fileInfo">The name of the file to retrieve a read stream for.</param>
        /// <param name="pollingInterval">The interval between subsequent attempts to open the file.</param>
        /// <param name="cancellationToken">The cancellation token which can be used to abort the opening of this file.</param>
        /// <returns>A task which will eventually resolve to a read stream for the requested file.</returns>
        /// <remarks>
        /// This method will wait until the file is available for reading and may not return immediately.
        /// </remarks>
        /// <exception cref="TaskCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled before the file can be opened for writing.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file cannot be found on the local filesystem.</exception>
        public static async Task<Stream> GetFileAppendStreamAsync(FileInfo fileInfo, TimeSpan pollingInterval, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return new FileStream(fileInfo.FullName, FileMode.Append, FileAccess.Write, FileShare.Read);
                }
                catch (IOException ex)
                {
                    // Windows uses the HResult code 32 to indicate that this is a sharing violation (i.e. the file is in use).
                    // see: https://docs.microsoft.com/en-us/dotnet/standard/io/handling-io-errors#handling-ioexception
                    if ((ex.HResult & 0x0000FFFF) == 32)
                    {
                        await Task.Delay(pollingInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
