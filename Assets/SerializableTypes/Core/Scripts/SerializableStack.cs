using System.Collections.Generic;
using UnityEngine;

namespace SerializableTypes {

    /// <summary>
    /// Wrapper class for Stack.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    [System.Serializable]
    public class SerializableStack<T> : ISerializationCallbackReceiver {
        readonly private Stack<T> _data = new();
        public int Count => _data.Count;

        [SerializeField] private List<T> _stack = new();


        #region Editing
        /// <summary>
        /// Push value to the stack.
        /// </summary>
        /// <param name="value">value to push</param>
        public void Push(T value) {
            _stack.Add(value);
            _data.Push(value);
        }

        /// <summary>
        /// Try pop value from the stack.
        /// </summary>
        /// <param name="result">Output variable for result</param>
        /// <returns>true on success</returns>
        public bool TryPop(out T result) {
            if (_data.TryPop(out result)) {
                _stack.RemoveAt(_stack.Count - 1);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Try peek a value from the stack.
        /// </summary>
        /// <param name="result">Output variable for result</param>
        /// <returns>true on success</returns>
        public bool TryPeek(out T result) {
            return _data.TryPeek(out result);
        }

        /// <summary>
        /// Pop value from the stack without checking stack size.
        /// </summary>
        /// <returns>Last added element to the stack</returns>
        public T Pop() {
            _stack.RemoveAt(_stack.Count - 1);
            return _data.Pop();
        }

        /// <summary>
        /// Peek value without checking stack size.
        /// </summary>
        /// <returns>Last added element to the stack</returns>
        public T Peek() {
            return _data.Peek();
        }

        /// <summary>
        /// Clear the Stack and serialized list.
        /// </summary>
        public void Clear() {
            _data.Clear();
            _stack.Clear();
        }
        #endregion

        #region Serialization
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() {
            _data.Clear();

            foreach (T el in _stack) {
                _data.Push(el);
            }
        }
        #endregion
    }

}
