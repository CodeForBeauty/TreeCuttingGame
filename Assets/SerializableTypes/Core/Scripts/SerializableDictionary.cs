using System.Collections.Generic;
using UnityEngine;


namespace SerializableTypes {

    /// <summary>
    /// Dictionary wrapper with elements serialized as values.
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TVal">Value type</typeparam>
    [System.Serializable]
    public class SerializableDictionary<TKey, TVal> : ISerializationCallbackReceiver {
        /// <summary>
        /// Key-Value pair for a list.
        /// </summary>
        [System.Serializable]
        public class KeyValue {
            public TKey Key;
            public TVal Value;
        }

        readonly private Dictionary<TKey, TVal> _data = new();
        public IReadOnlyDictionary<TKey, TVal> Data => _data;


        [SerializeField] private List<KeyValue> _dictionary = new();
        [SerializeField] protected bool _hasDuplicates = false;

        #region Editing
        /// <summary>
        /// Tries to add key value pair to the Dictionary.
        /// </summary>
        /// <param name="key">Key for the addition</param>
        /// <param name="value">Value for the addition</param>
        /// <returns>false if failed, true if added</returns>
        public bool Add(TKey key, TVal value) {
            if (_data.ContainsKey(key)) {
                return false;
            }

            _data.Add(key, value);
            _dictionary.Add(new KeyValue() { Key = key, Value = value });
            return true;
        }

        /// <summary>
        /// Tries to remove the key from dictionary.
        /// Removes all of the occurrences in a serialized list.
        /// </summary>
        /// <param name="key">Key to remove</param>
        /// <returns>false if key doesn't exist, true if key is removed</returns>
        public bool RemoveKey(TKey key) {
            if (!_data.ContainsKey(key)) {
                return false;
            }

            _data.Remove(key);
            for (int i = _dictionary.Count - 1; i >= 0; i--) {
                if (_dictionary[i].Key.Equals(key)) {
                    _dictionary.RemoveAt(i);
                }
            }
            return true;
        }

        /// <summary>
        /// Clear the dictionary and serialized list.
        /// </summary>
        public void Clear() {
            _data.Clear();
            _dictionary.Clear();
        }
        #endregion

        #region Serialization
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() {
            _data.Clear();
            _hasDuplicates = false;

            foreach (KeyValue kv in _dictionary) {
                if (_data.ContainsKey(kv.Key)) {
                    Debug.LogWarning($"Duplicate key: {kv.Key} in a dictionary");
                    _hasDuplicates = true;
                }
                else {
                    _data.Add(kv.Key, kv.Value);
                }
            }
        }
        #endregion
    }

}
