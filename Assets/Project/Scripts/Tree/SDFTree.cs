using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SDFTree : MonoBehaviour {
    [SerializeField] private Mesh _mesh;
    [SerializeField] private Renderer _renderQuad;

    [Min(1)]
    [SerializeField] private Vector3Int _boxSize = Vector3Int.one;
    private Vector3 _boxHalfSize = Vector3Int.one;

    [SerializeField] private float _texelSize = 0.05f;

    private Texture3D _tex = null;
    private Material _mat = null;

    private Vector4[,,] _sdf;

    private List<Vector3> _vertices = new();
    private List<int> _triangles = new();
    private List<Vector3> _normals = new();

    private void Start() {
        _boxHalfSize = (Vector3)_boxSize / 2.0f;
        if (_mesh == null || _renderQuad == null) {
            Debug.LogError("Mesh not set for SDF");
            return;
        }

        _tex = new Texture3D(_boxSize.x, _boxSize.y, _boxSize.z, TextureFormat.RGBAFloat, false);
        //_tex.filterMode = FilterMode.Bilinear;

        _mesh.GetVertices(_vertices);
        _mesh.GetTriangles(_triangles, 0);
        _mesh.GetNormals(_normals);
        _sdf = new Vector4[_boxSize.x, _boxSize.y, _boxSize.z];

        for (int i = 0; i < _boxSize.x; i++) {
            for (int j = 0; j < _boxSize.y; j++) {
                for (int k = 0; k < _boxSize.z; k++) {
                    _sdf[i, j, k] = GetSDF(TexelToLocal(new Vector3Int(k, j, i)));
                }
            }
        }

        _tex.SetPixelData(_sdf.Cast<Vector4>().ToArray(), 0);

        _tex.Apply();

        _mat = _renderQuad.material;

        _mat.SetTexture("_SDFTexture", _tex);
        _mat.SetFloat("_TexelSize", _texelSize);
        _mat.SetVector("_TexSize", new Vector4(_boxSize.x, _boxSize.y, _boxSize.z));

        _vertices = null;
        _triangles = null;
        _normals = null;
    }

    private void Update() {
        _mat.SetMatrix("_InvObjectTransform", transform.worldToLocalMatrix);
    }

    public void AddMark(Vector3 point, float size) {
        // TODO: Rework
        point = WorldToTexel(point);

        size /= _texelSize;

        for (int i = Mathf.Max(0, (int)(point.x - size * 2)); i < Mathf.Min(_boxSize.x, (int)(point.x + size * 2)); i++) {
            for (int j = Mathf.Max(0, (int)(point.y - size * 2)); j < Mathf.Min(_boxSize.y, (int)(point.y + size * 2)); j++) {
                for (int k = Mathf.Max(0, (int)(point.z - size * 2)); k < Mathf.Min(_boxSize.z, (int)(point.z + size * 2)); k++) {
                    Vector3 curr = new(k, j, i);
                    //_sdf[i, j, k].w = Mathf.Max(_sdf[i, j, k].w, (size - Vector3.Distance(point, curr)));
                    float dist = Vector3.Distance(point, curr);
                    if (_sdf[i, j, k].w <= 0) {
                        if (dist <= size) {
                            _sdf[i, j, k].w = (size - dist) / _boxHalfSize.x;
                            Vector3 norm = point - curr;
                            _sdf[i, j, k].x = norm.x;
                            _sdf[i, j, k].y = norm.y;
                            _sdf[i, j, k].z = norm.z;
                        }
                        else {
                            float tmp = (size - dist) / _boxHalfSize.x;
                            if (tmp > _sdf[i, j, k].w) {
                                _sdf[i, j, k].w = tmp;
                                Vector3 norm = point - curr;
                                _sdf[i, j, k].x = norm.x;
                                _sdf[i, j, k].y = norm.y;
                                _sdf[i, j, k].z = norm.z;
                            }
                        }
                    }
                    else {
                        _sdf[i, j, k].w = Mathf.Max(_sdf[i, j, k].w, (size - dist) / _boxHalfSize.x);
                    }
                    //Debug.DrawRay(TexelToWorld(new(i, j, k)), point - curr, Color.red, 5f);
                }
            }
        }

        _tex.SetPixelData(_sdf.Cast<Vector4>().ToArray(), 0);

        _tex.Apply();
    }

    private Vector4 GetSDF(Vector3 pos) {
        float smallest = float.MaxValue;
        float sign = 1f;
        Vector3 smallNormal = Vector3.zero;

        int smallestTri = 0;

        for (int i = 0; i < _triangles.Count; i += 3) {
            Vector3 a = _vertices[_triangles[i]];
            Vector3 b = _vertices[_triangles[i + 1]];
            Vector3 c = _vertices[_triangles[i + 2]];
            Vector3 ab = b - a;
            Vector3 ac = c - a;

            Vector3 normal = Vector3.Cross(ab, ac);
            Vector3 projected = Vector3.Project(pos - a, normal);

            Vector3 onPlane = pos - projected;

            float invN2 = 1 / Vector3.Dot(normal, normal);

            float u = Vector3.Dot(Vector3.Cross(b - pos, c - pos), normal) * invN2;
            float v = Vector3.Dot(Vector3.Cross(c - pos, a - pos), normal) * invN2;
            float w = 1.0f - u - v;

            float tmpSign = Vector3.Dot(normal, projected) > 0 ? 1 : -1;

            if (u >= 0 && v >= 0 && w >= 0) {
                var mag = Vector3.SqrMagnitude(onPlane - pos);
                if (mag < smallest) {
                    smallest = mag;
                    sign = tmpSign;
                    smallestTri = i;
                    smallNormal = normal;
                }
            }
            else {
                Vector3 qab = Project(pos, a, b);
                Vector3 qbc = Project(pos, b, c);
                Vector3 qca = Project(pos, c, a);
                var mag = Mathf.Min(Vector3.SqrMagnitude(qab - pos),
                    Vector3.SqrMagnitude(qbc - pos),
                    Vector3.SqrMagnitude(qca - pos));
                if (mag < smallest) {
                    smallest = mag;
                    sign = Vector3.Dot(normal, projected) > 0 ? 1 : -1;
                    smallestTri = i;
                    smallNormal = normal;
                }
            }
        }

        Vector3 a1 = _vertices[_triangles[smallestTri]];
        Vector3 b1 = _vertices[_triangles[smallestTri + 1]];
        Vector3 c1 = _vertices[_triangles[smallestTri + 2]];

        var f1 = a1 - pos;
        var f2 = b1 - pos;
        var f3 = c1 - pos;
        
        var area = Vector3.Cross(a1 - b1, a1 - c1);
        var va1 = Vector3.Cross(f2, f3);
        var va2 = Vector3.Cross(f3, f1);
        var va3 = Vector3.Cross(f1, f2);

        var areaV = area.magnitude;

        var ar1 = va1.magnitude / areaV * Mathf.Sign(Vector3.Dot(area, va1));
        var ar2 = va2.magnitude / areaV * Mathf.Sign(Vector3.Dot(area, va2));
        var ar3 = va3.magnitude / areaV * Mathf.Sign(Vector3.Dot(area, va3));

        Vector3 norm = _normals[_triangles[smallestTri]] * ar1
            + _normals[_triangles[smallestTri + 1]] * ar2
            + _normals[_triangles[smallestTri + 2]] * ar3;

        //var uv = uv1 * a1 + uv2 * a2 + uv3 * a3;

        //Debug.DrawRay(transform.position + smallPoint, smallNormal, Color.red, 5f);

        return new Vector4(norm.x, norm.y, norm.z, Mathf.Sqrt(smallest) / TexelToLocal(_boxHalfSize.x) * sign);
    }

    private Vector3Int LocalToTexel(Vector3 pos) {
        return Vector3Int.FloorToInt(pos / _texelSize + _boxHalfSize);
    }

    private Vector3 TexelToLocal(Vector3Int pos) {
        return ((Vector3)pos - _boxHalfSize) * _texelSize;
    }

    private Vector3Int WorldToTexel(Vector3 pos) {
        return Vector3Int.FloorToInt(transform.InverseTransformPoint(pos) / _texelSize + _boxHalfSize);
    }

    private Vector3 TexelToWorld(Vector3Int pos) {
        return transform.TransformPoint(((Vector3)pos - _boxHalfSize) * _texelSize);
    }

    private float TexelToLocal(float val) {
        return val * _texelSize;
    }

    private int LocalToTexel(float val) {
        return (int)(val / _texelSize);
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
            Gizmos.DrawWireCube(transform.position, TexelToLocal(_boxSize * 2));
            return;
        }

        Color tmp = Color.black;

        for (int i = 0; i < _boxSize.x; i++) {
            for (int j = 0; j < _boxSize.y; j++) {
                for (int k = 0; k < _boxSize.z; k++) {
                    if (_sdf[i, j, k].w < 0.01f) {

                    //tmp.a = (0.2f - _sdf[i, j, k].w);
                    Gizmos.color = tmp;
                    Gizmos.DrawWireCube(TexelToWorld(new Vector3Int(i, j, k)), Vector3.one * _texelSize * 2);
                    }
                }
            }
        }
    }
}
