using System.Collections.Generic;

namespace DataMiner
{
    public class MultiMap<TKey, TValue>
    {
        private readonly Dictionary<TKey, IList<TValue>> _storage;

        public MultiMap()
        {
            _storage = new Dictionary<TKey, IList<TValue>>();
        }

        public void Add(TKey key, TValue value)
        {
            if (!_storage.ContainsKey(key)) _storage.Add(key, new List<TValue>());
            _storage[key].Add(value);
        }

        public IEnumerable<TKey> Keys => _storage.Keys;

        public IList<TValue> this[TKey key]
        {
            get
            {
                if (!_storage.ContainsKey(key))
                    throw new KeyNotFoundException($"The given key {key} was not found in the collection.");
                return _storage[key];
            }
        }
    }
}
