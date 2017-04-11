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
    public sealed class PooledHttpContext : HttpContext
    {
        private readonly DefaultHttpContext _context;

        public PooledHttpContext(IFeatureCollection featureCollection)
        {
            _context = new DefaultHttpContext(featureCollection);
        }

        public void Initialize(IFeatureCollection featureCollection)
        {
            _context.Initialize(featureCollection);
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