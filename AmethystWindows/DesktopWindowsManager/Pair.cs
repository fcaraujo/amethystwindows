using System.Collections.Generic;

namespace AmethystWindows.DesktopWindowsManager
{
    // Check where/why we need this struct instead of Dictionary....?!
    public struct Pair<K, V>
    {
        public K Key { get; set; }
        public V Value { get; set; }

        public Pair(K key, V value)
        {
            Key = key;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            return obj is Pair<K, V> pair &&
                   EqualityComparer<K>.Default.Equals(Key, pair.Key) &&
                   EqualityComparer<V>.Default.Equals(Value, pair.Value);
        }

        public override int GetHashCode()
        {
            int hashCode = -1030903623;
            hashCode = hashCode * -1521134295 + EqualityComparer<K>.Default.GetHashCode(Key);
            hashCode = hashCode * -1521134295 + EqualityComparer<V>.Default.GetHashCode(Value);
            return hashCode;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
