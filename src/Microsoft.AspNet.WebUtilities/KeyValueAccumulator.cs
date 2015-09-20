// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.WebUtilities
{
    public struct KeyValueAccumulator
    {
        private Dictionary<string, List<string>> _accumulator;

        public void Append(string key, string value)
        {
            if (_accumulator == null)
            {
                _accumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            }
            List<string> values;
            if (_accumulator.TryGetValue(key, out values))
            {
                values.Add(value);
            }
            else
            {
                _accumulator[key] = new List<string>(1) { value };
            }
        }

        public bool HasValues => _accumulator != null;

        public IDictionary<string, StringValues> GetResults()
        {
            if (_accumulator == null)
            {
                return new LowAllocationDictionary<StringValues>();
            }

            var results = new LowAllocationDictionary<StringValues>(_accumulator.Count);

            foreach (var kv in _accumulator)
            {
                results.Add(kv.Key, kv.Value.ToArray());
            }
            return results;
        }
    }
}