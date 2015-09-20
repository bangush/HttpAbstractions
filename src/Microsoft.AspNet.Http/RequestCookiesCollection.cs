// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http.Internal
{
    public class RequestCookiesCollection : LowAllocationDictionary<string>, IDictionary<string, StringValues>
    {
        /// <summary>
        /// Get the associated values from the collection in their original format.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IList<string> GetValues(string key)
        {
            string value;
            return TryGetValue(key, out value) ? new[] { value } : null;
        }

        public void Reparse(IList<string> values)
        {
            Clear();

            IList<CookieHeaderValue> cookies;
            if (CookieHeaderValue.TryParseList(values, out cookies))
            {
                foreach (var cookie in cookies)
                {
                    var name = Uri.UnescapeDataString(cookie.Name.Replace('+', ' '));
                    var value = Uri.UnescapeDataString(cookie.Value.Replace('+', ' '));
                    this[name] = value;
                }
            }
        }

        StringValues IDictionary<string, StringValues>.this[string key]
        {
            get
            {
                string value;
                if (TryGetValue(key, out value))
                {
                    return new StringValues(value);
                }
                return new StringValues();
            }
            set { this[key] = value; }
        }

        int ICollection<KeyValuePair<string, StringValues>>.Count => Count;

        bool ICollection<KeyValuePair<string, StringValues>>.IsReadOnly => IsReadOnly;

        ICollection<string> IDictionary<string, StringValues>.Keys => Keys;

        ICollection<StringValues> IDictionary<string, StringValues>.Values => (ICollection<StringValues>)Values;

        void ICollection<KeyValuePair<string, StringValues>>.Add(KeyValuePair<string, StringValues> item)
        {
            Add(item.Key, item.Value.ToString());
        }

        void IDictionary<string, StringValues>.Add(string key, StringValues value)
        {
            Add(key, value.ToString());
        }

        void ICollection<KeyValuePair<string, StringValues>>.Clear()
        {
            Clear();
        }

        bool ICollection<KeyValuePair<string, StringValues>>.Contains(KeyValuePair<string, StringValues> item)
        {
            return Contains(new KeyValuePair<string, string>(item.Key, item.Value.ToString()));
        }

        bool IDictionary<string, StringValues>.ContainsKey(string key) => ContainsKey(key);

        void ICollection<KeyValuePair<string, StringValues>>.CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {

            if (Store == null)
            {
                return;
            }

            foreach (var item in Store)
            {
                array[arrayIndex] = new KeyValuePair<string, StringValues>(item.Key, item.Value);
                arrayIndex++;
            }
        }

        IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, StringValues>>)Store).GetEnumerator();
        }

        bool ICollection<KeyValuePair<string, StringValues>>.Remove(KeyValuePair<string, StringValues> item)
        {
            if (Store == null)
            {
                return false;
            }

            string value;

            if (Store.TryGetValue(item.Key, out value) && item.Value == value)
            {
                return Store.Remove(item.Key);
            }
            return false;
        }

        bool IDictionary<string, StringValues>.Remove(string key) => this.Remove(key);

        bool IDictionary<string, StringValues>.TryGetValue(string key, out StringValues value)
        {
            string val;
            if (!TryGetValue(key, out val))
            {
                return false;
            }
            value = val;
            return true;
        }
    }
}