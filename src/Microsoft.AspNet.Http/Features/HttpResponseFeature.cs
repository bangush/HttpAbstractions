// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Framework.Primitives;
using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class HttpResponseFeature : IHttpResponseFeature
    {
        IDictionary<string, StringValues> _headers;
        public HttpResponseFeature()
        {
            StatusCode = 200;
            Body = Stream.Null;
        }

        public int StatusCode { get; set; }

        public virtual long? ContentLength
        {
            get
            {
                return ParsingHelpers.GetContentLength(Headers);
            }
            set
            {
                ParsingHelpers.SetContentLength(Headers, value);
            }
        }

        public string ReasonPhrase { get; set; }

        public IDictionary<string, StringValues> Headers
        {
            get
            {
                if (_headers == null)
                {
                    _headers = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
                }
                return _headers;
            }
            set
            {
                _headers = value;
            }
        }

        public Stream Body { get; set; }

        public bool HasStarted
        {
            get { return false; }
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }
    }
}
