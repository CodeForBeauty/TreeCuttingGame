using UnityEngine;

namespace SerializableTypes {

    /// <summary>
    /// SerializeReference wrapper for classes.
    /// </summary>
    /// <typeparam name="T">Wrapped object type</typeparam>
    [System.Serializable]
    public class RefWrapper<T> where T : class {
        [SerializeReference, SubclassSelector] private T _value;
        public T Value {
            get => _value;
            set => _value = value;
        }


        public static implicit operator T(RefWrapper<T> wrapper) => wrapper?._value;

        public static implicit operator RefWrapper<T>(T value) => new() { _value = value };

        public override string ToString() => _value?.ToString() ?? "null";
    }

}
