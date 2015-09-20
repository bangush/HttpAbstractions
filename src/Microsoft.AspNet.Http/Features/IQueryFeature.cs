// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public interface IQueryFeature
    {
        IDictionary<string, StringValues> Query { get; set; }
    }
}
