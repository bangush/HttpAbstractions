// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http
{
    public interface IFormFile
    {
        string ContentType { get; }

        string ContentDisposition { get; }

        IDictionary<string, StringValues> Headers { get; }

        long Length { get; }

        Stream OpenReadStream();
    }
}