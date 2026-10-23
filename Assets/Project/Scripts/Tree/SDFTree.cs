using System.Collections.Generic;
using UnityEngine;

public class SDFTree : MonoBehaviour {
    [SerializeField] private Mesh _mesh;

    [Min(1)]
    [SerializeField] private Vector3Int _boxSize = Vector3Int.one;

    [SerializeField] private float _texelSize = 0.05f;

    private float[,,] _sdf;

    private List<Vector3> _vertices = new();
    private List<int> _triangles = new();

    private void Start() {
        if (_mesh == null) {
            Debug.LogError("Mesh not set for SDF");
            return;
        }

        _mesh.GetVertices(_vertices);
        _mesh.GetTriangles(_triangles, 0);
        _sdf = new float[_boxSize.x, _boxSize.y, _boxSize.z];
        Vector3 half = (Vector3)_boxSize / 2.0f;

        for (int i = 0; i < _boxSize.x; i++) {
            for (int j = 0; j < _boxSize.y; j++) {
                for (int k = 0; k < _boxSize.z; k++) {
                    _sdf[i, j, k] = GetSDF(new Vector3(i, j, k) - half);
                }
            }
        }


        _vertices = null;
        _triangles = null;
    }

    public void AddMark(Vector3 point, float size) {
        // TODO: Rework
        Vector3 half = (Vector3)_boxSize / 2.0f;

        point = transform.InverseTransformPoint(point);
        point = (point / _texelSize + half) - (Vector3.one * _texelSize / 2);

        size /= _texelSize;

        for (int i = Mathf.Max(0, (int)(point.x - size)); i < Mathf.Min(_boxSize.x, (int)(point.x + size)); i++) {
            for (int j = Mathf.Max(0, (int)(point.y - size)); j < Mathf.Min(_boxSize.y, (int)(point.y + size)); j++) {
                for (int k = Mathf.Max(0, (int)(point.z - size)); k < Mathf.Min(_boxSize.z, (int)(point.z + size)); k++) {
                    Vector3 curr = new(i, j, k);
                    _sdf[i, j, k] = Mathf.Max(_sdf[i, j, k], size - Vector3.Distance(point, curr));
                    //Debug.DrawRay(transform.TransformPoint((curr - half) * _texelSize) + (Vector3.one * _texelSize / 2), point - curr, Color.red, 5f);
                }
            }
        }
    }

    private float GetSDF(Vector3 pos) {
        float smallest = float.MaxValue;
        Vector3 smallPoint = Vector3.zero;
        Vector3 smallNormal = Vector3.zero;

        pos = pos * _texelSize + (Vector3.one * _texelSize / 2);

        for (int i = 0; i < _triangles.Count; i += 3) {
            Vector3 a = _vertices[_triangles[i]];
            Vector3 b = _vertices[_triangles[i + 1]];
            Vector3 c = _vertices[_triangles[i + 2]];
            Vector3 ab = b - a;
            Vector3 ac = c - a;

            Vector3 normal = Vector3.Cross(ab, ac);

            Vector3 onPlane = pos - Vector3.Project(pos - a, normal);

            float invN2 = 1 / Vector3.Dot(normal, normal);

            float u = Vector3.Dot(Vector3.Cross(b - pos, c - pos), normal) * invN2;
            float v = Vector3.Dot(Vector3.Cross(c - pos, a - pos), normal) * invN2;
            float w = 1.0f - u - v;

            if (u >= 0 && v >= 0 && w >= 0) {
                if (Vector3.SqrMagnitude(onPlane - pos) < smallest) {
                    smallPoint = onPlane;
                    smallNormal = normal;
                }
                smallest = Mathf.Min(smallest, Vector3.SqrMagnitude(onPlane - pos));
            }
            else {
                Vector3 qab = Project(pos, a, b);
                Vector3 qbc = Project(pos, b, c);
                Vector3 qca = Project(pos, c, a);
                if (Vector3.SqrMagnitude(qab - pos) < smallest) {
                    smallPoint = qab;
                    smallNormal = normal;
                }
                if (Vector3.SqrMagnitude(qbc - pos) < smallest) {
                    smallPoint = qbc;
                    smallNormal = normal;
                }
                if (Vector3.SqrMagnitude(qca - pos) < smallest) {
                    smallPoint = qca;
                    smallNormal = normal;
                }
                smallest = Mathf.Min(smallest,
                    Vector3.SqrMagnitude(qab - pos),
                    Vector3.SqrMagnitude(qbc - pos),
                    Vector3.SqrMagnitude(qca - pos));

            }

        }

        //Debug.DrawRay(transform.position + smallPoint, smallNormal, Color.red, 5f);

        return Mathf.Sqrt(smallest);
    }

    private Vector3 Project(Vector3 point, Vector3 a, Vector3 b) {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        return a + t * ab;
    }

    private void OnDrawGizmosSelected() {
        if (_sdf == null) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, (Vector3)_boxSize * _texelSize);
            return;
        }

        Color tmp = Color.black;

        Vector3 half = (Vector3)_boxSize / 2.0f;
        for (int i = 0; i < _boxSize.x; i++) {
            for (int j = 0; j < _boxSize.y; j++) {
                for (int k = 0; k < _boxSize.z; k++) {
                    tmp.a = (0.2f - _sdf[i, j, k]);
                    Gizmos.color = tmp;
                    Gizmos.DrawWireCube(transform.TransformPoint((new Vector3(i, j, k) - half) * _texelSize) + (Vector3.one * _texelSize / 2), Vector3.one * _texelSize);
                }
            }
        }
    }
}
