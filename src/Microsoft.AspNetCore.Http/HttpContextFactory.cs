// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http
{
    public class HttpContextFactory : IHttpContextFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Func<IFeatureCollection, ResponseCookiesFeature> _responseCookiesFeatureFactory;
        private readonly Func<HttpRequest, FormFeature> _formFeatureFactory;

        public HttpContextFactory(ObjectPoolProvider poolProvider, IOptions<FormOptions> formOptions)
            : this(poolProvider, formOptions, httpContextAccessor: null)
        {
        }

        public HttpContextFactory(ObjectPoolProvider poolProvider, IOptions<FormOptions> formOptions, IHttpContextAccessor httpContextAccessor)
        {
            if (poolProvider == null)
            {
                throw new ArgumentNullException(nameof(poolProvider));
            }
            if (formOptions == null)
            {
                throw new ArgumentNullException(nameof(formOptions));
            }


            var options = formOptions.Value;
            var builderPool = poolProvider.CreateStringBuilderPool();

            _responseCookiesFeatureFactory = features => new ResponseCookiesFeature(features, builderPool);
            _formFeatureFactory = request => new FormFeature(request, options);

            _httpContextAccessor = httpContextAccessor;
        }

        public HttpContext Create(IFeatureCollection featureCollection)
        {
            if (featureCollection == null)
            {
                throw new ArgumentNullException(nameof(featureCollection));
            }

            var httpContext = new DefaultHttpContext(featureCollection, _responseCookiesFeatureFactory, _formFeatureFactory);
            if (_httpContextAccessor != null)
            {
                _httpContextAccessor.HttpContext = httpContext;
            }

            return httpContext;
        }

        public void Dispose(HttpContext httpContext)
        {
            if (_httpContextAccessor != null)
            {
                _httpContextAccessor.HttpContext = null;
            }
        }
    }
}