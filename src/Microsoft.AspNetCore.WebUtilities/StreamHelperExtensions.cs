// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    public static class StreamHelperExtensions
    {
        public static Task DrainAsync(this Stream stream, CancellationToken cancellationToken)
        {
            return stream.DrainAsync(cancellationToken, ArrayPool<byte>.Shared);
        }
        public static async Task DrainAsync(this Stream stream, CancellationToken cancellationToken, ArrayPool<byte> bytePool)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var buffer = bytePool.Rent(1024);
            try
            {
                while (await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken) > 0)
                {
                    // Not all streams support cancellation directly.
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                bytePool.Return(buffer);
            }
        }
    }
}