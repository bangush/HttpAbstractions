// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Authentication.Internal;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Http.Internal;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Http
{
    public class DefaultHttpContext : HttpContext
    {
        // Lambdas hoisted to static readonly fields to improve inlining https://github.com/dotnet/roslyn/issues/13624
        private readonly static Func<IFeatureCollection, IItemsFeature> _newItemsFeature = f => new ItemsFeature();
        private readonly static Func<IFeatureCollection, IServiceProvidersFeature> _newServiceProvidersFeature = f => new ServiceProvidersFeature();
        private readonly static Func<IFeatureCollection, IHttpAuthenticationFeature> _newHttpAuthenticationFeature = f => new HttpAuthenticationFeature();
        private readonly static Func<IFeatureCollection, IHttpRequestLifetimeFeature> _newHttpRequestLifetimeFeature = f => new HttpRequestLifetimeFeature();
        private readonly static Func<IFeatureCollection, ISessionFeature> _newSessionFeature = f => new DefaultSessionFeature();
        private readonly static Func<IFeatureCollection, ISessionFeature> _nullSessionFeature = f => null;
        private readonly static Func<IFeatureCollection, IHttpRequestIdentifierFeature> _newHttpRequestIdentifierFeature = f => new HttpRequestIdentifierFeature();

        private Func<HttpRequest, FormFeature> _formFeatureFactory;

        private FeatureReferences<FeatureInterfaces> _features;

        private DefaultHttpRequest _request = null;
        private DefaultHttpResponse _response = null;

#pragma warning disable CS0618 // Type or member is obsolete
        private AuthenticationManager _authenticationManager;
#pragma warning restore CS0618 // Type or member is obsolete

        private ConnectionInfo _connection;
        private WebSocketManager _websockets;
        private bool _initialized;

        public DefaultHttpContext()
            : this(new FeatureCollection())
        {
            Features.Set<IHttpRequestFeature>(new HttpRequestFeature());
            Features.Set<IHttpResponseFeature>(new HttpResponseFeature());
        }

        public DefaultHttpContext(IFeatureCollection features)
            : this(features, null)
        {
        }

        public DefaultHttpContext(IFeatureCollection features, Func<HttpRequest, FormFeature> formFeatureFactory)
        {
            Initialize(features, formFeatureFactory);
        }

        public void Initialize(IFeatureCollection features, Func<HttpRequest, FormFeature> formFeatureFactory)
        {
            const int LazyInitialize = -1;

            _formFeatureFactory = formFeatureFactory;
            _features = new FeatureReferences<FeatureInterfaces>(features, LazyInitialize);
        }

        public void Uninitialize()
        {
            _features = default(FeatureReferences<FeatureInterfaces>);
            _request?.Uninitialize();
            _response?.Uninitialize();

            if (_authenticationManager != null)
            {
                _authenticationManager = null;
            }
            if (_connection != null)
            {
                _connection = null;
            }
            if (_websockets != null)
            {
                _websockets = null;
            }

            _initialized = false;
        }

        private IItemsFeature ItemsFeature =>
            _features.Fetch(ref _features.Cache.Items, _newItemsFeature);

        private IServiceProvidersFeature ServiceProvidersFeature =>
            _features.Fetch(ref _features.Cache.ServiceProviders, _newServiceProvidersFeature);

        private IHttpAuthenticationFeature HttpAuthenticationFeature =>
            _features.Fetch(ref _features.Cache.Authentication, _newHttpAuthenticationFeature);

        private IHttpRequestLifetimeFeature LifetimeFeature =>
            _features.Fetch(ref _features.Cache.Lifetime, _newHttpRequestLifetimeFeature);

        private ISessionFeature SessionFeature =>
            _features.Fetch(ref _features.Cache.Session, _newSessionFeature);

        private ISessionFeature SessionFeatureOrNull =>
            _features.Fetch(ref _features.Cache.Session, _nullSessionFeature);


        private IHttpRequestIdentifierFeature RequestIdentifierFeature =>
            _features.Fetch(ref _features.Cache.RequestIdentifier, _newHttpRequestIdentifierFeature);

        public override IFeatureCollection Features => _features.Collection;

        public override HttpRequest Request => _initialized ? _request : InitializeHttpRequest();

        public override HttpResponse Response => _initialized ? _response : InitializeHttpResponse();

        public override ConnectionInfo Connection => _connection ?? (_connection = InitializeConnectionInfo());

        /// <summary>
        /// This is obsolete and will be removed in a future version. 
        /// The recommended alternative is to use Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.
        /// See https://go.microsoft.com/fwlink/?linkid=845470.
        /// </summary>
        [Obsolete("This is obsolete and will be removed in a future version. The recommended alternative is to use Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions. See https://go.microsoft.com/fwlink/?linkid=845470.")]
        public override AuthenticationManager Authentication => _authenticationManager ?? (_authenticationManager = InitializeAuthenticationManager());

        public override WebSocketManager WebSockets => _websockets ?? (_websockets = InitializeWebSocketManager());


        public override ClaimsPrincipal User
        {
            get
            {
                var user = HttpAuthenticationFeature.User;
                if (user == null)
                {
                    user = new ClaimsPrincipal(new ClaimsIdentity());
                    HttpAuthenticationFeature.User = user;
                }
                return user;
            }
            set { HttpAuthenticationFeature.User = value; }
        }

        public override IDictionary<object, object> Items
        {
            get { return ItemsFeature.Items; }
            set { ItemsFeature.Items = value; }
        }

        public override IServiceProvider RequestServices
        {
            get { return ServiceProvidersFeature.RequestServices; }
            set { ServiceProvidersFeature.RequestServices = value; }
        }

        public override CancellationToken RequestAborted
        {
            get { return LifetimeFeature.RequestAborted; }
            set { LifetimeFeature.RequestAborted = value; }
        }

        public override string TraceIdentifier
        {
            get { return RequestIdentifierFeature.TraceIdentifier; }
            set { RequestIdentifierFeature.TraceIdentifier = value; }
        }

        public override ISession Session
        {
            get
            {
                var feature = SessionFeatureOrNull;
                if (feature == null)
                {
                    throw new InvalidOperationException("Session has not been configured for this application " +
                        "or request.");
                }
                return feature.Session;
            }
            set
            {
                SessionFeature.Session = value;
            }
        }

        private void InitializeRequestResponse()
        {
            var revision = _features.GetRevisionAndValidateCache();
            var collection = _features.Collection;

            if (_request == null)
            {
                _request = new DefaultHttpRequest(this, revision, collection, _formFeatureFactory);
            }
            else
            {
                _request.Initialize(collection, revision, _formFeatureFactory);
            }
            if (_response == null)
            {
                _response = new DefaultHttpResponse(this, revision, collection);
            }
            else
            {
                _response.Initialize(collection, revision);
            }

            _initialized = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private DefaultHttpRequest InitializeHttpRequest()
        {
            InitializeRequestResponse();
            return _request;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private DefaultHttpResponse InitializeHttpResponse()
        {
            InitializeRequestResponse();
            return _response;
        }

        public override void Abort()
        {
            LifetimeFeature.Abort();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ConnectionInfo InitializeConnectionInfo() => new DefaultConnectionInfo(Features);

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Obsolete("This is obsolete and will be removed in a future version. See https://go.microsoft.com/fwlink/?linkid=845470.")]
        private AuthenticationManager InitializeAuthenticationManager() => new DefaultAuthenticationManager(this);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private WebSocketManager InitializeWebSocketManager() => new DefaultWebSocketManager(Features);

        struct FeatureInterfaces
        {
            public IItemsFeature Items;
            public IServiceProvidersFeature ServiceProviders;
            public IHttpAuthenticationFeature Authentication;
            public IHttpRequestLifetimeFeature Lifetime;
            public ISessionFeature Session;
            public IHttpRequestIdentifierFeature RequestIdentifier;
        }
    }
}
