using System.Collections.Generic;
using UnityEngine;

public class SDFTree : MonoBehaviour {
    [SerializeField] private string _assetName;
    [SerializeField] private Mesh _treeMesh;
    [SerializeField] private Material _sdfMaterialBase;
    [SerializeField] private Material _trunkMaterial;
    [SerializeField] private MeshFilter _trunkMesh;
    [SerializeField] private MeshFilter _lowerTrunk;
    [SerializeField] private Renderer _renderQuad;

    [Min(1)]
    [SerializeField] private Vector3Int _boxSize = Vector3Int.one;
    private Vector3 _boxHalfSize = Vector3Int.one;

    [Min(0.001f)]
    [SerializeField] private float _texelSize = 0.05f;

    [SerializeField] private Transform _fallingTransform;
    [SerializeField] private Renderer _fallingRenderer;

    private Mesh _upperMesh;
    private Mesh _lowerMesh;

    [SerializeField] private Material _mat;
    private Material _fallingMat = null;

    [SerializeField] private Texture3D _sdfTex = null;
    [SerializeField] private Texture3D _sdfTangentTex = null;

    private Texture3D _tex = null;
    private Texture3D _uvTex = null;
    private Texture3D _tangentTex = null;

    [SerializeField] private float _sepDist = 0.4f;

    private Vector4[] _sdf; // xyz - normal, w - signed distance
    private Vector2[] _sdfUV;
    private Vector4[] _sdfTangent; // xyz - tangent, w - inside blending

    private List<Vector3> _vertices = new();
    private List<int> _triangles = new();
    private List<Vector3> _normals = new();
    private List<Vector4> _tangents = new();
    private List<Vector2> _uvs = new();

    private void Start() {
        _tex = Instantiate(_sdfTex);
        _tangentTex = Instantiate(_sdfTangentTex);
        _mat.SetTexture("_SDFTexture", _tex);
        _mat.SetTexture("_SDFTangentTexture", _tangentTex);
        _mat.SetVector("_SeparatePoint", new Vector4(0, 0));

        _boxHalfSize = (Vector3)_boxSize / 2.0f;
        _sdf = _tex.GetPixelData<Vector4>(0).ToArray();
        _sdfTangent = _tangentTex.GetPixelData<Vector4>(0).ToArray();
    }

    private void Update() {
        _mat.SetMatrix("_InvObjectTransform", transform.worldToLocalMatrix);
        if (_fallingMat != null) {
            _fallingMat.SetMatrix("_InvObjectTransform", _fallingTransform.worldToLocalMatrix);
        }
    }

    private void OnDestroy() {
        Destroy(_fallingMat);
    }

#if UNITY_EDITOR
    [ContextMenu("Generate SDF")]
    public void Generate() {
        _boxHalfSize = (Vector3)_boxSize / 2.0f;
        if (_trunkMaterial == null || _trunkMesh == null || _lowerTrunk == null || _renderQuad == null || _fallingTransform == null || _fallingRenderer == null) {
            Debug.LogError("SDF Tree not setup");
            return;
        }

        _tex = new Texture3D(_boxSize.x, _boxSize.y, _boxSize.z, TextureFormat.RGBAFloat, false, true) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        _uvTex = new Texture3D(_boxSize.x, _boxSize.y, _boxSize.z, TextureFormat.RGFloat, false, true) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        _tangentTex = new Texture3D(_boxSize.x, _boxSize.y, _boxSize.z, TextureFormat.RGBAFloat, false, true) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        _upperMesh = Instantiate(_treeMesh);
        _lowerMesh = Instantiate(_upperMesh);

        _lowerTrunk.mesh = _lowerMesh;
        _lowerTrunk.transform.SetPositionAndRotation(_trunkMesh.transform.position, _trunkMesh.transform.rotation);
        _lowerTrunk.transform.localScale = _trunkMesh.transform.localScale;

        _upperMesh.GetVertices(_vertices);
        _upperMesh.GetTriangles(_triangles, 0);
        _upperMesh.GetNormals(_normals);
        _upperMesh.GetTangents(_tangents);
        _upperMesh.GetUVs(0, _uvs);

        _sdf = new Vector4[_boxSize.x * _boxSize.y * _boxSize.z];
        _sdfUV = new Vector2[_boxSize.x * _boxSize.y * _boxSize.z];
        _sdfTangent = new Vector4[_boxSize.x * _boxSize.y * _boxSize.z];

        for (int i = 0; i < _boxSize.x; i++) {
            for (int j = 0; j < _boxSize.y; j++) {
                for (int k = 0; k < _boxSize.z; k++) {
                    int idx = Map3DtoIdx(i, j, k);
                    _sdf[idx] = GetSDF(TexelToLocal(new Vector3Int(i, j, k)), out Vector2 uv, out Vector4 tangent);
                    _sdfUV[idx] = uv;
                    _sdfTangent[idx] = tangent;
                }
            }
        }

        _tex.SetPixelData(_sdf, 0);
        _uvTex.SetPixelData(_sdfUV, 0);
        _tangentTex.SetPixelData(_sdfTangent, 0);

        _tex.Apply();
        _uvTex.Apply();
        _tangentTex.Apply();

        _sdfTex = _tex;
        _sdfTangentTex = _tangentTex;

        if (!UnityEditor.AssetDatabase.IsValidFolder($"Assets/Trees/{_assetName}")) {
            UnityEditor.AssetDatabase.CreateFolder("Assets/Trees", _assetName);
        }

        UnityEditor.AssetDatabase.CreateAsset(_tex, $"Assets/Trees/{_assetName}/sdf.asset");
        UnityEditor.AssetDatabase.CreateAsset(_uvTex, $"Assets/Trees/{_assetName}/uv.asset");
        UnityEditor.AssetDatabase.CreateAsset(_tangentTex, $"Assets/Trees/{_assetName}/tangent.asset");

        _mat = Instantiate(_sdfMaterialBase);

        _mat.SetTexture("_SDFTexture", _tex);
        _mat.SetTexture("_SDFTangentTexture", _tangentTex);
        _mat.SetTexture("_SDFUVTexture", _uvTex);
        _mat.SetFloat("_TexelSize", _texelSize);
        _mat.SetVector("_TexSize", new Vector4(_boxSize.x, _boxSize.y, _boxSize.z));

        _fallingRenderer.gameObject.SetActive(false);

        List<int> upperTries = new();
        List<int> lowerTries = new();

        float upperBound = (_boxSize.y - LocalToTexel(_sepDist)) * transform.localScale.y * _texelSize;
        float lowerBound = -upperBound;

        for (int i = 0; i < _triangles.Count; i += 3) {
            Vector3 a = MeshToLocal(_vertices[_triangles[i]]);
            Vector3 b = MeshToLocal(_vertices[_triangles[i + 1]]);
            Vector3 c = MeshToLocal(_vertices[_triangles[i + 2]]);

            if (a.y >= upperBound || b.y >= upperBound || c.y >= upperBound) {
                upperTries.Add(_triangles[i]);
                upperTries.Add(_triangles[i + 1]);
                upperTries.Add(_triangles[i + 2]);
            }

            if (a.y <= lowerBound || b.y <= lowerBound || c.y <= lowerBound) {
                lowerTries.Add(_triangles[i]);
                lowerTries.Add(_triangles[i + 1]);
                lowerTries.Add(_triangles[i + 2]);
            }
        }

        _upperMesh.SetTriangles(upperTries, 0);
        _lowerMesh.SetTriangles(lowerTries, 0);

        _upperMesh.UploadMeshData(true);
        _lowerMesh.UploadMeshData(true);

        _trunkMesh.mesh = _upperMesh;

        UnityEditor.AssetDatabase.CreateAsset(_upperMesh, $"Assets/Trees/{_assetName}/upperMesh.mesh");
        UnityEditor.AssetDatabase.CreateAsset(_lowerMesh, $"Assets/Trees/{_assetName}/lowerMesh.mesh");

        _vertices.Clear();
        _triangles.Clear();
        _normals.Clear();
        _tangents.Clear();
        _uvs.Clear();

        _mat.SetTexture("_DiffuseTexture", _trunkMaterial.GetTexture("_DiffuseTexture"));
        _mat.SetTexture("_NormalTexture", _trunkMaterial.GetTexture("_NormalTexture"));
        _mat.SetFloat("_SpecularMult", _trunkMaterial.GetFloat("_SpecularMult"));

        _renderQuad.material = _mat;

        UnityEditor.AssetDatabase.CreateAsset(_mat, $"Assets/Trees/{_assetName}/material.mat");

        _mat.SetMatrix("_InvObjectTransform", transform.worldToLocalMatrix);
    }
#endif

    public void Separate(Vector3 position) {
        Vector3 texelPos = WorldToTexelF(position);

        float affectRadius = LocalToTexel(0.5f);

        for (int j = Mathf.Max((int)(texelPos.y - affectRadius), 0); j < Mathf.Min((int)(texelPos.y + affectRadius), _boxSize.y); j++) {
            for (int i = 0; i < _boxSize.x; i++) {
                for (int k = 0; k < _boxSize.z; k++) {
                    float dist = Mathf.Abs(texelPos.y - j) - 2f;

                    int idx = Map3DtoIdx(i, j, k);

                    if (_sdf[idx].w <= 0.015f) {
                        float tmp = -dist / _boxHalfSize.x;
                        if (tmp > _sdf[idx].w) {
                            _sdf[idx].w = tmp;
                            Vector3 norm = Vector3.Normalize(Vector3.up * ((texelPos.y - j) > 0 ? 1f : -1f));
                            _sdf[idx].x = norm.x;
                            _sdf[idx].y = norm.y;
                            _sdf[idx].z = norm.z;

                            Vector3 tang = Vector3.Cross(norm, -Vector3.right);
                            _sdfTangent[idx].x = tang.x;
                            _sdfTangent[idx].y = tang.y;
                            _sdfTangent[idx].z = tang.z;
                        }
                    }
                    else {
                        _sdf[idx].w = Mathf.Max(_sdf[idx].w, dist / _boxHalfSize.x);
                    }
                }
            }
        }

        _tex.SetPixelData(_sdf, 0);
        _tangentTex.SetPixelData(_sdfTangent, 0);

        _tex.Apply();
        _tangentTex.Apply();

        _mat.SetVector("_SeparatePoint", new Vector4(texelPos.y - 1, -1));

        _fallingMat = Instantiate(_mat);

        _fallingMat.SetVector("_SeparatePoint", new Vector4(texelPos.y + 1, 1));

        _fallingRenderer.gameObject.SetActive(true);

        _fallingRenderer.material = _fallingMat;
    }

    public void AddMark(Vector3 point, Vector3 forward, Vector3 size) {
        Vector3 texelPoint = WorldToTexelF(point);
        Vector3 texelPointTMP = WorldToTexelF(point - forward * 0.5f);

        forward = Quaternion.Inverse(transform.rotation) * forward;

        Raymarch(texelPointTMP, forward, ref texelPoint);

        Debug.DrawRay(TexelToWorld(texelPoint), forward, Color.red, 5);

        size /= _texelSize;
        Vector3 invSize = new(1f / size.x, 1f / size.y, 1f / size.z);

        for (int i = Mathf.Max(0, (int)(texelPoint.x - size.x * 2)); i < Mathf.Min(_boxSize.x, (int)(texelPoint.x + size.x * 2)); i++) {
            for (int j = Mathf.Max(0, (int)(texelPoint.y - size.y * 2)); j < Mathf.Min(_boxSize.y, (int)(texelPoint.y + size.y * 2)); j++) {
                for (int k = Mathf.Max(0, (int)(texelPoint.z - size.z * 2)); k < Mathf.Min(_boxSize.z, (int)(texelPoint.z + size.z * 2)); k++) {
                    Vector3 curr = new(i, j, k);
                    Vector3 diff = texelPoint - curr;
                    Vector3 normal = Vector3.Normalize(diff);
                    float sizeScaled = Vector3.Magnitude(Vector3.Scale(normal, size));
                    float dist = Vector3.Magnitude(diff);

                    int idx = Map3DtoIdx(i, j, k);

                    if (_sdf[idx].w <= 0.015f) {
                        if (dist <= sizeScaled) {
                            _sdf[idx].w = (sizeScaled - dist) / _boxHalfSize.x;
                            Vector3 norm;
                            if (Mathf.Abs(dist) <= 0.015f) {
                                norm = -forward;
                            }
                            else {
                                 norm = Vector3.Normalize(texelPoint - curr);
                            }
                            _sdf[idx].x = norm.x;
                            _sdf[idx].y = norm.y;
                            _sdf[idx].z = norm.z;

                            Vector3 tang;
                            if (Mathf.Abs(Vector3.Dot(Vector3.Normalize(curr - texelPoint), Vector3.up)) >= 0.95f) {
                                tang = Vector3.Cross(norm, Vector3.right);
                            }
                            else {
                                tang = Vector3.Cross(norm, Vector3.up);
                            }
                            _sdfTangent[idx].x = tang.x;
                            _sdfTangent[idx].y = tang.y;
                            _sdfTangent[idx].z = tang.z;
                        }
                        else {
                            float tmp = (sizeScaled - dist) / _boxHalfSize.x;
                            if (tmp > _sdf[idx].w) {
                                _sdf[idx].w = tmp;
                                Vector3 norm = Vector3.Normalize(texelPoint - curr);
                                _sdf[idx].x = norm.x;
                                _sdf[idx].y = norm.y;
                                _sdf[idx].z = norm.z;

                                Vector3 tang;
                                if (Mathf.Abs(Vector3.Dot(Vector3.Normalize(curr - texelPoint), Vector3.up)) >= 0.95f) {
                                    tang = Vector3.Cross(norm, Vector3.right);
                                }
                                else {
                                    tang = Vector3.Cross(norm, Vector3.up);
                                }
                                _sdfTangent[idx].x = tang.x;
                                _sdfTangent[idx].y = tang.y;
                                _sdfTangent[idx].z = tang.z;
                            }
                        }
                    }
                    else {
                        _sdf[idx].w = Mathf.Max(_sdf[idx].w, (sizeScaled - dist) / _boxHalfSize.x);
                    }
                }
            }
        }

        _tex.SetPixelData(_sdf, 0);
        _tangentTex.SetPixelData(_sdfTangent, 0);

        _tex.Apply();
        _tangentTex.Apply();
    }

    private bool IsNormal(Vector3 vec) {
        return !float.IsNaN(vec.x) && !float.IsNaN(vec.y) && !float.IsNaN(vec.z)
            && !float.IsInfinity(vec.x) && !float.IsInfinity(vec.y) && !float.IsInfinity(vec.z);
    }

    private bool Raymarch(Vector3 start, Vector3 dir, ref Vector3 hit) {
        float t = 0f;

        for (int i = 0; i < 200; i++) {
            Vector3 curr = (start + dir * t);
            if (curr.x < 0 || curr.x >= _boxSize.x
                || curr.y < 0 || curr.y >= _boxSize.y
                || curr.z < 0 || curr.z >= _boxSize.z) {
                t += 2;
                continue;
            }

            Vector3Int currInt = Vector3Int.FloorToInt(curr);

            var sdf = _sdf[Map3DtoIdx(currInt.x, currInt.y, currInt.z)];

            if (sdf.w < 0.015f) {
                hit = curr;
                return true;
            }

            t += sdf.w;
        }

        return false;
    }

    private Vector4 GetSDF(Vector3 pos, out Vector2 uv, out Vector4 tangent) {
        float smallest = float.MaxValue;
        float sign = 1f;
        Vector3 smallNormal = Vector3.zero;

        int smallestTri = 0;

        for (int i = 0; i < _triangles.Count; i += 3) {
            Vector3 a = MeshToLocal(_vertices[_triangles[i]]);
            Vector3 b = MeshToLocal(_vertices[_triangles[i + 1]]);
            Vector3 c = MeshToLocal(_vertices[_triangles[i + 2]]);
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

        Vector3 a1 = MeshToLocal(_vertices[_triangles[smallestTri]]);
        Vector3 b1 = MeshToLocal(_vertices[_triangles[smallestTri + 1]]);
        Vector3 c1 = MeshToLocal(_vertices[_triangles[smallestTri + 2]]);

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

        tangent.w = Mathf.Clamp01(-(signedDist + 0.1f) / 0.1f);

        if (signedDist < -0.15f) {
            Vector2 newUV = new(norm.x, norm.z);
            newUV = Vector2.Normalize(newUV);
            uv = newUV * -signedDist / 2f + Vector2.one * 0.5f;
        }

        return new Vector4(norm.x, norm.y, norm.z, signedDist);
    }

    private Vector3 MeshToLocal(Vector3 pos) {
        var trans = _trunkMesh.transform;
        return trans.localPosition + Vector3.Scale(trans.localScale, trans.localRotation * pos);
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

    private int Map3DtoIdx(int i, int j, int k) {
        return (i * _boxSize.y * _boxSize.z) + (j * _boxSize.z) + k;
    }

    private Vector3 Project(Vector3 point, Vector3 a, Vector3 b) {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        return a + t * ab;
    }

    private void OnDrawGizmosSelected() {
        if (_sdf == null || _sdf.Length == 0) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.Scale((Vector3)_boxSize, transform.localScale) * _texelSize);
            return;
        }

        Color tmp = Color.black;

        for (int i = 0; i < _boxSize.x; i++) {
            for (int j = 0; j < _boxSize.y; j++) {
                for (int k = 0; k < _boxSize.z; k++) {
                    if (_sdf[Map3DtoIdx(i, j, k)].w < 0.01f) {
                        Gizmos.color = tmp;
                        Gizmos.DrawWireCube(TexelToWorld(new Vector3Int(i, j, k)), Vector3.one * _texelSize * 2);
                    }
                }
            }
        }
    }
}
