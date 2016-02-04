// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    /// <summary>
    /// Used to read an 'application/x-www-form-urlencoded' form.
    /// </summary>
    public class FormReader : IDisposable
    {
        private readonly TextReader _reader;
        private readonly char[] _buffer;
        private readonly ArrayPool<byte> _bytePool;
        private readonly ArrayPool<char> _charPool;
        private readonly StringBuilder _builder = new StringBuilder();
        private int _bufferOffset;
        private int _bufferCount;
        private bool _disposed;

        public FormReader(string data)
            : this(data, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
        {
        }

        public FormReader(string data, ArrayPool<byte> bytePool, ArrayPool<char> charPool)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _buffer = charPool.Rent(1024);
            _charPool = charPool;
            _bytePool = bytePool;
            _reader = new StringReader(data);
        }

        public FormReader(Stream stream, Encoding encoding)
            : this(stream, encoding, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
        {
        }

        public FormReader(Stream stream, Encoding encoding, ArrayPool<byte> bytePool, ArrayPool<char> charPool)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            _buffer = charPool.Rent(1024);
            _charPool = charPool;
            _bytePool = bytePool;
            _reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024 * 2, leaveOpen: true);
        }

        // Format: key1=value1&key2=value2
        /// <summary>
        /// Reads the next key value pair from the form.
        /// For unbuffered data use the async overload instead.
        /// </summary>
        /// <returns>The next key value pair, or null when the end of the form is reached.</returns>
        public KeyValuePair<string, string>? ReadNextPair()
        {
            var key = ReadWord('=');
            if (string.IsNullOrEmpty(key) && _bufferCount == 0)
            {
                return null;
            }
            var value = ReadWord('&');
            return new KeyValuePair<string, string>(key, value);
        }

        // Format: key1=value1&key2=value2
        /// <summary>
        /// Asynchronously reads the next key value pair from the form.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The next key value pair, or null when the end of the form is reached.</returns>
        public async Task<KeyValuePair<string, string>?> ReadNextPairAsync(CancellationToken cancellationToken)
        {
            var key = await ReadWordAsync('=', cancellationToken);
            if (string.IsNullOrEmpty(key) && _bufferCount == 0)
            {
                return null;
            }
            var value = await ReadWordAsync('&', cancellationToken);
            return new KeyValuePair<string, string>(key, value);
        }

        private string ReadWord(char seperator)
        {
            // TODO: Configurable value size limit
            while (true)
            {
                // Empty
                if (_bufferCount == 0)
                {
                    Buffer();
                }

                // End
                if (_bufferCount == 0)
                {
                    return BuildWord();
                }

                var c = _buffer[_bufferOffset++];
                _bufferCount--;

                if (c == seperator)
                {
                    return BuildWord();
                }
                _builder.Append(c);
            }
        }

        private async Task<string> ReadWordAsync(char seperator, CancellationToken cancellationToken)
        {
            // TODO: Configurable value size limit
            while (true)
            {
                // Empty
                if (_bufferCount == 0)
                {
                    await BufferAsync(cancellationToken);
                }

                // End
                if (_bufferCount == 0)
                {
                    return BuildWord();
                }

                var c = _buffer[_bufferOffset++];
                _bufferCount--;

                if (c == seperator)
                {
                    return BuildWord();
                }
                _builder.Append(c);
            }
        }

        // '+' un-escapes to ' ', %HH un-escapes as ASCII (or utf-8?)
        private string BuildWord()
        {
            if (_builder.Length == 0)
            {
                return string.Empty;
            }
            var result = UrlDecode(_builder, _bytePool, _charPool);
            _builder.Clear();
            return result;
        }

        private void Buffer()
        {
            _bufferOffset = 0;
            _bufferCount = _reader.Read(_buffer, 0, _buffer.Length);
        }

        private async Task BufferAsync(CancellationToken cancellationToken)
        {
            // TODO: StreamReader doesn't support cancellation?
            cancellationToken.ThrowIfCancellationRequested();
            _bufferOffset = 0;
            _bufferCount = await _reader.ReadAsync(_buffer, 0, _buffer.Length);
        }

        /// <summary>
        /// Parses text from an HTTP form body.
        /// </summary>
        /// <param name="text">The HTTP form body to parse.</param>
        /// <returns>The collection containing the parsed HTTP form body.</returns>
        public static Dictionary<string, StringValues> ReadForm(string text)
        {
            using (var reader = new FormReader(text))
            {
                var accumulator = new KeyValueAccumulator();
                var pair = reader.ReadNextPair();
                while (pair.HasValue)
                {
                    accumulator.Append(pair.Value.Key, pair.Value.Value);
                    pair = reader.ReadNextPair();
                }

                return accumulator.GetResults();
            }
        }

        /// <summary>
        /// Parses an HTTP form body.
        /// </summary>
        /// <param name="stream">The HTTP form body to parse.</param>
        /// <returns>The collection containing the parsed HTTP form body.</returns>
        public static Task<Dictionary<string, StringValues>> ReadFormAsync(Stream stream, CancellationToken cancellationToken = new CancellationToken())
        {
            return ReadFormAsync(stream, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Parses an HTTP form body.
        /// </summary>
        /// <param name="stream">The HTTP form body to parse.</param>
        /// <returns>The collection containing the parsed HTTP form body.</returns>
        public static async Task<Dictionary<string, StringValues>> ReadFormAsync(Stream stream, Encoding encoding, CancellationToken cancellationToken = new CancellationToken())
        {
            using (var reader = new FormReader(stream, encoding))
            {
                var accumulator = new KeyValueAccumulator();
                var pair = await reader.ReadNextPairAsync(cancellationToken);
                while (pair.HasValue)
                {
                    accumulator.Append(pair.Value.Key, pair.Value.Value);
                    pair = await reader.ReadNextPairAsync(cancellationToken);
                }

                return accumulator.GetResults();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _charPool.Return(_buffer);
            }
        }

        // Algorithm from https://github.com/dotnet/corefx/blob/ac67ffac987d0c27236c4a6cf1255c2bcbc7fe7d/src/System.Runtime.Extensions/src/System/Net/WebUtility.cs#L378
        // but accepting StringBuilder
        private string UrlDecode(StringBuilder value, ArrayPool<byte> bytePool, ArrayPool<char> charPool)
        {
            if (value == null)
            {
                return null;
            }

            var count = value.Length;
            UrlDecoder helper = new UrlDecoder(count, Encoding.UTF8, bytePool, charPool);

            // go through the string's chars collapsing %XX and
            // appending each char as char, with exception of %XX constructs
            // that are appended as bytes

            for (var pos = 0; pos < count; pos++)
            {
                var ch = value[pos];

                if (ch == '+')
                {
                    ch = ' ';
                }
                else if (ch == '%' && pos < count - 2)
                {
                    var h1 = HexToInt(value[pos + 1]);
                    var h2 = HexToInt(value[pos + 2]);

                    if (h1 >= 0 && h2 >= 0)
                    {     // valid 2 hex chars
                        var b = (byte)((h1 << 4) | h2);
                        pos += 2;

                        // don't add as char
                        helper.AddByte(b);
                        continue;
                    }
                }

                if ((ch & 0xFF80) == 0)
                {
                    helper.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode
                }
                else
                {
                    helper.AddChar(ch);
                }
            }

            var decodedString = helper.GetString();
            helper.Dispose();

            return decodedString;
        }

        // from https://github.com/dotnet/corefx/blob/ac67ffac987d0c27236c4a6cf1255c2bcbc7fe7d/src/System.Runtime.Extensions/src/System/Net/WebUtility.cs#L529
        // as private static there
        private static int HexToInt(char h)
        {
            return (h >= '0' && h <= '9') ? h - '0' :
            (h >= 'a' && h <= 'f') ? h - 'a' + 10 :
            (h >= 'A' && h <= 'F') ? h - 'A' + 10 :
            -1;
        }

        // from https://github.com/dotnet/corefx/blob/ac67ffac987d0c27236c4a6cf1255c2bcbc7fe7d/src/System.Runtime.Extensions/src/System/Net/WebUtility.cs#L610
        // but lower allocation struct
        private struct UrlDecoder
        {
            private int _bufferSize;

            // Accumulate characters in a special array
            private int _numChars;
            private char[] _charBuffer;

            // Accumulate bytes for decoding into characters in a special array
            private int _numBytes;
            private byte[] _byteBuffer;

            // Encoding to convert chars to bytes
            private Encoding _encoding;

            private ArrayPool<byte> _bytePool;
            private ArrayPool<char> _charPool;

            public UrlDecoder(int bufferSize, Encoding encoding, ArrayPool<byte> bytePool, ArrayPool<char> charPool)
            {
                _bufferSize = bufferSize;
                _encoding = encoding;
                _numChars = 0;
                _numBytes = 0;

                _bytePool = bytePool;
                _charPool = charPool;

                _byteBuffer = null;
                _charBuffer = charPool.Rent(bufferSize);
                // byte buffer created on demand
            }

            private void FlushBytes()
            {
                if (_numBytes > 0)
                {
                    _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                    _numBytes = 0;
                }
            }

            public void AddChar(char ch)
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }

                _charBuffer[_numChars++] = ch;
            }

            public void AddByte(byte b)
            {
                if (_byteBuffer == null)
                {
                    _byteBuffer = _bytePool.Rent(_bufferSize);
                }

                _byteBuffer[_numBytes++] = b;
            }

            public string GetString()
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }

                if (_numChars > 0)
                {
                    return new string(_charBuffer, 0, _numChars);
                }
                else
                {
                    return string.Empty;
                }
            }

            public void Dispose()
            {
                _charPool.Return(_charBuffer);

                if (_byteBuffer != null)
                {
                    _bytePool.Return(_byteBuffer);
                }
            }
        }
    }
}