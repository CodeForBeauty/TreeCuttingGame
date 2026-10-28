using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class SDFTree : MonoBehaviour {
    [SerializeField] private Mesh _mesh;
    [SerializeField] private Renderer _renderQuad;

    [Min(1)]
    [SerializeField] private Vector3Int _boxSize = Vector3Int.one;
    private Vector3 _boxHalfSize = Vector3Int.one;

    [SerializeField] private float _texelSize = 0.05f;

    private Texture3D _tex = null;
    private Material _mat = null;

    private Texture3D _uvTex = null;
    private Texture3D _tangentTex = null;

    private Vector4[,,] _sdf;
    private Vector2[,,] _sdfUV;
    private Vector4[,,] _sdfTangent;

    private List<Vector3> _vertices = new();
    private List<int> _triangles = new();
    private List<Vector3> _normals = new();
    private List<Vector4> _tangents = new();
    private List<Vector2> _uvs = new();

    private void Start() {
        _boxHalfSize = (Vector3)_boxSize / 2.0f;
        if (_mesh == null || _renderQuad == null) {
            Debug.LogError("Mesh not set for SDF");
            return;
        }

        _tex = new Texture3D(_boxSize.x, _boxSize.y, _boxSize.z, TextureFormat.RGBAFloat, false);
        _tex.filterMode = FilterMode.Bilinear;

        _uvTex = new Texture3D(_boxSize.x, _boxSize.y, _boxSize.z, TextureFormat.RGFloat, false);
        _uvTex.filterMode = FilterMode.Bilinear;

        _tangentTex = new Texture3D(_boxSize.x, _boxSize.y, _boxSize.z, TextureFormat.RGBAFloat, false);
        _tangentTex.filterMode = FilterMode.Bilinear;

        _mesh.GetVertices(_vertices);
        _mesh.GetTriangles(_triangles, 0);
        _mesh.GetNormals(_normals);
        _mesh.GetTangents(_tangents);
        _mesh.GetUVs(0, _uvs);
        _sdf = new Vector4[_boxSize.x, _boxSize.y, _boxSize.z];
        _sdfUV = new Vector2[_boxSize.x, _boxSize.y, _boxSize.z];
        _sdfTangent = new Vector4[_boxSize.x, _boxSize.y, _boxSize.z];

        for (int i = 0; i < _boxSize.x; i++) {
            for (int j = 0; j < _boxSize.y; j++) {
                for (int k = 0; k < _boxSize.z; k++) {
                    _sdf[i, j, k] = GetSDF(TexelToLocal(new Vector3Int(i, j, k)), out Vector2 uv, out Vector4 tangent);
                    _sdfUV[i, j, k] = uv;
                    _sdfTangent[i, j, k] = tangent;
                }
            }
        }

        _tex.SetPixelData(_sdf.Cast<Vector4>().ToArray(), 0);
        _uvTex.SetPixelData(_sdfUV.Cast<Vector2>().ToArray(), 0);
        _tangentTex.SetPixelData(_sdfTangent.Cast<Vector4>().ToArray(), 0);

        _tex.Apply();
        _uvTex.Apply();
        _tangentTex.Apply();

        _mat = _renderQuad.material;

        _mat.SetTexture("_SDFTexture", _tex);
        _mat.SetTexture("_SDFTangentTexture", _tangentTex);
        _mat.SetTexture("_SDFUVTexture", _uvTex);
        _mat.SetFloat("_TexelSize", _texelSize);
        _mat.SetVector("_TexSize", new Vector4(_boxSize.x, _boxSize.y, _boxSize.z));

        _vertices = null;
        _triangles = null;
        _normals = null;
        _tangents = null;
        _uvs = null;
    }

    private void Update() {
        _mat.SetMatrix("_InvObjectTransform", transform.worldToLocalMatrix);
    }

    public void AddMark(Vector3 point, Vector3 forward, float size) {
        Vector3 texelPoint = WorldToTexelF(point);
        Vector3 texelPointTMP = WorldToTexelF(point - forward * 0.5f);

        forward = Quaternion.Inverse(transform.rotation) * forward;

        float t = 0f;


        for (int i = 0; i < 100; i++) {
            Vector3 curr = (texelPointTMP + forward * t);
            if (curr.x < 0 || curr.x >= _boxSize.x
                || curr.y < 0 || curr.y >= _boxSize.y
                || curr.z < 0 || curr.z >= _boxSize.z) {
                t += 2;
                continue;
            }

            Vector3Int currInt = Vector3Int.FloorToInt(curr);

            var sdf = _sdf[currInt.x, currInt.y, currInt.z];

            if (sdf.w < 0.015f) {
                texelPoint = currInt;
                break;
            }

            t += sdf.w;
        }

        size /= _texelSize;

        for (int i = Mathf.Max(0, (int)(texelPoint.x - size * 2)); i < Mathf.Min(_boxSize.x, (int)(texelPoint.x + size * 2)); i++) {
            for (int j = Mathf.Max(0, (int)(texelPoint.y - size * 2)); j < Mathf.Min(_boxSize.y, (int)(texelPoint.y + size * 2)); j++) {
                for (int k = Mathf.Max(0, (int)(texelPoint.z - size * 2)); k < Mathf.Min(_boxSize.z, (int)(texelPoint.z + size * 2)); k++) {
                    Vector3 curr = new(i, j, k);
                    float dist = Vector3.Distance(texelPoint, curr);
                    if (_sdf[i, j, k].w <= -0.015f) {
                        if (dist <= size) {
                            _sdf[i, j, k].w = (size - dist) / _boxHalfSize.x;
                            Vector3 norm = Vector3.Normalize(texelPoint - curr);
                            _sdf[i, j, k].x = norm.x;
                            _sdf[i, j, k].y = norm.y;
                            _sdf[i, j, k].z = norm.z;

                            Vector3 tang;
                            if (Mathf.Abs(j) > _boxHalfSize.y) {
                                tang = Vector3.Cross(norm, Vector3.right);
                            }
                            else {
                                tang = Vector3.Cross(norm, Vector3.up);
                            }
                            _sdfTangent[i, j, k].x = tang.x;
                            _sdfTangent[i, j, k].y = tang.y;
                            _sdfTangent[i, j, k].z = tang.z;
                        }
                        else {
                            float tmp = (size - dist) / _boxHalfSize.x;
                            if (tmp > _sdf[i, j, k].w) {
                                _sdf[i, j, k].w = tmp;
                                Vector3 norm = Vector3.Normalize(texelPoint - curr);
                                _sdf[i, j, k].x = norm.x;
                                _sdf[i, j, k].y = norm.y;
                                _sdf[i, j, k].z = norm.z;

                                Vector3 tang;
                                if (Mathf.Abs(j) > _boxHalfSize.y) {
                                    tang = Vector3.Cross(norm, Vector3.right);
                                }
                                else {
                                    tang = Vector3.Cross(norm, Vector3.up);
                                }
                                _sdfTangent[i, j, k].x = tang.x;
                                _sdfTangent[i, j, k].y = tang.y;
                                _sdfTangent[i, j, k].z = tang.z;
                            }
                        }
                    }
                    else {
                        _sdf[i, j, k].w = Mathf.Max(_sdf[i, j, k].w, (size - dist) / _boxHalfSize.x);
                    }
                }
            }
        }

        _tex.SetPixelData(_sdf.Cast<Vector4>().ToArray(), 0);
        _tangentTex.SetPixelData(_sdfTangent.Cast<Vector4>().ToArray(), 0);

        _tex.Apply();
        _tangentTex.Apply();
    }

    private Vector4 GetSDF(Vector3 pos, out Vector2 uv, out Vector4 tangent) {
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
            Vector3 posDir = pos - a;
            Vector3 projected = Vector3.Project(posDir, normal);

            Vector3 onPlane = pos - projected;

            float invN2 = 1 / Vector3.Dot(normal, normal);

            float u = Vector3.Dot(Vector3.Cross(b - pos, c - pos), normal) * invN2;
            float v = Vector3.Dot(Vector3.Cross(c - pos, a - pos), normal) * invN2;
            float w = 1.0f - u - v;

            if (u >= 0 && v >= 0 && w >= 0) {
                var mag = Vector3.SqrMagnitude(onPlane - pos);
                if (mag < smallest) {
                    smallest = mag;
                    sign = Vector3.Dot(normal, projected) > 0 ? 1 : -1;
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
                    sign = 1;
                    smallestTri = i;
                    smallNormal = normal;
                }
            }
        }

        Vector3 a1 = _vertices[_triangles[smallestTri]];
        Vector3 b1 = _vertices[_triangles[smallestTri + 1]];
        Vector3 c1 = _vertices[_triangles[smallestTri + 2]];

        float invN21 = 1 / Vector3.Dot(smallNormal, smallNormal);

        float u1 = Vector3.Dot(Vector3.Cross(b1 - pos, c1 - pos), smallNormal) * invN21;
        float v1 = Vector3.Dot(Vector3.Cross(c1 - pos, a1 - pos), smallNormal) * invN21;
        float w1 = 1.0f - u1 - v1;

        Vector3 norm = _normals[_triangles[smallestTri]] * u1
            + _normals[_triangles[smallestTri + 1]] * v1
            + _normals[_triangles[smallestTri + 2]] * w1;
        norm = Vector3.Normalize(norm);

        uv = _uvs[_triangles[smallestTri]] * u1
            + _uvs[_triangles[smallestTri + 1]] * v1
            + _uvs[_triangles[smallestTri + 2]] * w1;

        tangent = _tangents[_triangles[smallestTri]] * u1
            + _tangents[_triangles[smallestTri + 1]] * v1
            + _tangents[_triangles[smallestTri + 2]] * w1;

        float signedDist = Mathf.Sqrt(smallest) / TexelToLocal(_boxHalfSize.x) * sign;

        tangent.w = Mathf.Clamp01(-(signedDist + 0.15f) / 0.1f);

        if (signedDist < 0) {
            Vector2 newUV = new(norm.x, norm.z);
            newUV = Vector2.Normalize(newUV);
            uv = newUV * -signedDist * 2f + Vector2.one;
        }

        return new Vector4(norm.x, norm.y, norm.z, signedDist);
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

    private Vector3 WorldToTexelF(Vector3 pos) {
        return transform.InverseTransformPoint(pos) / _texelSize + _boxHalfSize;
    }

    private Vector3 TexelToWorld(Vector3 pos) {
        return transform.TransformPoint((pos - _boxHalfSize) * _texelSize);
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
            Gizmos.DrawWireCube(transform.position, Vector3.Scale((Vector3)_boxSize, transform.localScale) * _texelSize);
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
