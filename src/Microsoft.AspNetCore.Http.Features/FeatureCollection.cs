// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Http.Features
{
    public class FeatureCollection : IFeatureCollection
    {
        private static KeyComparer FeatureKeyComparer = new KeyComparer();
        private readonly IFeatureCollection _defaults;
        private IDictionary<Type, object> _features;
        private volatile int _containerRevision;

        public FeatureCollection()
        {
        }

        public FeatureCollection(IFeatureCollection defaults)
        {
            _defaults = defaults;
        }

        public virtual int Revision
        {
            get { return _containerRevision + (_defaults?.Revision ?? 0); }
        }

        public bool IsReadOnly { get { return false; } }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            if (_features != null)
            {
                foreach (var pair in _features)
                {
                    yield return pair;
                }
            }

            if (_defaults != null)
            {
                // Don't return features masked by the wrapper.
                foreach (var pair in _features == null ? _defaults : _defaults.Except(_features, FeatureKeyComparer))
                {
                    yield return pair;
                }
            }
        }

        public TFeature Get<TFeature>() where TFeature : class
        {
            TFeature instance;
            if (_features != null && StronglyTyped<TFeature>.Features.TryGetValue(this, out instance))
            {
                return instance;
            }
            else if (_defaults != null && StronglyTyped<TFeature>.Features.TryGetValue(_defaults, out instance))
            {
                return instance;
            }
            return null;
        }

        public void Set<TFeature>(TFeature instance) where TFeature : class
        {
            var cwt = StronglyTyped<TFeature>.Features;
            var removed = false;
            // remove+add https://github.com/dotnet/coreclr/issues/4545
            removed = (_features?.Remove(typeof(TFeature)) ?? false) && cwt.Remove(this);
            if (instance == null)
            {
                if (removed)
                {
                    _containerRevision++;
                }
                return;
            }
            cwt.Add(this, instance);
            if (_features == null)
            {
                _features = new Dictionary<Type, object>();
            }
            _features[typeof(TFeature)] = instance;
            _containerRevision++;
        }

        private static class StronglyTyped<TFeature> where TFeature : class
        {
            public static ConditionalWeakTable<IFeatureCollection, TFeature> Features { get; } = new ConditionalWeakTable<IFeatureCollection, TFeature>();
        }

        private class KeyComparer : IEqualityComparer<KeyValuePair<Type, object>>
        {
            public bool Equals(KeyValuePair<Type, object> x, KeyValuePair<Type, object> y)
            {
                return x.Key.Equals(y.Key);
            }

            public int GetHashCode(KeyValuePair<Type, object> obj)
            {
                return obj.Key.GetHashCode();
            }
        }
    }
}