// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features;

namespace SampleApp
{
    public class PooledHttpContext : HttpContext
    {
        private readonly DefaultHttpContext _context;

        public PooledHttpContext(IFeatureCollection featureCollection, Func<HttpRequest, FormFeature> formFeatureFactory)
        {
            _context = new DefaultHttpContext(featureCollection, formFeatureFactory);
        }

        public void Initialize(IFeatureCollection featureCollection, Func<HttpRequest, FormFeature> formFeatureFactory)
        {
            _context.Initialize(featureCollection, formFeatureFactory);
        }

        public void Uninitialize()
        {
            _context.Uninitialize();
        }

        public override IFeatureCollection Features => _context.Features;

        public override HttpRequest Request => _context.Request;

        public override HttpResponse Response => _context.Response;

        public override ConnectionInfo Connection => _context.Connection;

        public override WebSocketManager WebSockets => _context.WebSockets;

        /// <summary>
        /// This is obsolete and will be removed in a future version. 
        /// The recommended alternative is to use Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.
        /// See https://go.microsoft.com/fwlink/?linkid=845470.
        /// </summary>
        [Obsolete("This is obsolete and will be removed in a future version. The recommended alternative is to use Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions. See https://go.microsoft.com/fwlink/?linkid=845470.")]
        public override AuthenticationManager Authentication => _context.Authentication;

        public override ClaimsPrincipal User
        {
            get => _context.User;
            set => _context.User = value;
        }

        public override IDictionary<object, object> Items
        {
            get => _context.Items;
            set => _context.Items = value;
        }

        public override IServiceProvider RequestServices
        {
            get => _context.RequestServices;
            set => _context.RequestServices = value;
        }

        public override CancellationToken RequestAborted
        {
            get => _context.RequestAborted;
            set => _context.RequestAborted = value;
        }

        public override string TraceIdentifier
        {
            get => _context.TraceIdentifier;
            set => _context.TraceIdentifier = value;
        }

        public override ISession Session
        {
            get => _context.Session;
            set => _context.Session = value;
        }

        public override void Abort()
        {
            _context.Abort();
        }
    }
}