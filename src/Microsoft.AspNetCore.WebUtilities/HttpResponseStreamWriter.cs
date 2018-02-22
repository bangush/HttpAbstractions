// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Threading;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.WebUtilities
{
    /// <summary>
    /// Writes to the <see cref="Stream"/> using the supplied <see cref="Encoding"/>.
    /// It does not write the BOM and also does not close the stream.
    /// </summary>
    public partial class HttpResponseStreamWriter : TextWriter
    {
        private const int MinBufferSize = 128;
        internal const int DefaultBufferSize = 16 * 1024;

        private readonly PipeWriter _pipe;
        private readonly StreamWriter _streamWriter;
        private readonly Encoder _encoder;

        private bool _disposed;

        public HttpResponseStreamWriter(Stream stream, Encoding encoding)
            : this(stream, encoding, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
        {
        }

        public HttpResponseStreamWriter(Stream stream, Encoding encoding, int bufferSize)
            : this(stream, encoding, bufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
        {
        }

        public HttpResponseStreamWriter(
            Stream stream,
            Encoding encoding,
            int bufferSize,
            ArrayPool<byte> bytePool,
            ArrayPool<char> charPool)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

            if (bytePool == null)
            {
                throw new ArgumentNullException(nameof(bytePool));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }
            if (!stream.CanWrite)
            {
                throw new ArgumentException(Resources.HttpResponseStreamWriter_StreamNotWritable, nameof(stream));
            }

            Encoding = encoding;
            if (stream is IDuplexPipe pipe)
            {
                _encoder = encoding.GetEncoder();
                _pipe = pipe.Output;
            }
            else
            {
                _streamWriter = new StreamWriter(stream, encoding, bufferSize, bytePool, charPool);
            }
        }

        public override Encoding Encoding { get; }

        public override void Write(char value)
        {
            ThrowIfDisposed();

            if (_pipe != null)
            {
                WriteChar(value);
            }
            else
            {
                _streamWriter.Write(value);
            }
        }

        public override void Write(char[] values, int index, int count)
        {
            ThrowIfDisposed();

            if (values == null)
            {
                return;
            }

            if (_pipe != null)
            {
                WriteSpan(values.AsReadOnlySpan());
            }
            else
            {
                _streamWriter.Write(values, index, count);
            }
        }

        public override void Write(string value)
        {
            ThrowIfDisposed();

            if (value == null)
            {
                return;
            }

            if (_pipe != null)
            {
                WriteSpan(value.AsReadOnlySpan());
            }
            else
            {
                _streamWriter.Write(value);
            }
        }

#if NETCOREAPP2_1
        public override void Write(ReadOnlySpan<char> buffer)
        {
            ThrowIfDisposed();

            if (buffer.Length == 0)
            {
                return;
            }

            if (_pipe != null)
            {
                WriteSpan(buffer);
            }
            else
            {
                _streamWriter.Write(buffer);
            }
        }

        public override void WriteLine(ReadOnlySpan<char> buffer)
        {
            ThrowIfDisposed();

            if (buffer.Length == 0)
            {
                Write(buffer);
            }

            Write(CoreNewLine.AsReadOnlySpan());
        }
#endif

        public override Task WriteAsync(char value)
        {
            ThrowIfDisposed();

            if (_pipe != null)
            {
                WriteChar(value);
                return _pipe.WriteAsync(default);
            }
            else
            {
                return _streamWriter.WriteAsync(value);
            }
        }

        public override Task WriteAsync(char[] values, int index, int count)
        {
            ThrowIfDisposed();

            if (values == null)
            {
                return Task.CompletedTask;
            }

            if (_pipe != null)
            {
                WriteSpan(new ReadOnlySpan<char>(values, index, count));
                return _pipe.WriteAsync(default);
            }
            else
            {
                return _streamWriter.WriteAsync(values, index, count);
            }
        }

        public override Task WriteAsync(string value)
        {
            ThrowIfDisposed();

            if (value == null)
            {
                return Task.CompletedTask;
            }

            if (_pipe != null)
            {
                WriteSpan(value.AsReadOnlySpan());
                return _pipe.WriteAsync(default);
            }
            else
            {
                return _streamWriter.WriteAsync(value);
            }
        }

#if NETCOREAPP2_1
        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var span = buffer.Span;
            if (span.Length == 0)
            {
                return Task.CompletedTask;
            }

            if (_pipe != null)
            {
                WriteSpan(span);
                return _pipe.WriteAsync(default, cancellationToken);
            }
            else
            {
                return _streamWriter.WriteAsync(buffer, cancellationToken);
            }
        }

        public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var span = buffer.Span;
            if (span.Length == 0)
            {
                return Task.CompletedTask;
            }

            if (_pipe != null)
            {
                WriteSpan(span);
                Write(CoreNewLine.AsReadOnlySpan());
                return _pipe.WriteAsync(default, cancellationToken);
            }
            else
            {
                return _streamWriter.WriteAsync(buffer, cancellationToken);
            }
        }
#endif

        private unsafe void WriteChar(char value)
        {
            Span<byte> bytes = _pipe.GetSpan(Encoding.GetMaxByteCount(1));
            int encoded;
#if NETCOREAPP2_1
            encoded = _encoder.GetBytes(new ReadOnlySpan<char>(&value, sizeof(char)), bytes, flush: false);
#else
            fixed (byte* pBytes = &MemoryMarshal.GetReference(bytes))
            {
                encoded = _encoder.GetBytes(&value, sizeof(char), pBytes, bytes.Length, flush: false);
            }
#endif
            _pipe.Advance(encoded);
        }

        private unsafe void WriteSpan(ReadOnlySpan<char> input, bool flush = false)
        {
            int minBytes = Encoding.GetMaxCharCount(1);
            while (input.Length > 0 || (flush && _encoder.FallbackBuffer.Remaining > 0))
            {
                Span<byte> bytes = _pipe.GetSpan(minBytes);
                int totalEncoded = 0;
                while (bytes.Length > 0)
                {
                    int toEncode = Math.Min(Encoding.GetMaxCharCount(bytes.Length), input.Length);
                    int encoded;
#if NETCOREAPP2_1
                    encoded = _encoder.GetBytes(input.Slice(0, toEncode), bytes, flush: flush);
#else
                    fixed (char* pChars = &MemoryMarshal.GetReference(input))
                    fixed (byte* pBytes = &MemoryMarshal.GetReference(bytes))
                    {
                        encoded = _encoder.GetBytes(pChars, toEncode, pBytes, bytes.Length, flush: flush);
                    }
#endif
                    input = input.Slice(toEncode);
                    bytes = bytes.Slice(encoded);
                    totalEncoded += encoded;
                    if (bytes.Length < minBytes)
                    {
                        break;
                    }
                }

                _pipe.Advance(totalEncoded);
            }
        }

        // We want to flush the stream when Flush/FlushAsync is explicitly
        // called by the user (example: from a Razor view).

        public override void Flush()
        {
            ThrowIfDisposed();

            if (_pipe != null)
            {
                if (_encoder.FallbackBuffer.Remaining > 0)
                {
                    WriteSpan(default, flush: true);
                }

                _pipe.FlushAsync().GetAwaiter().GetResult();
            }
            else
            {
                _streamWriter.Flush();
            }
        }

        public async override Task FlushAsync()
        {
            ThrowIfDisposed();

            if (_pipe != null)
            {
                if (_encoder.FallbackBuffer.Remaining > 0)
                {
                    WriteSpan(default, flush: true);
                }

                await _pipe.FlushAsync();
            }
            else
            {
                await _streamWriter.FlushAsync();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {

                if (_pipe != null)
                {
                    if (_encoder.FallbackBuffer.Remaining > 0)
                    {
                        Flush();
                    }
                }
                else
                {
                    _streamWriter.ParentDispose(disposing);
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                ThrowDisposed();
            }
        }

        private void ThrowDisposed() => throw new ObjectDisposedException(nameof(HttpResponseStreamWriter));
    }
}
