using System.Collections.Generic;

namespace WFCLevelGeneration.Util
{
    public class LinkedHashMap<K, V>
    {
        private Dictionary<K, LinkedListNode<LinkedHashMapItem<K, V>>> _cacheMap =
            new Dictionary<K, LinkedListNode<LinkedHashMapItem<K, V>>>();

        private LinkedList<LinkedHashMapItem<K, V>> _linkedList = new LinkedList<LinkedHashMapItem<K, V>>();

        public int Count => _cacheMap.Count;

        public V this[K c]
        {
            get => _cacheMap[c].Value.Value;

            set
            {
                if (_cacheMap.ContainsKey(c)) _linkedList.Remove(_cacheMap[c]);

                _cacheMap[c] = new LinkedListNode<LinkedHashMapItem<K, V>>(new LinkedHashMapItem<K, V>(c, value));
                _linkedList.AddLast(_cacheMap[c]);
            }
        }

        public bool ContainsKey(K k)
        {
            return _cacheMap.ContainsKey(k);
        }

        public LinkedHashMapItem<K, V> RemoveFirst()
        {
            var node = _linkedList.First;
            _linkedList.Remove(node);
            _cacheMap.Remove(node.Value.Key);
            return node.Value;
        }

        public void Add(K key, V value)
        {
            var node = new LinkedListNode<LinkedHashMapItem<K, V>>(new LinkedHashMapItem<K, V>(key, value));
            _linkedList.AddLast(node);
            _cacheMap.Add(key, node);
        }
    }

    public struct LinkedHashMapItem<K, V>
    {
        public K Key;
        public V Value;

        public LinkedHashMapItem(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }
}