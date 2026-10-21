using SerializableTypes;
using UnityEngine;

public class Tree : MonoBehaviour, IInteractable {
    [SerializeField] private int _health = 5;

    [Min(0.1f)]
    [SerializeField] private float _proximityForScore = 0.3f;

    [SerializeField] private SerializableDictionary<Vector3Int, int> _hits = new();

    public void Interact(InteractSystem system, RaycastHit hit) {
        Vector3Int point = Map(hit.point);

        if (_hits.Data.ContainsKey(point)) {
            int tmp = _hits.Data[point];
            _hits.RemoveKey(point);
            _hits.Add(point, tmp + 1);
        }
        else {
            _hits.Add(point, 1);
        }

        foreach (var h in _hits.Data) {
            if (h.Value >= _health) {
                Destroy(gameObject);
            }
        }
    }

    private Vector3Int Map(Vector3 vec) {
        return new Vector3Int(Mathf.FloorToInt(vec.x / _proximityForScore),
            Mathf.FloorToInt(vec.y / _proximityForScore),
            Mathf.FloorToInt(vec.z / _proximityForScore));
    }
}
