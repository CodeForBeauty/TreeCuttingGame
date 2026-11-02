using SerializableTypes;
using UnityEngine;

[RequireComponent(typeof(TreeFall))]
[RequireComponent(typeof(SDFTree))]
public class CuttableTree : MonoBehaviour, IInteractable {
    [SerializeField] private int _health = 5;

    [Min(0.1f)]
    [SerializeField] private float _proximityForScore = 0.3f;

    [Min(0.01f)]
    [SerializeField] private Vector3 _hitDentSize = new Vector3(0.2f, 0.1f, 0.2f);

    [SerializeField] private ParticleSystem _impactParticles;
    [SerializeField] private AudioClip _impactAudio;

    [SerializeField] private SerializableDictionary<Vector3Int, int> _hits = new();
    [SerializeField] private SerializableDictionary<int, int> _hitLevels = new();
    [SerializeField] private SerializableDictionary<Vector3Int, Vector3> _damagedNormals = new();

    private TreeFall _fall;
    private SDFTree _sdf;

    private void Awake() {
        _fall = GetComponent<TreeFall>();
        _sdf = GetComponent<SDFTree>();
    }

    public void Interact(InteractSystem system, RaycastHit hit, Vector3 forward) {
        Vector3Int point = Map(hit.point);

        if (_hits.Data.ContainsKey(point)) {
            _hits.Set(point, _hits.Data[point] + 1);
            _hitLevels.Set(point.y, _hitLevels.Data[point.y] + 1);
            _damagedNormals.Set(point, forward);
        }
        else {
            _hits.Add(point, 1);
            _hitLevels.Add(point.y, 1);
            _damagedNormals.Add(point, forward);
        }

        AudioSource.PlayClipAtPoint(_impactAudio, hit.point);
        Instantiate(_impactParticles, hit.point, Quaternion.identity);
        _sdf.AddMark(hit.point, forward, _hitDentSize);

        foreach (var h in _hitLevels.Data) {
            if (h.Value >= _health) {
                _sdf.Separate(UnMap(point));
                TreeFall(h.Key);
            }
        }
    }

    private void TreeFall(int height) {
        Destroy(this);

        int mostHits = -1;
        Vector3Int mostDamagedPoint = Vector3Int.zero;
        Vector3 normal = Vector3.zero;

        foreach (var h in _hits.Data) {
            if (h.Key.y == height) {
                if (h.Value > mostHits) {
                    mostHits = h.Value;
                    mostDamagedPoint = h.Key;
                    normal = -_damagedNormals.Data[h.Key];
                }
            }
        }

        _fall.StartFall(normal, UnMap(mostDamagedPoint));
    }

    private Vector3Int Map(Vector3 vec) {
        vec = transform.InverseTransformPoint(vec);
        return Vector3Int.FloorToInt(vec / _proximityForScore);
    }

    private Vector3 UnMap(Vector3Int vec) {
        return transform.TransformPoint((Vector3)vec * _proximityForScore);
    }
}
