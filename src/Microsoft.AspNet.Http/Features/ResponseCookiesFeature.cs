// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class ResponseCookiesFeature : IResponseCookiesFeature
    {
        private readonly IFeatureCollection _features;
        private readonly FeatureReference<IHttpResponseFeature> _request = FeatureReference<IHttpResponseFeature>.Default;
        private IResponseCookies _cookiesCollection;

        public ResponseCookiesFeature(IFeatureCollection features)
        {
            _features = features;
        }

        public IResponseCookies Cookies
        {
            get
            {
                if (_cookiesCollection == null)
                {
                    var headers = _request.Fetch(_features).Headers;
                    _cookiesCollection = new ResponseCookies(headers);
                }

                return _cookiesCollection;
            }
        }
    }
}