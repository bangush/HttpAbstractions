// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Internal;
using Microsoft.Net.Http.Headers;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http.Internal
{
    public class DefaultHttpRequest : HttpRequest
    {
        private readonly DefaultHttpContext _context;
        private readonly IFeatureCollection _features;

        private FeatureReference<IHttpRequestFeature> _request = FeatureReference<IHttpRequestFeature>.Default;
        private FeatureReference<IQueryFeature> _query = FeatureReference<IQueryFeature>.Default;
        private FeatureReference<IFormFeature> _form = FeatureReference<IFormFeature>.Default;
        private FeatureReference<IRequestCookiesFeature> _cookies = FeatureReference<IRequestCookiesFeature>.Default;

        public DefaultHttpRequest(DefaultHttpContext context, IFeatureCollection features)
        {
            _context = context;
            _features = features;
        }

        private IHttpRequestFeature HttpRequestFeature
        {
            get { return _request.Fetch(_features); }
        }

        private IQueryFeature QueryFeature
        {
            get { return _query.Fetch(_features) ?? _query.Update(_features, new QueryFeature(_features)); }
        }

        private IFormFeature FormFeature
        {
            get { return _form.Fetch(_features) ?? _form.Update(_features, new FormFeature(this)); }
        }

        private IRequestCookiesFeature RequestCookiesFeature
        {
            get { return _cookies.Fetch(_features) ?? _cookies.Update(_features, new RequestCookiesFeature(_features)); }
        }

        public override HttpContext HttpContext { get { return _context; } }

        public override PathString PathBase
        {
            get { return new PathString(HttpRequestFeature.PathBase); }
            set { HttpRequestFeature.PathBase = value.Value; }
        }

        public override PathString Path
        {
            get { return new PathString(HttpRequestFeature.Path); }
            set { HttpRequestFeature.Path = value.Value; }
        }

        public override QueryString QueryString
        {
            get { return new QueryString(HttpRequestFeature.QueryString); }
            set { HttpRequestFeature.QueryString = value.Value; }
        }

        public override long? ContentLength
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

        public override Stream Body
        {
            get { return HttpRequestFeature.Body; }
            set { HttpRequestFeature.Body = value; }
        }

        public override string Method
        {
            get { return HttpRequestFeature.Method; }
            set { HttpRequestFeature.Method = value; }
        }

        public override string Scheme
        {
            get { return HttpRequestFeature.Scheme; }
            set { HttpRequestFeature.Scheme = value; }
        }

        public override bool IsHttps
        {
            get { return string.Equals(Constants.Https, Scheme, StringComparison.OrdinalIgnoreCase); }
            set { Scheme = value ? Constants.Https : Constants.Http; }
        }

        public override HostString Host
        {
            get { return HostString.FromUriComponent(Headers["Host"]); }
            set { Headers["Host"] = value.ToUriComponent(); }
        }

        public override IDictionary<string, StringValues> Query
        {
            get { return QueryFeature.Query; }
            set { QueryFeature.Query = value; }
        }

        public override string Protocol
        {
            get { return HttpRequestFeature.Protocol; }
            set { HttpRequestFeature.Protocol = value; }
        }

        public override IDictionary<string, StringValues> Headers
        {
            get { return HttpRequestFeature.Headers; }
        }

        public override IDictionary<string, StringValues> Cookies
        {
            get { return RequestCookiesFeature.Cookies; }
            set { RequestCookiesFeature.Cookies = value; }
        }

        public override string ContentType
        {
            get
            {
                StringValues value;
                if (Headers.TryGetValue(HeaderNames.ContentType, out value))
                {
                    return value;
                }
                return null;
            }
            set { Headers[HeaderNames.ContentType] = value; }
        }

        public override bool HasFormContentType
        {
            get { return FormFeature.HasFormContentType; }
        }

        public override IFormCollection Form
        {
            get { return FormFeature.ReadForm(); }
            set { FormFeature.Form = value; }
        }

        public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken)
        {
            return FormFeature.ReadFormAsync(cancellationToken);
        }
    }
}