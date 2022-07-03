using System;
using System.Collections.Generic;

namespace AmethystWindows.Models
{
    // TODO Check where/why we need this struct instead of Dictionary....?!
    public struct Pair<K, V>
    {
        public K Key { get; set; }
        public V Value { get; set; }

        public Pair(K key, V value)
        {
            Key = key;
            Value = value;
        }

        public override bool Equals(object? obj)
        {
            return obj is Pair<K, V> pair &&
                   EqualityComparer<K>.Default.Equals(Key, pair.Key) &&
                   EqualityComparer<V>.Default.Equals(Value, pair.Value);
        }

        public override int GetHashCode()
        {
            var k = Key ?? throw new ArgumentNullException(nameof(Key));
            var v = Value ?? throw new ArgumentNullException(nameof(Value));

            var hashCode = -1030903623;
            hashCode = hashCode * -1521134295 + EqualityComparer<K>.Default.GetHashCode(k);
            hashCode = hashCode * -1521134295 + EqualityComparer<V>.Default.GetHashCode(v);

            return hashCode;
        }

        // This apparently is never used
        public override string ToString()
        {
            var str = base.ToString() ?? string.Empty;
            return str;
        }
    }
}
