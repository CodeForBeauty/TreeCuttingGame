using SerializableTypes;
using UnityEngine;

[RequireComponent(typeof(TreeFall))]
public class Tree : MonoBehaviour, IInteractable {
    [SerializeField] private int _health = 5;

    [Min(0.1f)]
    [SerializeField] private float _proximityForScore = 0.3f;

    [SerializeField] private SerializableDictionary<Vector3Int, int> _hits = new();
    [SerializeField] private SerializableDictionary<int, int> _hitLevels = new();

    private TreeFall _fall;

    private void Awake() {
        _fall = GetComponent<TreeFall>();
    }

    public void Interact(InteractSystem system, RaycastHit hit) {
        Vector3Int point = Map(hit.point);

        if (_hits.Data.ContainsKey(point)) {
            _hits.Set(point, _hits.Data[point] + 1);
            _hitLevels.Set(point.y, _hitLevels.Data[point.y] + 1);
        }
        else {
            _hits.Add(point, 1);
            _hitLevels.Add(point.y, 1);
        }

        foreach (var h in _hitLevels.Data) {
            if (h.Value >= _health) {
                TreeFall(h.Key, hit.normal);
            }
        }
    }

    private void TreeFall(int height, Vector3 normal) {
        Destroy(this);

        int mostHits = -1;
        Vector3Int mostDamagedPoint = Vector3Int.zero;

        foreach (var h in _hits.Data) {
            if (h.Key.y == height) {
                if (h.Value > mostHits) {
                    mostHits = h.Value;
                    mostDamagedPoint = h.Key;
                }
            }
        }

        _fall.StartFall(normal, UnMap(mostDamagedPoint));
        print($"Fall from: {mostDamagedPoint}");
    }

    private Vector3Int Map(Vector3 vec) {
        vec = transform.InverseTransformPoint(vec);
        return new Vector3Int(Mathf.FloorToInt(vec.x / _proximityForScore),
            Mathf.FloorToInt(vec.y / _proximityForScore),
            Mathf.FloorToInt(vec.z / _proximityForScore));
    }

    private Vector3 UnMap(Vector3Int vec) {
        return (Vector3)vec * _proximityForScore;
    }
}
