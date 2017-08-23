// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace SampleApp
{
    public class PooledHttpContextFactory : IHttpContextFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Stack<PooledHttpContext> _pool = new Stack<PooledHttpContext>();
        private readonly Func<HttpRequest, FormFeature> _formFeatureFactory;

        public PooledHttpContextFactory(IOptions<FormOptions> formOptions)
            : this(formOptions, httpContextAccessor: null)
        {
        }

        public PooledHttpContextFactory(IOptions<FormOptions> formOptions, IHttpContextAccessor httpContextAccessor)
        {
            if (formOptions == null)
            {
                throw new ArgumentNullException(nameof(formOptions));
            }

            _formFeatureFactory = request => new FormFeature(request, formOptions.Value);
            _httpContextAccessor = httpContextAccessor;
        }

        public HttpContext Create(IFeatureCollection featureCollection)
        {
            if (featureCollection == null)
            {
                throw new ArgumentNullException(nameof(featureCollection));
            }

            PooledHttpContext httpContext = null;
            lock (_pool)
            {
                if (_pool.Count != 0)
                {
                    httpContext = _pool.Pop();
                }
            }

            if (httpContext == null)
            {
                httpContext = new PooledHttpContext(featureCollection, _formFeatureFactory);
            }
            else
            {
                httpContext.Initialize(featureCollection, _formFeatureFactory);
            }

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

            var pooled = httpContext as PooledHttpContext;
            if (pooled != null)
            {
                pooled.Uninitialize();
                lock (_pool)
                {
                    _pool.Push(pooled);
                }
            }
        }
    }
}
