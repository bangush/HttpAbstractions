// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http.Internal
{
    /// <summary>
    /// Contains the parsed form values.
    /// </summary>
    public class FormCollection : LowAllocationDictionary<StringValues>, IFormCollection
    {
        private static IFormFileCollection EmptyFiles = new FormFileCollection();

        private IFormFileCollection _files;

        public FormCollection([NotNull] IDictionary<string, StringValues> store)
        {
            Store = store;
        }

        public FormCollection([NotNull] IDictionary<string, StringValues> store, [NotNull] IFormFileCollection files)
        {
            Store = store;
            _files = files;
        }

        public IFormFileCollection Files {
            get { return _files ?? EmptyFiles; }
            private set { _files = value; }
        }
    }
}
