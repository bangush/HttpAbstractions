// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.WebUtilities
{
    public class LowAllocationDictionary<TValue> : IDictionary<string, TValue>
    {

#if !DNXCORE50
        private static readonly string[] EmptyKeys = new string[0];
        private static readonly TValue[] EmptyValues = new TValue[0];
        private static readonly KeyValuePair<string, TValue>[] EmptyEnumerator = new KeyValuePair<string, TValue>[0];
#endif 
        public IDictionary<string, TValue> Store { get; set; }

        public LowAllocationDictionary()
        {
        }

        public LowAllocationDictionary(int capacity)
        {
            Store = new Dictionary<string, TValue>(capacity, StringComparer.OrdinalIgnoreCase);
        }

        public TValue this[string key]
        {
            get
            {
                if (Store == null)
                {
                    return default(TValue);
                }
                TValue value;
                if (TryGetValue(key, out value))
                {
                    return value;
                }
                return default(TValue);
            }

            set
            {
                if (Store == null)
                {
                    Store = new Dictionary<string, TValue>(1, StringComparer.OrdinalIgnoreCase);
                }
                Store[key] = value;
            }
        }

        public int Count
        {
            get
            {
                if (Store == null)
                {
                    return 0;
                }
                return Store.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                if (Store == null)
                {
#if DNXCORE50
                    return Array.Empty<string>();
#else
                    return EmptyKeys;
#endif
                }
                return Store.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (Store == null)
                {
#if DNXCORE50
                    return Array.Empty<TValue>();
#else
                    return EmptyValues;
#endif
                }
                return Store.Values;
            }
        }

        public void Add(KeyValuePair<string, TValue> item)
        {
            if (Store == null)
            {
                Store = new Dictionary<string, TValue>(1, StringComparer.OrdinalIgnoreCase);
            }
            Store.Add(item.Key, item.Value);
        }

        public void Add(string key, TValue value)
        {
            if (Store == null)
            {
                Store = new Dictionary<string, TValue>(1);
            }
            Store.Add(key, value);
        }

        public void Clear()
        {
            if (Store == null)
            {
                return;
            }
            Store.Clear();
        }

        public bool Contains(KeyValuePair<string, TValue> item)
        {
            if (Store == null)
            {
                return false;
            }
            return Store.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            if (Store == null)
            {
                return false;
            }
            return Store.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            if (Store == null)
            {
                return;
            }

            foreach (var item in Store)
            {
                array[arrayIndex] = item;
                arrayIndex++;
            }
        }

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            if (Store == null)
            {
#if DNXCORE50
                return ((IEnumerable<KeyValuePair<string, TValue>>)Array.Empty<KeyValuePair<string, TValue>>()).GetEnumerator();
#else
                return ((IEnumerable<KeyValuePair<string, TValue>>)EmptyEnumerator).GetEnumerator();
#endif
            }
            return Store.GetEnumerator();
        }

        public bool Remove(KeyValuePair<string, TValue> item)
        {
            if (Store == null)
            {
                return false;
            }

            TValue value;

            if (Store.TryGetValue(item.Key, out value) && EqualityComparer<TValue>.Default.Equals(item.Value.Equals(value)))
            {
                return Store.Remove(item.Key);
            }
            return false;
        }

        public bool Remove(string key)
        {
            if (Store == null)
            {
                return false;
            }
            return Store.Remove(key);
        }

        public bool TryGetValue(string key, out TValue value)
        {
            if (Store == null)
            {
                value = default(TValue);
                return false;
            }
            return Store.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (Store == null)
            {
#if DNXCORE50
                return Array.Empty<TValue>().GetEnumerator();
#else
                return EmptyEnumerator.GetEnumerator();
#endif
            }
            return Store.GetEnumerator();
        }
    }
}
