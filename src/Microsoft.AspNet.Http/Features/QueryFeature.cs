// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class QueryFeature : IQueryFeature
    {

        private readonly IFeatureCollection _features;
        private FeatureReference<IHttpRequestFeature> _request = FeatureReference<IHttpRequestFeature>.Default;

        private string _original;
        private IDictionary<string, StringValues> _parsedValues;

        public QueryFeature([NotNull] IDictionary<string, StringValues> query)
        {
            _parsedValues = query;
        }

        public QueryFeature([NotNull] IFeatureCollection features)
        {
            _features = features;
        }

        public IDictionary<string, StringValues> Query
        {
            get
            {
                if (_features == null)
                {
                    if (_parsedValues == null)
                    {
                        _parsedValues = new LowAllocationDictionary<StringValues>();
                    }
                    return _parsedValues;
                }

                var current = _request.Fetch(_features).QueryString;
                if (_parsedValues == null || !string.Equals(_original, current, StringComparison.Ordinal))
                {
                    _original = current;
                    _parsedValues = QueryHelpers.ParseQuery(current);
                }
                return _parsedValues;
            }
            set
            {
                _parsedValues = value;
                if (_features != null)
                {
                    if (value == null)
                    {
                        _original = string.Empty;
                        _request.Fetch(_features).QueryString = string.Empty;
                    }
                    else
                    {
                        _original = QueryString.Create(_parsedValues).ToString();
                        _request.Fetch(_features).QueryString = _original;
                    }
                }
            }
        }
    }
}