using System.Collections.Generic;
using UnityEngine;

namespace SerializableTypes {

    /// <summary>
    /// Wrapper class for Queue.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    [System.Serializable]
    public class SerializableQueue<T> : ISerializationCallbackReceiver {
        readonly private Queue<T> _data = new();
        public int Count => _data.Count;

        [SerializeField] private List<T> _queue = new();

        #region Editing
        /// <summary>
        /// Enqueue a value to the end of the queue.
        /// </summary>
        /// <param name="value">Value to enqueue</param>
        public void Enqueue(T value) {
            _data.Enqueue(value);
            _queue.Add(value);
        }

        /// <summary>
        /// Try dequeue a value from the queue.
        /// </summary>
        /// <param name="result">Output variable for result</param>
        /// <returns>true on success</returns>
        public bool TryDequeue(out T result) {
            if (_data.TryDequeue(out result)) {
                _queue.RemoveAt(0);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Try peek a value from the queue.
        /// </summary>
        /// <param name="result">Output variable for result</param>
        /// <returns>true on success</returns>
        public bool TryPeek(out T result) {
            return _data.TryPeek(out result);
        }

        /// <summary>
        /// Dequeue a value from the queue without checking queue size.
        /// </summary>
        /// <returns>First added element to the queue</returns>
        public T Dequeue() {
            _queue.RemoveAt(0);
            return _data.Dequeue();
        }

        /// <summary>
        /// Peek a value from the queue without checking queue size.
        /// </summary>
        /// <returns>First added element to the queue</returns>
        public T Peek() {
            return _data.Peek();
        }

        /// <summary>
        /// Clear the Queue and serialized list.
        /// </summary>
        public void Clear() {
            _data.Clear();
            _queue.Clear();
        }
        #endregion

        #region Serialization
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() {
            _data.Clear();

            foreach (T el in _queue) {
                _data.Enqueue(el);
            }
        }
        #endregion
    }

}
